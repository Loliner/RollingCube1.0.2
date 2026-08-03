using UnityEngine;

public sealed class LevelContext : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string levelId;
    [SerializeField] private int chapterNumber;
    [SerializeField] private int levelNumber;

    [Header("Runtime Content")]
    [SerializeField] private Transform levelRoot;
    [SerializeField] private Transform levelSpawn;
    [SerializeField] private LevelEntryAnimator transitionAnimator;
    [SerializeField] private Player standalonePlayerPrefab;
    [SerializeField] private GameObject standaloneRig;

    [Header("Camera Framing")]
    [SerializeField] private Vector3 gameplayCameraOffset = new Vector3(-3.73f, 4.23f, -7.05f);
    [SerializeField] private Vector3 previewCameraOffset = new Vector3(-6.5f, 4.23f, -7.05f);
    [SerializeField] private Vector3 cameraEulerAngles = new Vector3(30f, 28f, 0f);
    [SerializeField] private float gameplayOrthographicSize = 5f;
    [SerializeField] private float previewOrthographicSize = 5.5f;
    [SerializeField] private float killPlaneDistance = 5.5f;

    private Player runtimePlayer;
    private bool standalone;

    public string LevelId => levelId;
    public int ChapterNumber => chapterNumber;
    public int LevelNumber => levelNumber;
    public Transform LevelRoot => levelRoot;
    public Transform LevelSpawn => levelSpawn;
    public LevelEntryAnimator TransitionAnimator => transitionAnimator;
    public Player RuntimePlayer => runtimePlayer;
    public bool IsStandalone => standalone;
    public Vector3 GameplayCameraPosition => levelSpawn.position + gameplayCameraOffset;
    public Vector3 PreviewCameraPosition => levelSpawn.position + previewCameraOffset;
    public Quaternion CameraRotation => Quaternion.Euler(cameraEulerAngles);
    public float GameplayOrthographicSize => gameplayOrthographicSize;
    public float PreviewOrthographicSize => previewOrthographicSize;
    public float KillPlaneY => levelSpawn.position.y - killPlaneDistance;

    void Awake()
    {
        if (GameFlowController.Instance != null)
        {
            if (standaloneRig != null) standaloneRig.SetActive(false);
            transitionAnimator.PrepareEnterState();
            GameFlowController.Instance.RegisterLevelContextAwake(this);
            return;
        }

        standalone = true;
        if (standaloneRig != null) standaloneRig.SetActive(true);
        transitionAnimator.PrepareEnterState();

        if (standalonePlayerPrefab == null)
        {
            Debug.LogError($"[{nameof(LevelContext)}] {name} has no standalone Player prefab.", this);
            return;
        }

        runtimePlayer = Instantiate(standalonePlayerPrefab, levelSpawn.position, levelSpawn.rotation);
        runtimePlayer.name = "Player";
        transitionAnimator.Configure(runtimePlayer);
    }

    void Start()
    {
        if (!standalone || runtimePlayer == null) return;

        runtimePlayer.PrepareForLevel(levelSpawn.position, levelSpawn.rotation, KillPlaneY);
        runtimePlayer.BeginExternalControl();
        transitionAnimator.PlayEnter(runtimePlayer.transform.position);
        StartCoroutine(FinishStandaloneEntry(
            transitionAnimator.GetTransitionDuration(runtimePlayer.transform.position, true)));
    }

    private System.Collections.IEnumerator FinishStandaloneEntry(float duration)
    {
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);
        Physics.SyncTransforms();
        runtimePlayer.EndExternalControl();
    }

    /// <summary>Moves the complete level so its spawn aligns with a world-space anchor.</summary>
    public void AlignSpawnTo(Vector3 anchor)
    {
        if (levelRoot == null || levelSpawn == null) return;
        levelRoot.position += anchor - levelSpawn.position;
    }

    /// <summary>Injects the persistent player used by GameShell flow.</summary>
    public void ConfigureRuntimePlayer(Player player)
    {
        runtimePlayer = player;
        transitionAnimator.Configure(player);
    }

#if UNITY_EDITOR
    public void Configure(
        string id,
        int chapter,
        int level,
        Transform root,
        Transform spawn,
        LevelEntryAnimator animator,
        Player playerPrefab,
        GameObject rig,
        Vector3 gameplayOffset,
        Vector3 previewOffset,
        Vector3 cameraEuler,
        float gameplaySize,
        float previewSize)
    {
        levelId = id;
        chapterNumber = chapter;
        levelNumber = level;
        levelRoot = root;
        levelSpawn = spawn;
        transitionAnimator = animator;
        standalonePlayerPrefab = playerPrefab;
        standaloneRig = rig;
        gameplayCameraOffset = gameplayOffset;
        previewCameraOffset = previewOffset;
        cameraEulerAngles = cameraEuler;
        gameplayOrthographicSize = gameplaySize;
        previewOrthographicSize = previewSize;
    }
#endif
}
