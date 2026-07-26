using System.Collections;
using UnityEngine;

public class BridgeTrigger : MonoBehaviour
{
    [SerializeField] private GameObject firstHinge;
    [SerializeField] private GameObject secondHinge;
    [SerializeField] private float delayBetweenHinges = 5f;

    private bool isTriggered;

    void OnTriggerEnter(Collider other)
    {
        if (isTriggered || other.GetComponent<Player>() == null) return;
        isTriggered = true;
        StartCoroutine(CollapseSequence());
    }

    private IEnumerator CollapseSequence()
    {
        Destroy(firstHinge.GetComponents<HingeJoint>()[1]);

        yield return new WaitForSeconds(delayBetweenHinges);

        Destroy(secondHinge.GetComponents<HingeJoint>()[0]);
    }
}
