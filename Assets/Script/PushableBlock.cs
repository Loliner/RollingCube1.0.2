using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PushableBlock : MonoBehaviour, IExternallyControllable
{
    [SerializeField] private float cubeHalfSize = 0.5f; // half the cube's unit size
    [SerializeField] private LayerMask surfaceMask = ~0; // layers treated as ground/walls/pushables
    [SerializeField] private float pushDuration = 0.25f; // should match Player.rollDuration for sync

    private Rigidbody rb;
    private bool isMoving; // true while a push slide is animating
    private bool isExternallyControlled; // true while a mechanism (e.g. elevator) owns movement

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        transform.position = SnapToGrid(transform.position);
    }

    public Transform Transform => transform;
    public bool IsExternallyControlled => isExternallyControlled;

    // Lets an external mechanism (e.g. an elevator) drive this transform directly.
    public void BeginExternalControl()
    {
        isExternallyControlled = true;
    }

    // Hands control back, re-snapping to the grid and falling if unsupported.
    public void EndExternalControl()
    {
        isExternallyControlled = false;
        transform.position = SnapToGrid(transform.position);
        FallIfUnsupported();
    }

    // Starts sliding this block one cell in direction if the destination is clear.
    // Returns false (and does nothing) if the block can't be pushed right now.
    public bool TryBeginPush(Vector3 direction)
    {
        if (isMoving || isExternallyControlled) return false;

        Vector3 destination = SnapToGrid(transform.position + direction);
        if (IsOccupied(destination)) return false;

        isMoving = true;
        transform.DOMove(destination, pushDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                transform.position = destination;
                isMoving = false;
                FallIfUnsupported();
            });

        return true;
    }

    // Any collider (other than this block) occupying the same cell as position?
    private bool IsOccupied(Vector3 position)
    {
        Collider[] hits = Physics.OverlapBox(position, Vector3.one * (cubeHalfSize * 0.9f), Quaternion.identity, surfaceMask);
        foreach (Collider hit in hits)
            if (hit.transform != transform) return true;
        return false;
    }

    // Hands control to physics if there's no ground directly underneath.
    private void FallIfUnsupported()
    {
        bool hasGround = Physics.Raycast(transform.position, Vector3.down, cubeHalfSize + 0.05f, surfaceMask);
        if (!hasGround)
            rb.isKinematic = false;
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
}
