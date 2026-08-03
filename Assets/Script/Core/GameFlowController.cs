using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameFlowController : MonoBehaviour
{
    private const float LevelRegistrationTimeout = 15f;
    private const float MinimumExitOverlap = 0.2f;

    private enum Destination
    {
        Preview,
        Gameplay
    }

    [Header("GameShell")]
    [SerializeField] private LevelCatalog catalog;
    [SerializeField] private Player player;
    [SerializeField] private Camera mainCamera;

    [Header("Timing")]
    [SerializeField] private float incomingOverlapDelay = 1f;
    [SerializeField] private float cameraMoveDuration = 0.6f;
    [SerializeField] private Ease cameraEase = Ease.InOutSine;

    public static GameFlowController Instance { get; private set; }

    public event Action<GameFlowState> StateChanged;
    public event Action<LevelDefinition> PreviewChanged;
    public event Action<string> FlowFailed;

    public GameFlowState State { get; private set; } = GameFlowState.Uninitialized;
    public LevelCatalog Catalog => catalog;
    public LevelContext CurrentLevel => currentLevel;
    public LevelDefinition SelectedLevel => catalog != null ? catalog.FindLevel(selectedLevelId) : null;
    public string TransitionPhase { get; private set; } = "Idle";
    public bool IsPlaying => State == GameFlowState.Playing;
    public bool IsMenuVisible =>
        State == GameFlowState.PreviewLoading ||
        State == GameFlowState.PreviewReady ||
        State == GameFlowState.ReturningToMenu;

    private LevelContext currentLevel;
    private LevelContext pendingContext;
    private string pendingSceneName;
    private string selectedLevelId;
    private string queuedLevelId;
    private Coroutine transitionRoutine;
    private bool flowControlHeld;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Menu previews and transition coroutines must finish even when the
        // standalone game window or Unity Editor is temporarily unfocused.
        Application.runInBackground = true;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Loads the persisted or highest-unlocked level as the initial live preview.</summary>
    public void Initialize()
    {
        if (State != GameFlowState.Uninitialized || catalog == null || player == null || mainCamera == null)
            return;

        Time.timeScale = 0f;
        EnsureFlowControl();

        LevelDefinition initial = catalog.FindLevel(LevelProgress.Instance?.GetLastSelectedLevelId());
        if (initial == null ||
            (LevelProgress.Instance != null &&
             !LevelProgress.Instance.IsUnlocked(initial.ChapterNumber, initial.LevelNumber)))
        {
            initial = catalog.FindHighestUnlocked(LevelProgress.Instance);
        }

        if (initial == null)
        {
            Fail("LevelCatalog contains no level that can be previewed.");
            return;
        }

        selectedLevelId = initial.LevelId;
        transitionRoutine = StartCoroutine(TransitionTo(initial, Destination.Preview, true));
    }

    /// <summary>Queues an unlocked level for live preview; the last rapid selection wins.</summary>
    public void SelectLevel(string levelId)
    {
        LevelDefinition target = catalog != null ? catalog.FindLevel(levelId) : null;
        if (target == null) return;
        if (LevelProgress.Instance != null &&
            !LevelProgress.Instance.IsUnlocked(target.ChapterNumber, target.LevelNumber))
            return;

        selectedLevelId = target.LevelId;
        LevelProgress.Instance?.SetLastSelectedLevelId(selectedLevelId);

        if (transitionRoutine != null)
        {
            queuedLevelId = selectedLevelId;
            return;
        }

        if (currentLevel != null && currentLevel.LevelId == selectedLevelId &&
            State == GameFlowState.PreviewReady)
        {
            PreviewChanged?.Invoke(target);
            return;
        }

        transitionRoutine = StartCoroutine(TransitionTo(target, Destination.Preview, false));
    }

    /// <summary>Leaves preview mode without loading another scene.</summary>
    public void StartSelectedLevel()
    {
        if (State != GameFlowState.PreviewReady || currentLevel == null) return;
        StartCoroutine(BeginGameplay());
    }

    /// <summary>Handles a qualified SceneSwitcher completion from the active LevelContext.</summary>
    public void CompleteLevel(LevelContext source)
    {
        if (State != GameFlowState.Playing || source == null || source != currentLevel) return;

        LevelDefinition completed = catalog.FindLevel(source.LevelId);
        if (completed == null)
        {
            Fail($"Completed level '{source.LevelId}' is missing from LevelCatalog.");
            return;
        }

        LevelProgress.Instance?.RegisterCompletion(completed.ChapterNumber, completed.LevelNumber);
        LevelDefinition next = catalog.FindNext(completed);
        if (next == null)
        {
            SetState(GameFlowState.ReturningToMenu);
            StartTransition(completed, Destination.Preview);
            return;
        }

        selectedLevelId = next.LevelId;
        LevelProgress.Instance?.SetLastSelectedLevelId(selectedLevelId);
        StartTransition(next, Destination.Gameplay);
    }

    /// <summary>Recreates the current level and returns to gameplay.</summary>
    public void ResetCurrentLevel()
    {
        if (currentLevel == null || transitionRoutine != null) return;
        LevelDefinition current = catalog.FindLevel(currentLevel.LevelId);
        if (current != null) StartTransition(current, Destination.Gameplay);
    }

    /// <summary>Discards the run and recreates the current level as a clean menu preview.</summary>
    public void ReturnToMenu()
    {
        if (currentLevel == null || transitionRoutine != null) return;
        LevelDefinition current = catalog.FindLevel(currentLevel.LevelId);
        if (current == null) return;

        selectedLevelId = current.LevelId;
        SetState(GameFlowState.ReturningToMenu);
        StartTransition(current, Destination.Preview);
    }

    /// <summary>Freezes an active run and exposes the pause state.</summary>
    public bool PauseGameplay()
    {
        if (State != GameFlowState.Playing) return false;
        Time.timeScale = 0f;
        SetState(GameFlowState.Paused);
        return true;
    }

    /// <summary>Resumes a paused run.</summary>
    public bool ResumeGameplay()
    {
        if (State != GameFlowState.Paused) return false;
        Time.timeScale = 1f;
        SetState(GameFlowState.Playing);
        return true;
    }

    // Called synchronously from LevelContext.Awake, before mechanism Start methods
    // cache world-space positions.
    public void RegisterLevelContextAwake(LevelContext context)
    {
        if (context == null || string.IsNullOrEmpty(pendingSceneName)) return;
        if (context.gameObject.scene.name != pendingSceneName) return;

        context.AlignSpawnTo(player.transform.position);
        context.ConfigureRuntimePlayer(player);
        pendingContext = context;
    }

    private void StartTransition(LevelDefinition target, Destination destination)
    {
        if (transitionRoutine != null)
        {
            queuedLevelId = target.LevelId;
            return;
        }

        transitionRoutine = StartCoroutine(TransitionTo(target, destination, false));
    }

    private IEnumerator BeginGameplay()
    {
        SetState(GameFlowState.StartingGameplay);
        PlayCameraTween(currentLevel, false);
        yield return WaitRealtime(cameraMoveDuration);

        Physics.SyncTransforms();
        Time.timeScale = 1f;
        ReleaseFlowControl();
        StepCounter.Instance?.BeginLevel();
        SetState(GameFlowState.Playing);
    }

    private IEnumerator TransitionTo(LevelDefinition target, Destination destination, bool initial)
    {
        TransitionPhase = "Preparing";
        bool wasGameplay = State == GameFlowState.Playing || State == GameFlowState.Paused;
        Time.timeScale = 0f;
        EnsureFlowControl();

        if (destination == Destination.Preview)
            SetState(initial ? GameFlowState.PreviewLoading : GameFlowState.Transitioning);
        else
            SetState(GameFlowState.Transitioning);

        LevelContext outgoing = currentLevel;
        float exitDeadline = Time.realtimeSinceStartup;
        if (outgoing != null)
        {
            float minimumExitDuration = incomingOverlapDelay + MinimumExitOverlap;
            float exitDuration =
                outgoing.TransitionAnimator.GetTransitionDuration(
                    player.transform.position,
                    false,
                    minimumExitDuration);
            exitDeadline += exitDuration;
            outgoing.TransitionAnimator.PlayExit(
                player.transform.position,
                minimumExitDuration);
        }

        bool reloadSameScene = outgoing != null && outgoing.gameObject.scene.name == target.SceneName;
        AsyncOperation loadOperation = null;
        float transitionStart = Time.realtimeSinceStartup;

        if (!reloadSameScene)
        {
            TransitionPhase = "Loading additive scene";
            loadOperation = BeginAdditiveLoad(target);
        }

        if (reloadSameScene)
        {
            TransitionPhase = "Waiting for same-scene exit";
            while (Time.realtimeSinceStartup < exitDeadline) yield return null;
            if (outgoing != null)
            {
                Scene sceneToUnload = outgoing.gameObject.scene;
                SceneManager.UnloadSceneAsync(sceneToUnload);
                while (sceneToUnload.isLoaded) yield return null;
                currentLevel = null;
            }

            loadOperation = BeginAdditiveLoad(target);
        }

        if (loadOperation == null)
        {
            RestoreAfterFailure(outgoing, wasGameplay, $"Could not start loading '{target.SceneName}'.");
            yield break;
        }

        // Awake normally supplies pendingContext; the loaded-scene lookup is
        // a defensive fallback for lifecycle ordering.
        TransitionPhase = "Waiting for LevelContext.Awake";
        float registrationDeadline = Time.realtimeSinceStartup + LevelRegistrationTimeout;
        while (pendingContext == null && Time.realtimeSinceStartup < registrationDeadline)
        {
            pendingContext = FindLevelContext(target.SceneName);
            if (pendingContext != null) break;
            yield return null;
        }

        if (pendingContext == null)
        {
            Scene invalidScene = SceneManager.GetSceneByName(target.SceneName);
            if (invalidScene.IsValid() && invalidScene.isLoaded)
                SceneManager.UnloadSceneAsync(invalidScene);

            RestoreAfterFailure(
                outgoing,
                wasGameplay,
                $"Scene '{target.SceneName}' did not register a LevelContext.");
            yield break;
        }

        yield return null;

        TransitionPhase = "Waiting for entry overlap";
        float overlapRemaining = incomingOverlapDelay - (Time.realtimeSinceStartup - transitionStart);
        if (!initial && !reloadSameScene && overlapRemaining > 0f)
            yield return WaitRealtime(overlapRemaining);

        LevelContext incoming = pendingContext;
        pendingContext = null;
        pendingSceneName = null;

        // Idempotent safeguard for scene-lifecycle ordering: when Awake
        // already aligned the spawn the delta is zero.
        incoming.AlignSpawnTo(player.transform.position);
        incoming.ConfigureRuntimePlayer(player);
        player.PrepareForLevel(player.transform.position, incoming.LevelSpawn.rotation, incoming.KillPlaneY);
        flowControlHeld = false;
        EnsureFlowControl();

        float enterDuration =
            incoming.TransitionAnimator.GetTransitionDuration(player.transform.position, true);
        incoming.TransitionAnimator.PlayEnter(player.transform.position);
        PlayCameraTween(incoming, destination == Destination.Preview);
        float entryDeadline = Time.realtimeSinceStartup +
            Mathf.Max(enterDuration, cameraMoveDuration);

        TransitionPhase = "Waiting for outgoing exit";
        while (Time.realtimeSinceStartup < exitDeadline) yield return null;

        if (!reloadSameScene && outgoing != null)
        {
            Scene sceneToUnload = outgoing.gameObject.scene;
            SceneManager.UnloadSceneAsync(sceneToUnload);
            while (sceneToUnload.isLoaded) yield return null;
            currentLevel = null;
        }

        TransitionPhase = "Waiting for incoming entry";
        while (Time.realtimeSinceStartup < entryDeadline) yield return null;

        TransitionPhase = "Finalizing";
        Physics.SyncTransforms();
        currentLevel = incoming;
        selectedLevelId = target.LevelId;
        LevelProgress.Instance?.SetLastSelectedLevelId(selectedLevelId);

        if (destination == Destination.Gameplay)
        {
            Time.timeScale = 1f;
            ReleaseFlowControl();
            StepCounter.Instance?.BeginLevel();
            SetState(GameFlowState.Playing);
        }
        else
        {
            Time.timeScale = 0f;
            SetState(GameFlowState.PreviewReady);
            PreviewChanged?.Invoke(target);
        }

        transitionRoutine = null;
        TransitionPhase = "Idle";

        if (!string.IsNullOrEmpty(queuedLevelId) && queuedLevelId != selectedLevelId)
        {
            string queued = queuedLevelId;
            queuedLevelId = null;
            SelectLevel(queued);
        }
        else
        {
            queuedLevelId = null;
        }
    }

    private AsyncOperation BeginAdditiveLoad(LevelDefinition target)
    {
        pendingContext = null;
        pendingSceneName = target.SceneName;
        return SceneManager.LoadSceneAsync(target.SceneName, LoadSceneMode.Additive);
    }

    private static LevelContext FindLevelContext(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            LevelContext context = root.GetComponentInChildren<LevelContext>(true);
            if (context != null) return context;
        }

        return null;
    }

    private void PlayCameraTween(LevelContext context, bool preview)
    {
        Vector3 position = preview ? context.PreviewCameraPosition : context.GameplayCameraPosition;
        float size = preview ? context.PreviewOrthographicSize : context.GameplayOrthographicSize;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(mainCamera.transform.DOMove(position, cameraMoveDuration).SetEase(cameraEase).SetUpdate(true));
        sequence.Join(mainCamera.transform.DORotateQuaternion(context.CameraRotation, cameraMoveDuration)
            .SetEase(cameraEase).SetUpdate(true));
        sequence.Join(DOTween.To(
                () => mainCamera.orthographicSize,
                value => mainCamera.orthographicSize = value,
                size,
                cameraMoveDuration)
            .SetEase(cameraEase)
            .SetUpdate(true));
    }

    private void EnsureFlowControl()
    {
        if (flowControlHeld || player == null) return;
        player.BeginExternalControl();
        flowControlHeld = true;
    }

    private void ReleaseFlowControl()
    {
        if (!flowControlHeld || player == null) return;
        player.EndExternalControl();
        flowControlHeld = false;
    }

    private void RestoreAfterFailure(LevelContext outgoing, bool wasGameplay, string message)
    {
        TransitionPhase = "Restoring outgoing level";
        pendingContext = null;
        pendingSceneName = null;
        transitionRoutine = null;

        if (outgoing != null)
        {
            outgoing.TransitionAnimator.PlayEnter(player.transform.position, () =>
            {
                TransitionPhase = "Idle";
                currentLevel = outgoing;
                Physics.SyncTransforms();
                if (wasGameplay)
                {
                    Time.timeScale = 1f;
                    ReleaseFlowControl();
                    SetState(GameFlowState.Playing);
                }
                else
                {
                    Time.timeScale = 0f;
                    SetState(GameFlowState.PreviewReady);
                }
            });
        }
        else
        {
            TransitionPhase = "Error";
            Fail(message);
        }

        Debug.LogError($"[{nameof(GameFlowController)}] {message}", this);
        FlowFailed?.Invoke(message);
    }

    private static IEnumerator WaitRealtime(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSecondsRealtime(seconds);
    }

    private void SetState(GameFlowState newState)
    {
        State = newState;
        StepCounter.Instance?.SetVisible(
            newState == GameFlowState.Playing || newState == GameFlowState.Paused);
        StateChanged?.Invoke(newState);
    }

    private void Fail(string message)
    {
        Time.timeScale = 0f;
        SetState(GameFlowState.Error);
        Debug.LogError($"[{nameof(GameFlowController)}] {message}", this);
        FlowFailed?.Invoke(message);
    }
}
