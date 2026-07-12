using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [SerializeField] private float rollDuration = 0.25f;
    [SerializeField] private float cubeHalfSize = 0.5f;
    [SerializeField] private LayerMask groundMask = ~0;

    private Rigidbody rb;
    private bool isRolling;
    private bool isFalling;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Update()
    {
        if (isRolling || isFalling) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            StartCoroutine(Roll(Vector3.forward));
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            StartCoroutine(Roll(Vector3.back));
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
            StartCoroutine(Roll(Vector3.left));
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            StartCoroutine(Roll(Vector3.right));
    }

    // Rolls the cube by rotating it around the bottom edge facing the travel
    // direction, instead of spinning it in place around its own center.
    //
    // The start/target position and rotation are solved exactly with
    // quaternion math up front. We never reconstruct rotation from Euler
    // angles: for a compound orientation (e.g. after turning more than once)
    // Unity's Euler decomposition doesn't split into independent per-axis
    // 90-degree multiples, so rounding each axis component-wise silently
    // rebuilds the wrong quaternion and the cube visibly pops to a bad pose.
    private IEnumerator Roll(Vector3 direction)
    {
        isRolling = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 pivot = startPos + direction * cubeHalfSize + Vector3.down * cubeHalfSize;
        Vector3 axis = Vector3.Cross(Vector3.up, direction);
        Quaternion fullTurn = Quaternion.AngleAxis(90f, axis);

        Vector3 targetPos = fullTurn * (startPos - pivot) + pivot;
        Quaternion targetRot = fullTurn * startRot;

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rollDuration);
            Quaternion step = Quaternion.Slerp(Quaternion.identity, fullTurn, t);
            transform.position = step * (startPos - pivot) + pivot;
            transform.rotation = step * startRot;
            yield return null;
        }

        // Land exactly on the precomputed target so no interpolation error
        // (or drift from prior rolls) can accumulate across turns.
        transform.position = targetPos;
        transform.rotation = targetRot;

        if (HasGroundBelow())
            isRolling = false;
        else
            StartFalling();
    }

    // Rolling is grid-based, so a single downward ray from the cube's center
    // is enough to tell whether the landed cell is still over the ground.
    private bool HasGroundBelow()
    {
        return Physics.Raycast(transform.position, Vector3.down, cubeHalfSize + 0.05f, groundMask);
    }

    // No ground under the cube: hand it off to physics so it drops naturally.
    private void StartFalling()
    {
        isFalling = true;
        rb.isKinematic = false;
    }
}
