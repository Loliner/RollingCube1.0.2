using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PushableBlock : MonoBehaviour
{
    [SerializeField] private float cubeHalfSize = 0.5f; // half the cube's unit size
    [SerializeField] private LayerMask surfaceMask = ~0; // layers treated as ground/walls/pushables
    [SerializeField] private float pushDuration = 0.25f; // should match Player.rollDuration for sync

    private Rigidbody rb;
    private bool isMoving; // true while a push slide is animating

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        transform.position = SnapToGrid(transform.position);
    }

    // Starts sliding this block one cell in direction if the destination is clear.
    // Returns false (and does nothing) if the block can't be pushed right now.
    public bool TryBeginPush(Vector3 direction)
    {
        if (isMoving) return false;

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

    private Vector3 SnapToGrid(Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x - cubeHalfSize) + cubeHalfSize;
        pos.y = Mathf.Round(pos.y - cubeHalfSize) + cubeHalfSize;
        pos.z = Mathf.Round(pos.z - cubeHalfSize) + cubeHalfSize;
        return pos;
    }
}
