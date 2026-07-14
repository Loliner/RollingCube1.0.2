using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [SerializeField] private float rollDuration = 0.25f;
    [SerializeField] private float cubeHalfSize = 0.5f;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private bool canClimb = true;

    private Rigidbody rb;
    private bool isRolling;
    private bool isFalling;
    private bool isStuck;

    private int groundLevel;
    private int stuckLevel;
    private int stuckBaseLevel;
    private int stuckWallTop;
    private Vector3 stuckDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        Vector3 pos = SnapToGrid(transform.position);
        pos.y = LevelToY(0);
        transform.position = pos;
        groundLevel = 0;
    }

    void Update()
    {
        if (isRolling || isFalling) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Vector3 direction;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) direction = Vector3.forward;
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) direction = Vector3.back;
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) direction = Vector3.left;
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) direction = Vector3.right;
        else return;

        if (isStuck)
        {
            if (direction == stuckDirection)
                StartCoroutine(ClimbStep(direction));
            else if (direction == -stuckDirection)
                StartCoroutine(ClimbDownStep());
            // Perpendicular keys are ignored while stuck to a wall.
            return;
        }

        StartCoroutine(TryMove(direction));
    }

    // Decides how to respond to a roll input by comparing the height of
    // whatever is in the target cell against the cube's current level:
    // equal height rolls flat, +1 climbs in a single half-turn, +2 or more
    // starts a multi-step climb (see ClimbStep), -1 descends in a single
    // half-turn when climbing is allowed, and anything else (a bigger drop,
    // or no support at all) is an ordinary fall.
    private IEnumerator TryMove(Vector3 direction)
    {
        int targetHeight;
        bool hasSupport = TryGetTargetHeight(direction, out targetHeight);
        int diff = hasSupport ? targetHeight - groundLevel : int.MinValue;

        if (!hasSupport || diff <= -2 || (diff == -1 && !canClimb))
        {
            yield return AnimateRoll(direction, Vector3.down, 90f);
            FinishAfterRoll();
            yield break;
        }

        if (diff == 0)
        {
            yield return AnimateRoll(direction, Vector3.down, 90f);
            groundLevel = targetHeight;
            FinishAfterRoll();
            yield break;
        }

        if (diff == -1)
        {
            yield return AnimateRoll(direction, Vector3.down, -180f);
            groundLevel = targetHeight;
            FinishAfterRoll();
            yield break;
        }

        if (!canClimb) yield break;

        if (diff == 1)
        {
            yield return AnimateRoll(direction, Vector3.up, -180f);
            groundLevel = targetHeight;
            FinishAfterRoll();
            yield break;
        }

        stuckBaseLevel = groundLevel;
        stuckDirection = direction;
        stuckWallTop = targetHeight;
        yield return AnimateRoll(direction, Vector3.up, 90f);
        stuckLevel = groundLevel + 1;
        isStuck = true;
        isRolling = false;
    }

    // A single quarter-turn can never combine a horizontal step with a
    // vertical one (there's no grid-aligned edge that produces both at
    // once), so climbing more than one level up always takes a quarter-turn
    // per intermediate level (tipping the cube up flush against the wall,
    // pinned over its starting cell) followed by one final half-turn that
    // carries it over onto the wall's top.
    private IEnumerator ClimbStep(Vector3 direction)
    {
        int remaining = stuckWallTop - stuckLevel;
        if (remaining == 1)
        {
            yield return AnimateRoll(direction, Vector3.up, -180f);
            groundLevel = stuckWallTop;
            isStuck = false;
            isRolling = false;
        }
        else
        {
            yield return AnimateRoll(direction, Vector3.up, 90f);
            stuckLevel += 1;
            isRolling = false;
        }
    }

    // Reverses the last climb step around the same edge it pivoted on, one
    // level at a time; reaching the level the climb started from drops the
    // cube back to its normal grounded state.
    private IEnumerator ClimbDownStep()
    {
        yield return AnimateRoll(stuckDirection, Vector3.down, -90f);
        stuckLevel -= 1;
        if (stuckLevel == stuckBaseLevel)
        {
            groundLevel = stuckBaseLevel;
            isStuck = false;
        }
        isRolling = false;
    }

    // Rotates the cube by an exact angle around the edge offset from its
    // current position by direction*half (horizontal) and verticalOffset*half
    // (vertical). A 90-degree down-offset pivot is the ordinary flat roll; a
    // 90-degree up-offset pivot tips the cube up flush against a taller
    // obstacle without advancing into it; a 180-degree pivot (up or down
    // offset) covers a full climb/descend of exactly one level in a single
    // motion.
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
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Quaternion step = Quaternion.Slerp(Quaternion.identity, fullTurn, t);
            transform.position = step * (startPos - pivot) + pivot;
            transform.rotation = step * startRot;
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    private void FinishAfterRoll()
    {
        if (HasSupportBelow())
            isRolling = false;
        else
            StartFalling();
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x - cubeHalfSize) + cubeHalfSize;
        pos.y = Mathf.Round(pos.y - cubeHalfSize) + cubeHalfSize;
        pos.z = Mathf.Round(pos.z - cubeHalfSize) + cubeHalfSize;
        return pos;
    }

    private float LevelToY(int level)
    {
        return level * (cubeHalfSize * 2f) + cubeHalfSize;
    }

    // A Climbable reports its own height; anything else solid ahead (bare
    // ground) counts as height 0. Casting from high above the target cell
    // straight down finds whichever is there regardless of how high the
    // cube currently is, and finds nothing at all off the edge of the world.
    private bool TryGetTargetHeight(Vector3 direction, out int height)
    {
        Vector3 targetCenter = SnapToGrid(transform.position + direction);
        Vector3 rayOrigin = new Vector3(targetCenter.x, transform.position.y + 50f, targetCenter.z);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 200f, surfaceMask))
        {
            Climbable climbable = hit.collider.GetComponent<Climbable>();
            height = climbable != null ? climbable.HeightUnits : 0;
            return true;
        }

        height = 0;
        return false;
    }

    private bool HasSupportBelow()
    {
        return Physics.Raycast(transform.position, Vector3.down, cubeHalfSize + 0.05f, surfaceMask);
    }

    private void StartFalling()
    {
        isFalling = true;
        rb.isKinematic = false;
    }
}
