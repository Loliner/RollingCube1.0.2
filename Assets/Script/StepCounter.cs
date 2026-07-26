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
    private int stepCount;

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
        GameObject canvasInstance = Instantiate(canvasPrefab);
        DontDestroyOnLoad(canvasInstance);
        stepText = canvasInstance.GetComponentInChildren<Text>(true);

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateText();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        stepCount = 0;
        UpdateText();
    }

    // Called by Player.cs after each successful roll completes.
    public void RegisterStep()
    {
        stepCount++;
        UpdateText();
    }

    private void UpdateText()
    {
        if (stepText != null) stepText.text = $"Step: {stepCount}";
    }
}
