using System.Collections;
using UnityEngine;

// 独立的电梯触发开关，支持：
// 1. 触发一个或多个 Elevator 移动，每个 Elevator 可以单独设置延迟（targets）；
// 2. 自己只负责调用 TriggerMove() 发起触发，移动、驮载、reset 等后续行为完全交给
//    Elevator 自己处理。
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

    private Coroutine pending;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;
        pending = StartCoroutine(TriggerAfterDelay());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;
        if (pending != null) StopCoroutine(pending);
        pending = null;
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
