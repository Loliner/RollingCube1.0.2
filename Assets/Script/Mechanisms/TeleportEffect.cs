using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class TeleportEffect : MonoBehaviour
{
    [SerializeField] private GameObject cubeModel; // normal-state cube model
    [SerializeField] private GameObject debrisPrefab; // prefab split into per-cell shatter pieces
    [SerializeField] private Transform targetPoint;

    [SerializeField] private float floatHeight = 2f;
    [SerializeField] private float layerDelay = 0.2f; // stagger between Y-layers
    [SerializeField] private float pieceRandomDelay = 0.15f; // jitter within a layer
    [SerializeField] private float disassemblePieceDuration = 0.8f;
    [SerializeField] private float reassemblePieceDuration = 0.8f;

    private bool isTeleporting;

    public void StartTeleport()
    {
        if (isTeleporting || targetPoint == null) return;
        isTeleporting = true;
        cubeModel.SetActive(false);

        PlayFloatingShatter();

        float totalWaitTime = (layerDelay * 2) + disassemblePieceDuration;
        DOVirtual.DelayedCall(totalWaitTime * 0.8f, () =>
        {
            transform.position = targetPoint.position;
            PlayRisingReassemble();
        });
    }

    private void PlayFloatingShatter()
    {
        GameObject debris = Instantiate(debrisPrefab, transform.position, transform.rotation);
        List<Transform> pieces = debris.transform.Cast<Transform>().ToList();

        var layers = pieces
            .GroupBy(p => Mathf.Round(p.position.y * 10f) / 10f)
            .OrderByDescending(g => g.Key)
            .ToList();

        for (int i = 0; i < layers.Count; i++)
        {
            float baseLayerDelay = i * layerDelay;
            List<Transform> currentLayerPieces = layers[i].ToList();
            ShuffleList(currentLayerPieces);

            float interval = pieceRandomDelay / currentLayerPieces.Count;
            for (int j = 0; j < currentLayerPieces.Count; j++)
            {
                Transform child = currentLayerPieces[j];
                float jitter = Random.Range(0f, interval * 0.5f);
                float finalDelay = baseLayerDelay + (j * interval) + jitter;

                Vector3 targetWorldPos = child.position + Vector3.up * floatHeight;
                child.DOMove(targetWorldPos, disassemblePieceDuration).SetDelay(finalDelay).SetEase(Ease.OutBack);

                Vector3 tiltRotation = new Vector3(
                    Random.Range(-35f, 35f),
                    Random.Range(-10f, 10f),
                    Random.Range(-35f, 35f));
                child.DORotate(tiltRotation, disassemblePieceDuration, RotateMode.LocalAxisAdd)
                    .SetDelay(finalDelay).SetEase(Ease.OutSine);

                child.DOScale(Vector3.zero, disassemblePieceDuration).SetDelay(finalDelay).SetEase(Ease.OutSine);
            }
        }

        float maxAnimTime = (layers.Count * layerDelay) + disassemblePieceDuration;
        Destroy(debris, maxAnimTime + 1f);
    }

    private void PlayRisingReassemble()
    {
        GameObject debris = Instantiate(debrisPrefab, transform.position, transform.rotation);
        List<Transform> pieces = debris.transform.Cast<Transform>().ToList();

        var layers = pieces
            .GroupBy(p => Mathf.Round(p.position.y * 10f) / 10f)
            .OrderBy(g => g.Key)
            .ToList();

        for (int i = 0; i < layers.Count; i++)
        {
            float baseLayerDelay = i * layerDelay;
            foreach (Transform child in layers[i])
            {
                Vector3 finalWorldPos = child.position;
                Quaternion finalRotation = child.rotation;

                child.position = finalWorldPos + Vector3.up * floatHeight;
                child.localScale = Vector3.zero;
                child.localRotation = Quaternion.Euler(
                    Random.Range(-35f, 35f),
                    Random.Range(-10f, 10f),
                    Random.Range(-35f, 35f));

                float finalDelay = baseLayerDelay + Random.Range(0f, pieceRandomDelay);

                child.DOMove(finalWorldPos, reassemblePieceDuration).SetDelay(finalDelay).SetEase(Ease.OutBack);
                child.DORotateQuaternion(finalRotation, reassemblePieceDuration).SetDelay(finalDelay).SetEase(Ease.OutSine);
                child.DOScale(Vector3.one, reassemblePieceDuration).SetDelay(finalDelay).SetEase(Ease.OutSine);
            }
        }

        float maxAnimTime = (layers.Count * layerDelay) + reassemblePieceDuration + 1f;
        DOVirtual.DelayedCall(maxAnimTime, () =>
        {
            cubeModel.transform.position = targetPoint.position;
            cubeModel.SetActive(true);
            Destroy(debris);
            isTeleporting = false;
        });
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }
}
