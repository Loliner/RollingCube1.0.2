using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 独立的电梯触发压力板，支持：
// 1. 触发一个或多个 Elevator 移动，每个 Elevator 可以单独设置延迟（targets）；
// 2. 持续感应：玩家和木箱都能激活（显式检测这两种组件——是否能触发开关，跟
//    IExternallyControllable「能否被机关搬运」是两个不相关的语义，所以不复用
//    那个接口），只要还有至少一个在压着就保持触发状态，多个物体可以同时压住；
// 3. 最后一个物体离开时立即让目标电梯开始缩回（TriggerReset()），不等待、不
//    需要先到达终点——例如玩家踩满 holdDuration 后先走开，箱子还压着不会缩回；
//    等箱子也被推走，才会立刻回缩；
// 4. 自己只负责调用 TriggerMove()/TriggerReset()，移动、驮载、能否复位（reset）
//    等后续行为完全交给 Elevator 自己处理。
public class ElevatorSwitch : MonoBehaviour
{
    [System.Serializable]
    private struct Target
    {
        public Elevator elevator;
        public float delay; // extra seconds after holdDuration before this elevator moves
    }

    [SerializeField] private Target[] targets;
    [SerializeField] private float holdDuration = 1f;

    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private Coroutine pending;

    private static bool CanActivate(Collider other) =>
        other.GetComponent<Player>() != null || other.GetComponent<PushableBlock>() != null;

    void OnTriggerEnter(Collider other)
    {
        if (!CanActivate(other)) return;
        occupants.Add(other);
        if (occupants.Count == 1) pending = StartCoroutine(TriggerAfterDelay());
    }

    void OnTriggerExit(Collider other)
    {
        if (!CanActivate(other)) return;
        occupants.Remove(other);
        if (occupants.Count > 0) return;

        if (pending != null)
        {
            StopCoroutine(pending);
            pending = null;
            return; // left before holdDuration elapsed — never actually triggered, nothing to reset
        }

        foreach (Target target in targets)
            target.elevator.TriggerReset();
    }

    private IEnumerator TriggerAfterDelay()
    {
        yield return new WaitForSeconds(holdDuration);
        pending = null;

        foreach (Target target in targets)
            StartCoroutine(TriggerTarget(target));
    }

    private IEnumerator TriggerTarget(Target target)
    {
        if (target.delay > 0f) yield return new WaitForSeconds(target.delay);
        target.elevator.TriggerMove();
    }
}
