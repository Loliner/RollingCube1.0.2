using System.Collections;
using UnityEngine;

// A standalone trigger that starts a specific Elevator's movement from a
// different location than the elevator itself (e.g. a floor switch/button).
// The player must stand on it for holdDuration before it fires; stepping off
// early cancels the pending trigger.
public class ElevatorSwitch : MonoBehaviour
{
    [SerializeField] private Elevator elevator;
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
        elevator.TriggerMove();
    }
}
