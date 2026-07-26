using System.Collections;
using DG.Tweening;
using UnityEngine;

public class ConveyorLogic : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // cells per second; keep close to Player's roll speed
    [SerializeField] private bool isActive = true;
    [SerializeField] private Transform forwardPoint; // child transform pointing in the transport direction

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        Player player = other.GetComponent<Player>();
        if (player == null || player.IsExternallyControlled) return;

        StartCoroutine(ContinuousTransport(player));
    }

    private IEnumerator ContinuousTransport(Player player)
    {
        player.BeginExternalControl();

        GameObject currentConveyor = gameObject;
        while (currentConveyor != null)
        {
            ConveyorLogic logic = currentConveyor.GetComponent<ConveyorLogic>();
            if (logic == null || !logic.isActive || logic.forwardPoint == null)
                break;

            Vector3 direction = logic.forwardPoint.position - currentConveyor.transform.position;
            direction.y = 0f;
            direction.Normalize();

            Vector3 targetPos = currentConveyor.transform.position + direction;
            targetPos.y = player.transform.position.y;

            bool hopDone = false;
            player.transform.DOMove(targetPos, 1f / logic.moveSpeed)
                .SetEase(Ease.Linear)
                .OnComplete(() => hopDone = true);
            yield return new WaitUntil(() => hopDone);

            currentConveyor = GetNextConveyor(targetPos);
        }

        player.EndExternalControl();
    }

    private GameObject GetNextConveyor(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapBox(pos, new Vector3(0.1f, 0.1f, 0.1f));
        foreach (Collider col in colliders)
            if (col.GetComponent<ConveyorLogic>() != null) return col.gameObject;
        return null;
    }
}
