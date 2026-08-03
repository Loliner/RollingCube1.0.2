using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Counts successful player rolls (TryMove -> AnimateRoll, see Player.cs) for
// the current level and shows "Step: n" top-left. Bootstrapped via
// RuntimeInitializeOnLoadMethod so it exists regardless of which scene is
// opened and played directly (this project's usual test workflow), and
// resets on every scene load since each scene is one level.
public class StepCounter : MonoBehaviour
{
    private const string CanvasPrefabResourcePath = "StepCounterCanvas";

    public static StepCounter Instance { get; private set; }

    private Text stepText;
    private Text deadText;
    private GameObject canvasInstance;
    private int stepCount;
    private int deathCount;

    [RuntimeInitializeOnLoadMethod]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject host = new GameObject("StepCounter");
        Instance = host.AddComponent<StepCounter>();
        DontDestroyOnLoad(host);
    }

    void Awake()
    {
        GameObject canvasPrefab = Resources.Load<GameObject>(CanvasPrefabResourcePath);
        canvasInstance = Instantiate(canvasPrefab);
        DontDestroyOnLoad(canvasInstance);
        Text[] texts = canvasInstance.GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text.gameObject.name == "StepText") stepText = text;
            else if (text.gameObject.name == "DeadText") deadText = text;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateText();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            BeginLevel();
    }

    /// <summary>Clears the counters when GameFlow begins a fresh level.</summary>
    public void BeginLevel()
    {
        stepCount = 0;
        deathCount = 0;
        UpdateText();
    }

    /// <summary>Shows the gameplay HUD only while a run is active.</summary>
    public void SetVisible(bool visible)
    {
        if (canvasInstance != null)
            canvasInstance.SetActive(visible);
    }

    // Called by Player.cs after each successful roll completes.
    public void RegisterStep()
    {
        stepCount++;
        UpdateText();
    }

    // Called by Player.cs when the player respawns after falling past the kill plane.
    public void RegisterDeath()
    {
        deathCount++;
        UpdateText();
    }

    private void UpdateText()
    {
        if (stepText != null) stepText.text = $"Step: {stepCount}";
        if (deadText != null) deadText.text = $"Dead: {deathCount}";
    }
}
