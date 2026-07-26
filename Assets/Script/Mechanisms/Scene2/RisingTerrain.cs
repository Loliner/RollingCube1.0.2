using System.Collections;
using DG.Tweening;
using UnityEngine;

public class RisingTerrain : MonoBehaviour
{
    [SerializeField] private GameObject[] terrainSteps;
    [SerializeField] private float riseHeight = 1.5f;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private float stepDelay = 0.5f;

    private bool isTriggered;

    void OnTriggerEnter(Collider other)
    {
        if (isTriggered || other.GetComponent<Player>() == null) return;
        isTriggered = true;
        StartCoroutine(RiseSequence());
    }

    private IEnumerator RiseSequence()
    {
        for (int i = 0; i < terrainSteps.Length; i++)
        {
            GameObject step = terrainSteps[i];
            step.transform.DOMove(step.transform.position + Vector3.up * riseHeight, moveDuration)
                .SetEase(Ease.InOutSine);

            if (i < terrainSteps.Length - 1)
                yield return new WaitForSeconds(stepDelay);
        }
    }
}
