using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ConveyorBeltAni : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.5f;
    [SerializeField] private int beltMaterialIndex = 1; // material slot holding the scrolling belt texture

    private Material beltMaterial;

    void Start()
    {
        beltMaterial = GetComponent<Renderer>().materials[beltMaterialIndex];
    }

    void Update()
    {
        float offset = (Time.time * scrollSpeed) % 1f;
        beltMaterial.SetTextureOffset("_BaseMap", new Vector2(-offset, 0));
    }
}
