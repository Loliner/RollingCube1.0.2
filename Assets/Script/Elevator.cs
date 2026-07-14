using System.Collections;
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

    void Start()
    {
        elevatorStartPos = elevator.transform.position;
        switcherStartPos = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isTriggered || other.GetComponent<Player>() == null) return;
        StartCoroutine(StartAnimation());
    }

    void OnTriggerExit(Collider other)
    {
        if (!isTriggered || other.GetComponent<Player>() == null) return;
        StartCoroutine(ResetAnimation());
    }

    private IEnumerator StartAnimation()
    {
        isTriggered = true;
        elevator.transform.DOMove(elevatorStartPos + offset, moveDuration).SetEase(Ease.InOutSine);
        if (switcherFollow && elevator.transform != transform)
            transform.DOMove(switcherStartPos + offset, moveDuration).SetEase(Ease.InOutSine);

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
}
