using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Elevators : MonoBehaviour
{
    [SerializeField] private GameObject[] elevators;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool reset;
    [SerializeField] private float resetDelay = 3f;
    [SerializeField] private float moveDuration = 2f;

    private Vector3[] startPositions;
    private bool isTriggered;

    void Start()
    {
        startPositions = new Vector3[elevators.Length];
        for (int i = 0; i < elevators.Length; i++)
            startPositions[i] = elevators[i].transform.position;
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
        foreach (GameObject el in elevators)
            el.transform.DOMove(el.transform.position + offset, moveDuration).SetEase(Ease.InOutSine);
        yield return null;
    }

    private IEnumerator ResetAnimation()
    {
        if (!reset) yield break;

        yield return new WaitForSeconds(resetDelay);

        for (int i = 0; i < elevators.Length; i++)
        {
            bool isLast = i == elevators.Length - 1;
            Tween tween = elevators[i].transform.DOMove(startPositions[i], moveDuration).SetEase(Ease.InOutSine);
            if (isLast) tween.OnComplete(() => isTriggered = false);
        }
    }
}
