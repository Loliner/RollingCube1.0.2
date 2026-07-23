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
//    触发器调用 TriggerMove()/TriggerReset() 触发移动（见 ElevatorSwitch）。
// 5. TriggerMove()/TriggerReset() 在动画进行到一半时被对方调用也能正确响应——
//    直接从当前实际位置反向重新 tween，而不是必须等先到达终点/起点才能改变方向
//    （压力板场景下开关可能被反复按下/松开，动画不能只允许从静止状态开始）。
public class Elevator : MonoBehaviour
{
    private enum State { AtStart, MovingToTarget, AtTarget, MovingToStart }

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
    private State state = State.AtStart;
    private int moveGeneration; // bumped every time a new tween starts, so a delayed reset scheduled against a stale arrival can detect it's been superseded and skip
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

    void OnTriggerExit(Collider other)
    {
        IExternallyControllable rider = other.GetComponent<IExternallyControllable>();
        if (rider == null) return;
        riders.Remove(rider);

        if (resetOnArrival) return; // reset timing is driven by arrival, not by riders leaving
        if (state != State.AtTarget || riders.Count > 0) return; // wait until everyone's off (and we've actually arrived) before resetting
        StartCoroutine(ResetAfterDelay(resetDelay));
    }

    // Starts moving toward the target position (elevatorStartPos + offset).
    // Called from the elevator's own trigger (if selfTriggered) or externally
    // (e.g. ElevatorSwitch). Safe to call while a reset is still in flight
    // (MovingToStart) — reverses direction immediately from wherever the
    // elevator currently is. No-ops if already at/heading to the target.
    public void TriggerMove()
    {
        if (state == State.MovingToTarget || state == State.AtTarget) return;
        BeginMove(elevatorStartPos + offset, switcherStartPos + offset, offset, State.MovingToTarget, State.AtTarget);
        OnStartAnimation();

        if (resetOnArrival && reset)
            StartCoroutine(ResetAfterDelay(resetDelay));
    }

    public virtual void OnStartAnimation() { }

    // Starts moving back toward the start position. Callable externally (e.g.
    // a pressure-plate style ElevatorSwitch letting go) as well as internally
    // via the delayed reset paths. Safe to call while still moving toward the
    // target (MovingToTarget) — reverses immediately rather than waiting for
    // arrival. No-ops if this elevator can't reset (reset == false) or is
    // already at/heading to the start position.
    public void TriggerReset()
    {
        if (!reset) return;
        if (state == State.MovingToStart || state == State.AtStart) return;
        BeginMove(elevatorStartPos, switcherStartPos, -offset, State.MovingToStart, State.AtStart);
        OnResetAnimation();
    }

    public virtual void OnResetAnimation() { }

    private IEnumerator ResetAfterDelay(float delay)
    {
        int myGeneration = moveGeneration;
        yield return new WaitForSeconds(delay);
        if (myGeneration != moveGeneration) yield break; // a more recent TriggerMove/TriggerReset already superseded this wait
        TriggerReset();
    }

    // Kills whatever tween is currently playing on the elevator (and switcher,
    // if following) and starts a fresh one toward targetPos/targetSwitcherPos
    // from wherever they currently are, so a move can be redirected mid-flight
    // instead of only ever starting from a resting state.
    private void BeginMove(Vector3 targetPos, Vector3 targetSwitcherPos, Vector3 riderOffset, State movingState, State arrivedState)
    {
        moveGeneration++;
        int myGeneration = moveGeneration;
        state = movingState;

        elevator.transform.DOKill();
        elevator.transform.DOMove(targetPos, moveDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                if (myGeneration == moveGeneration) state = arrivedState;
            });

        if (switcherFollow && elevator.transform != transform)
        {
            transform.DOKill();
            transform.DOMove(targetSwitcherPos, moveDuration).SetEase(Ease.InOutSine);
        }

        CarryRiders(riderOffset);
    }

    // Moves every current rider by moveOffset in lockstep with the elevator,
    // taking control of each one for the duration of the tween.
    private void CarryRiders(Vector3 moveOffset)
    {
        foreach (IExternallyControllable rider in riders)
        {
            if (rider.IsExternallyControlled) continue;
            rider.BeginExternalControl();
            rider.Transform.DOKill();
            rider.Transform.DOMove(rider.Transform.position + moveOffset, moveDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(rider.EndExternalControl);
        }
    }
}
