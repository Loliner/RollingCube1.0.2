using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 可移动到任意位置的电梯开关，支持：
// 1. 触发后移动到固定位置（elevatorStartPos + offset）；
// 2. 携带站在它上面的物体（玩家或木箱）一起移动（riders）；
// 3. 物体离开后可回到最初位置，也可以不回（reset 开关控制，配合 resetDelay）；
//    复位的计时方式可选：等所有人离开触发器后才开始计时（默认），或者一到达
//    目标位置就立刻开始计时，不管上面是否还站着人（resetOnArrival），后者会把
//    还在平台上的物体一并带回起点。
// 4. 既支持通过自身 trigger 触发移动（selfTriggered=true），也支持由外部单独的
//    触发器调用 TriggerMove() 触发移动（见 ElevatorSwitch）。
public class Elevator : MonoBehaviour
{
    [SerializeField] private GameObject elevator;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool reset;
    [SerializeField] private float resetDelay = 3f;
    [SerializeField] private bool resetOnArrival; // false: reset timer starts once all riders leave the trigger (default). true: reset timer starts immediately on arrival, regardless of riders, and carries back whoever is still on board.
    [SerializeField] private bool switcherFollow;
    [SerializeField] protected float moveDuration = 2f;
    [SerializeField] private bool selfTriggered = true; // whether stepping onto the elevator's own trigger starts movement

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

        if (selfTriggered) TriggerMove();
    }

    // Starts the elevator's move-to-offset animation. Called from the elevator's
    // own trigger (if selfTriggered) or externally (e.g. a separate switch/button
    // mechanism holding a reference to this elevator). No-ops if already moving.
    public void TriggerMove()
    {
        if (isTriggered) return;
        StartCoroutine(StartAnimation());
    }

    void OnTriggerExit(Collider other)
    {
        IExternallyControllable rider = other.GetComponent<IExternallyControllable>();
        if (rider == null) return;
        riders.Remove(rider);

        if (resetOnArrival) return; // reset timing is driven by arrival, not by riders leaving
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

        if (resetOnArrival)
        {
            yield return new WaitForSeconds(moveDuration);
            StartCoroutine(ResetAnimation());
        }
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

        CarryRiders(-offset); // no-op if everyone already left (exit-triggered reset); brings back anyone still riding (arrival-triggered reset)

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
