using System.Collections;
using UnityEngine;

public class FragileGround : MonoBehaviour
{
    [SerializeField] private Transform[] firstDropSegments;
    [SerializeField] private float exitToFallDelay = 1f;
    [SerializeField] private float fadeDuration = 1f;

    private bool isTriggered;

    void OnTriggerEnter(Collider other)
    {
        if (isTriggered || other.GetComponent<Player>() == null) return;
        isTriggered = true;

        foreach (Transform segment in firstDropSegments)
        {
            Rigidbody rb = segment.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            MeshCollider mc = segment.GetComponent<MeshCollider>();
            if (mc != null) mc.enabled = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!isTriggered || other.GetComponent<Player>() == null) return;

        foreach (BoxCollider box in GetComponents<BoxCollider>())
            box.enabled = false;

        foreach (Transform child in transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            MeshCollider mc = child.GetComponent<MeshCollider>();
            if (mc != null) mc.enabled = true;
        }

        StartCoroutine(FadeOutWithDelay());
    }

    private IEnumerator FadeOutWithDelay()
    {
        yield return new WaitForSeconds(exitToFallDelay);
        yield return FadeOutCoroutine();
    }

    private IEnumerator FadeOutCoroutine()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        Material[][] materials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].gameObject == gameObject) continue;
            materials[i] = renderers[i].materials; // instantiates per-renderer material copies
        }

        float time = 0f;
        while (time < fadeDuration)
        {
            SetAlpha(materials, Mathf.Lerp(1f, 0f, time / fadeDuration));
            time += Time.deltaTime;
            yield return null;
        }

        SetAlpha(materials, 0f);
        gameObject.SetActive(false);
    }

    private void SetAlpha(Material[][] materials, float alpha)
    {
        foreach (Material[] mats in materials)
        {
            if (mats == null) continue;
            foreach (Material mat in mats)
            {
                if (mat == null || !mat.HasProperty("_BaseColor")) continue;
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
        }
    }
}
