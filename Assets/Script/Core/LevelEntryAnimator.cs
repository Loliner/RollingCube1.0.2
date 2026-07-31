using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Plays the level's "ripple" entry: every direct child of this transform (terrain,
// obstacles, mechanisms, boxes, the goal) starts scaled to zero and pops in with a
// distance-staggered DOTween punch, rippling outward from the player's spawn position
// like an expanding wave. Player input stays locked (BeginExternalControl) for the whole
// sequence, since a tile's collider is only full-sized once its own pop-in tween
// completes — letting the player move earlier could roll them onto ground that hasn't
// materialized yet.
public class LevelEntryAnimator : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private float secondsPerGridUnit = 0.1f; // ripple speed: delay added per world unit of distance from the player
    [SerializeField] private float popDuration = 0.35f; // duration of each object's own scale-in tween
    [SerializeField] private Ease popEase = Ease.OutBack;

    private struct Target
    {
        public Transform transform;
        public Vector3 originalScale;
        public float delay;
    }

    private List<Target> targets;
    private Tween unlockCall;
    private bool skipped;

    void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("LevelEntryAnimator has no player reference; skipping ripple intro.", this);
            return;
        }

        CaptureTargets();
        Play();
    }

    private void CaptureTargets()
    {
        targets = new List<Target>(transform.childCount);
        Vector3 origin = player.transform.position;

        foreach (Transform child in transform)
        {
            Vector3 flatOffset = child.position - origin;
            flatOffset.y = 0f;
            float delay = flatOffset.magnitude * secondsPerGridUnit;

            targets.Add(new Target { transform = child, originalScale = child.localScale, delay = delay });
            child.localScale = Vector3.zero;
        }
    }

    private void Play()
    {
        player.BeginExternalControl();

        float maxDelay = 0f;
        foreach (Target target in targets)
        {
            target.transform.DOScale(target.originalScale, popDuration).SetDelay(target.delay).SetEase(popEase);
            if (target.delay > maxDelay) maxDelay = target.delay;
        }

        unlockCall = DOVirtual.DelayedCall(maxDelay + popDuration, Unlock);
    }

    private void Unlock()
    {
        if (skipped) return;
        player.EndExternalControl();
    }

    // Instantly finishes the ripple: every target snaps to its final scale and player
    // control is handed back right away. Called by automated tests so they don't have to
    // sit through the real animation, and so physics queries see fully-sized colliders.
    public void SkipToComplete()
    {
        if (targets == null || skipped) return;
        skipped = true;

        unlockCall?.Kill();
        foreach (Target target in targets)
        {
            DOTween.Kill(target.transform);
            target.transform.localScale = target.originalScale;
        }

        if (player.IsExternallyControlled)
            player.EndExternalControl();
    }
}
