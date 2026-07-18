using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [SerializeField] private GameObject elevator;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool reset;
    [SerializeField] private float resetDelay = 3f;
    [SerializeField] private bool switcherFollow;
    [SerializeField] protected float moveDuration = 2f;

    private Vector3 elevatorStartPos;
    private Vector3 switcherStartPos;
    private bool isTriggered;
    private readonly List<IExternallyControllable> riders = new List<IExternallyControllable>(); // everything currently standing on the elevator

    void Start()
    {
        elevatorStartPos = elevator.transform.position;
        switcherStartPos = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        IExternallyControllable rider = other.GetComponent<IExternallyControllable>();
        if (rider == null) return;
        if (!riders.Contains(rider)) riders.Add(rider);

        if (isTriggered) return; // already running; new rider just joins for the next leg
        StartCoroutine(StartAnimation());
    }

    void OnTriggerExit(Collider other)
    {
        IExternallyControllable rider = other.GetComponent<IExternallyControllable>();
        if (rider == null) return;
        riders.Remove(rider);

        if (!isTriggered || riders.Count > 0) return; // wait until everyone's off before resetting
        StartCoroutine(ResetAnimation());
    }

    private IEnumerator StartAnimation()
    {
        isTriggered = true;
        elevator.transform.DOMove(elevatorStartPos + offset, moveDuration).SetEase(Ease.InOutSine);
        if (switcherFollow && elevator.transform != transform)
            transform.DOMove(switcherStartPos + offset, moveDuration).SetEase(Ease.InOutSine);

        CarryRiders(offset);

        OnStartAnimation();
        yield return null;
    }

    public virtual void OnStartAnimation() { }

    private IEnumerator ResetAnimation()
    {
        if (!reset) yield break;

        yield return new WaitForSeconds(resetDelay);

        elevator.transform.DOMove(elevatorStartPos, moveDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => isTriggered = false);
        if (switcherFollow && elevator.transform != transform)
            transform.DOMove(switcherStartPos, moveDuration).SetEase(Ease.InOutSine);

        OnResetAnimation();
    }

    public virtual void OnResetAnimation() { }

    // Moves every current rider by moveOffset in lockstep with the elevator,
    // taking control of each one for the duration of the tween.
    private void CarryRiders(Vector3 moveOffset)
    {
        foreach (IExternallyControllable rider in riders)
        {
            if (rider.IsExternallyControlled) continue;
            rider.BeginExternalControl();
            rider.Transform.DOMove(rider.Transform.position + moveOffset, moveDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(rider.EndExternalControl);
        }
    }
}
