using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour, IExternallyControllable
{
    [SerializeField] private float rollDuration = 0.25f; // seconds per 90-degree turn
    [SerializeField] private float cubeHalfSize = 0.5f; // half the cube's unit size
    [SerializeField] private LayerMask surfaceMask = ~0; // layers treated as ground/walls/pushables

    private Rigidbody rb;
    private bool isRolling; // true while a roll or shake animation is playing
    private bool isFalling; // true once gravity has taken over
    private bool isExternallyControlled; // true while a mechanism (e.g. conveyor) owns movement

    private int groundLevel; // level the cube is currently resting at

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        Vector3 pos = SnapToGrid(transform.position);
        groundLevel = Mathf.RoundToInt(pos.y / (cubeHalfSize * 2f));
        pos.y = LevelToY(groundLevel);
        transform.position = pos;
    }

    public Transform Transform => transform;
    public bool IsExternallyControlled => isExternallyControlled;

    // Lets an external mechanism (e.g. a conveyor belt) drive this transform
    // directly; Update() stops polling input until EndExternalControl() is called.
    public void BeginExternalControl()
    {
        isExternallyControlled = true;
    }

    // Hands control back and re-derives groundLevel/grid alignment from wherever
    // the mechanism left the cube, so normal rolling resumes correctly.
    public void EndExternalControl()
    {
        isExternallyControlled = false;
        Vector3 pos = SnapToGrid(transform.position);
        groundLevel = Mathf.RoundToInt(pos.y / (cubeHalfSize * 2f));
        pos.y = LevelToY(groundLevel);
        transform.position = pos;
    }

    void Update()
    {
        if (isRolling || isFalling || isExternallyControlled) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Vector3 direction;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) direction = Vector3.forward;
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) direction = Vector3.back;
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) direction = Vector3.left;
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) direction = Vector3.right;
        else return;

        StartCoroutine(TryMove(direction));
    }

    // Rolls into the target cell if it's clear or holds a pushable block that
    // can slide out of the way; otherwise shakes in place to signal a blocked move.
    private IEnumerator TryMove(Vector3 direction)
    {
        Vector3 targetColumn = SnapToGrid(transform.position + direction);
        Collider blocker = GetBlockingCollider(targetColumn, groundLevel);

        if (blocker != null)
        {
            PushableBlock pushable = blocker.GetComponent<PushableBlock>();
            if (pushable == null || !pushable.TryBeginPush(direction))
            {
                yield return ShakeFeedback();
                yield break;
            }
        }

        yield return AnimateRoll(direction, Vector3.down, 90f);
        FinishAfterRoll();
    }

    // Rotates the cube by angle around the edge offset from its current
    // position by direction*half and verticalOffset*half. The pivot-rotation
    // math is manual, but the interpolation parameter is driven by DOTween
    // so the roll gets an eased curve instead of linear stepping.
    private IEnumerator AnimateRoll(Vector3 direction, Vector3 verticalOffset, float angle)
    {
        isRolling = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 pivot = startPos + direction * cubeHalfSize + verticalOffset * cubeHalfSize;
        Vector3 axis = Vector3.Cross(Vector3.up, direction);
        Quaternion fullTurn = Quaternion.AngleAxis(angle, axis);

        Vector3 targetPos = SnapToGrid(fullTurn * (startPos - pivot) + pivot);
        Quaternion targetRot = fullTurn * startRot;

        float duration = rollDuration * Mathf.Abs(angle) / 90f;
        float t = 0f;
        bool done = false;
        DOTween.To(() => t, x => t = x, 1f, duration)
            .SetEase(Ease.InOutSine)
            .OnUpdate(() =>
            {
                Quaternion step = Quaternion.Slerp(Quaternion.identity, fullTurn, t);
                transform.position = step * (startPos - pivot) + pivot;
                transform.rotation = step * startRot;
            })
            .OnComplete(() => done = true);

        yield return new WaitUntil(() => done);

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    // Brief random jitter to signal a blocked move; blocks input while it plays.
    private IEnumerator ShakeFeedback(float duration = 0.15f, float magnitude = 0.05f)
    {
        isRolling = true;
        Vector3 origin = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 offset = Random.insideUnitSphere * magnitude;
            offset.y = 0f;
            transform.position = origin + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = origin;
        isRolling = false;
    }

    // Lands normally if supported, otherwise starts a physics fall.
    private void FinishAfterRoll()
    {
        if (HasSupportBelow())
            isRolling = false;
        else
            StartFalling();
    }

    // Rounds x/y/z to the nearest 0.25, correcting float drift (e.g. 1.4999999
    // -> 1.5) without collapsing legitimate sub-integer positions.
    private Vector3 SnapToGrid(Vector3 pos)
    {
        const float snapUnit = 0.25f;
        pos.x = Mathf.Round(pos.x / snapUnit) * snapUnit;
        pos.y = Mathf.Round(pos.y / snapUnit) * snapUnit;
        pos.z = Mathf.Round(pos.z / snapUnit) * snapUnit;
        return pos;
    }

    // Converts a grid level to a world Y position.
    private float LevelToY(int level)
    {
        return level * (cubeHalfSize * 2f);
    }

    // Returns the collider occupying this column/level, if any (other than the cube itself).
    // Trigger colliders (mechanisms like SceneSwitcher, Elevator, ...) are never
    // physical obstacles — they detect the player via OnTrigger, not blocking.
    private Collider GetBlockingCollider(Vector3 columnXZ, int level)
    {
        Vector3 center = new Vector3(columnXZ.x, LevelToY(level), columnXZ.z);
        Collider[] hits = Physics.OverlapBox(center, Vector3.one * (cubeHalfSize * 0.9f), Quaternion.identity, surfaceMask);
        foreach (Collider hit in hits)
            if (hit.transform != transform && !hit.isTrigger) return hit;
        return null;
    }

    // Short raycast straight down from the cube's own position.
    private bool HasSupportBelow()
    {
        return Physics.Raycast(transform.position, Vector3.down, cubeHalfSize + 0.05f, surfaceMask);
    }

    // Hands control over to physics.
    private void StartFalling()
    {
        isFalling = true;
        rb.isKinematic = false;
    }
}
