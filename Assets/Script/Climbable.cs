using UnityEngine;

public class Climbable : MonoBehaviour
{
    [SerializeField] private int heightUnits = 1;

    public int HeightUnits => heightUnits;
}
