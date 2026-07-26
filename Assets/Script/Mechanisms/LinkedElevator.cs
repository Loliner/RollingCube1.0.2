using DG.Tweening;
using UnityEngine;

public class LinkedElevator : Elevator
{
    [SerializeField] private GameObject linkedGameObject;
    [SerializeField] private Vector3 linkedOffset;

    public override void OnStartAnimation()
    {
        linkedGameObject.transform.DOMove(linkedGameObject.transform.position + linkedOffset, moveDuration)
            .SetEase(Ease.InOutSine);
    }
}
