using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [SerializeField] private float rollDuration = 0.25f; // seconds per 90-degree turn
    [SerializeField] private float cubeHalfSize = 0.5f; // half the cube's unit size
    [SerializeField] private LayerMask surfaceMask = ~0; // layers treated as ground/walls
    [SerializeField] private bool canClimb = true; // sticky ability toggle

    private Rigidbody rb;
    private bool isRolling; // true while a roll animation is playing
    private bool isFalling; // true once gravity has taken over
    private bool isStuck; // true while pinned mid-climb against a wall

    private int groundLevel; // level the cube is currently resting at
    private int stuckLevel; // level reached so far while stuck climbing
    private Vector3 stuckDirection; // direction of the wall being climbed

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
                StartCoroutine(ClimbStep(direction, stuckLevel));
            else if (direction == -stuckDirection)
                StartCoroutine(ClimbDownStep());
            // Perpendicular keys are ignored while stuck to a wall.
            return;
        }

        StartCoroutine(TryMove(direction));
    }

    // Decides the next single roll from what's physically at the target
    // cell right now: blocked at the same level starts a climb, support one
    // level down is a flat roll, a climbable surface underfoot allows a
    // graceful descent, otherwise it's an ordinary fall.
    private IEnumerator TryMove(Vector3 direction)
    {
        int level = groundLevel;
        Vector3 targetColumn = SnapToGrid(transform.position + direction);

        if (IsSolidSlice(targetColumn, level))
        {
            if (!canClimb) yield break;
            yield return ClimbStep(direction, level);
            yield break;
        }

        bool flatLanding = IsSolidSlice(targetColumn, level - 1);
        if (!flatLanding && canClimb && IsClimbableSlice(SnapToGrid(transform.position), level - 1))
        {
            yield return AnimateRoll(direction, Vector3.down, -180f);
            groundLevel = level - 1;
            FinishAfterRoll();
            yield break;
        }

        yield return AnimateRoll(direction, Vector3.down, 90f);
        FinishAfterRoll();
    }

    // Tips the cube up one level if the wall continues above, or finishes
    // with a half-turn onto the top if it doesn't. Used both to start a
    // climb from the ground and to continue one already in progress.
    private IEnumerator ClimbStep(Vector3 direction, int fromLevel)
    {
        Vector3 targetColumn = SnapToGrid(transform.position + direction);
        bool moreWallAbove = IsSolidSlice(targetColumn, fromLevel + 1);

        if (moreWallAbove)
        {
            yield return AnimateRoll(direction, Vector3.up, 90f);
            stuckDirection = direction;
            stuckLevel = fromLevel + 1;
            isStuck = true;
            isRolling = false;
        }
        else
        {
            yield return AnimateRoll(direction, Vector3.up, -180f);
            groundLevel = fromLevel + 1;
            isStuck = false;
            FinishAfterRoll();
        }
    }

    // Undoes one climb step; reaching the starting level exits stuck state.
    private IEnumerator ClimbDownStep()
    {
        yield return AnimateRoll(stuckDirection, Vector3.down, -90f);
        stuckLevel -= 1;
        if (stuckLevel == groundLevel)
        {
            isStuck = false;
        }
        isRolling = false;
    }

    // Rotates the cube by angle around the edge offset from its current
    // position by direction*half and verticalOffset*half.
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

    // Lands normally if supported, otherwise starts a physics fall.
    private void FinishAfterRoll()
    {
        if (HasSupportBelow())
            isRolling = false;
        else
            StartFalling();
    }

    // Rounds x/y/z to the nearest cell center, avoiding float drift.
    private Vector3 SnapToGrid(Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x - cubeHalfSize) + cubeHalfSize;
        pos.y = Mathf.Round(pos.y - cubeHalfSize) + cubeHalfSize;
        pos.z = Mathf.Round(pos.z - cubeHalfSize) + cubeHalfSize;
        return pos;
    }

    // Converts a grid level to a world Y position.
    private float LevelToY(int level)
    {
        return level * (cubeHalfSize * 2f) + cubeHalfSize;
    }

    // Is there any collider (other than the cube itself) at this column/level?
    private bool IsSolidSlice(Vector3 columnXZ, int level)
    {
        Vector3 center = new Vector3(columnXZ.x, LevelToY(level), columnXZ.z);
        Collider[] hits = Physics.OverlapBox(center, Vector3.one * (cubeHalfSize * 0.9f), Quaternion.identity, surfaceMask);
        foreach (Collider hit in hits)
            if (hit.transform != transform) return true;
        return false;
    }

    // Same as IsSolidSlice, but only true if the collider is a Climbable.
    private bool IsClimbableSlice(Vector3 columnXZ, int level)
    {
        Vector3 center = new Vector3(columnXZ.x, LevelToY(level), columnXZ.z);
        Collider[] hits = Physics.OverlapBox(center, Vector3.one * (cubeHalfSize * 0.9f), Quaternion.identity, surfaceMask);
        foreach (Collider hit in hits)
            if (hit.transform != transform && hit.GetComponent<Climbable>() != null) return true;
        return false;
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
