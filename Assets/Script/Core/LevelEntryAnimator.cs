using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Bidirectional level ripple. Every eligible direct child of LevelRoot is one
// transition unit, so terrain tiles ripple independently while a complex
// mechanism and its nested parts remain a single visual object.
public sealed class LevelEntryAnimator : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private float secondsPerGridUnit = 0.1f;
    [SerializeField] private float enterDuration = 0.35f;
    [SerializeField] private float exitDuration = 0.35f;
    [SerializeField] private Ease enterEase = Ease.OutBack;
    [SerializeField] private Ease exitEase = Ease.InBack;

    private struct Target
    {
        public Transform Transform;
        public Vector3 OriginalScale;
    }

    private readonly List<Target> targets = new List<Target>();
    private Sequence activeSequence;
    private bool captured;

    public bool IsAnimating => activeSequence != null && activeSequence.IsActive() && activeSequence.IsPlaying();

    /// <summary>Returns the real-time duration of a ripple from the supplied origin.</summary>
    public float GetTransitionDuration(
        Vector3 origin,
        bool entering,
        float minimumTotalDuration = 0f)
    {
        CaptureTargets();
        float itemDuration = entering ? enterDuration : exitDuration;
        float delayScale = GetDelayScale(origin, itemDuration, minimumTotalDuration);
        return GetMaximumRawDelay(origin) * delayScale + itemDuration;
    }

    /// <summary>Injects the player that owns the ripple origin.</summary>
    public void Configure(Player configuredPlayer)
    {
        player = configuredPlayer;
        CaptureTargets();
    }

    /// <summary>
    /// Hides all transition units before the first rendered frame of an
    /// additively loaded scene.
    /// </summary>
    public void PrepareEnterState()
    {
        CaptureTargets();
        KillActiveSequence();
        foreach (Target target in targets)
            if (target.Transform != null)
                target.Transform.localScale = Vector3.zero;
    }

    /// <summary>Plays the distance-staggered scale-in ripple using unscaled time.</summary>
    public void PlayEnter(Vector3 origin, Action onComplete = null)
    {
        CaptureTargets();
        KillActiveSequence();

        activeSequence = DOTween.Sequence().SetUpdate(true);
        foreach (Target target in targets)
        {
            if (target.Transform == null) continue;

            float delay = FlatDistance(target.Transform.position, origin) * secondsPerGridUnit;
            target.Transform.localScale = Vector3.zero;
            Tween tween = target.Transform
                .DOScale(target.OriginalScale, enterDuration)
                .SetEase(enterEase)
                .SetUpdate(true);
            activeSequence.Insert(delay, tween);
        }

        activeSequence.OnComplete(() =>
        {
            activeSequence = null;
            onComplete?.Invoke();
        });
    }

    /// <summary>Plays the distance-staggered scale-out ripple using unscaled time.</summary>
    public void PlayExit(Vector3 origin, Action onComplete = null)
    {
        PlayExit(origin, 0f, onComplete);
    }

    /// <summary>
    /// Plays the exit ripple and, when requested, stretches only the
    /// distance delays so the overall ripple lasts at least the supplied
    /// duration. Individual easing duration remains unchanged.
    /// </summary>
    public void PlayExit(
        Vector3 origin,
        float minimumTotalDuration,
        Action onComplete = null)
    {
        CaptureTargets();
        KillActiveSequence();
        float delayScale = GetDelayScale(origin, exitDuration, minimumTotalDuration);

        activeSequence = DOTween.Sequence().SetUpdate(true);
        foreach (Target target in targets)
        {
            if (target.Transform == null) continue;

            float delay =
                FlatDistance(target.Transform.position, origin) *
                secondsPerGridUnit *
                delayScale;
            target.Transform.localScale = target.OriginalScale;
            Tween tween = target.Transform
                .DOScale(Vector3.zero, exitDuration)
                .SetEase(exitEase)
                .SetUpdate(true);
            activeSequence.Insert(delay, tween);
        }

        activeSequence.OnComplete(() =>
        {
            activeSequence = null;
            onComplete?.Invoke();
        });
    }

    /// <summary>Immediately restores every transition target to its authored scale.</summary>
    public void SkipToComplete()
    {
        CaptureTargets();
        KillActiveSequence();
        foreach (Target target in targets)
            if (target.Transform != null)
                target.Transform.localScale = target.OriginalScale;

        if (player != null && player.IsExternallyControlled)
            player.EndExternalControl();
    }

    private void CaptureTargets()
    {
        if (captured) return;

        captured = true;
        targets.Clear();
        foreach (Transform child in transform)
        {
            if (child.GetComponent<LevelTransitionExclude>() != null) continue;
            targets.Add(new Target
            {
                Transform = child,
                OriginalScale = child.localScale
            });
        }
    }

    private void KillActiveSequence()
    {
        if (activeSequence == null) return;
        activeSequence.Kill();
        activeSequence = null;
    }

    private float GetDelayScale(
        Vector3 origin,
        float itemDuration,
        float minimumTotalDuration)
    {
        float maximumRawDelay = GetMaximumRawDelay(origin);
        float requiredDelay = Mathf.Max(0f, minimumTotalDuration - itemDuration);
        if (maximumRawDelay <= Mathf.Epsilon || requiredDelay <= maximumRawDelay)
            return 1f;

        return requiredDelay / maximumRawDelay;
    }

    private float GetMaximumRawDelay(Vector3 origin)
    {
        float maximumDelay = 0f;
        foreach (Target target in targets)
        {
            if (target.Transform == null) continue;
            maximumDelay = Mathf.Max(
                maximumDelay,
                FlatDistance(target.Transform.position, origin) * secondsPerGridUnit);
        }

        return maximumDelay;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector3 offset = a - b;
        offset.y = 0f;
        return offset.magnitude;
    }

    void OnDestroy()
    {
        KillActiveSequence();
    }
}
