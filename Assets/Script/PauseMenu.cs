using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The project's first interactive UI: a global pause overlay toggled by Esc.
// Bootstrapped the same way as StepCounter (RuntimeInitializeOnLoadMethod +
// DontDestroyOnLoad) so it exists no matter which scene is opened and played
// directly. See design/gdd/ui-pause-menu.md for the full design.
public class PauseMenu : MonoBehaviour
{
    private const string CanvasPrefabResourcePath = "PauseMenuCanvas";
    private const string MainMenuSceneName = "MainMenu";

    public static PauseMenu Instance { get; private set; }
    public bool IsPaused { get; private set; }

    private GameObject panel;

    [RuntimeInitializeOnLoadMethod]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject host = new GameObject("PauseMenu");
        Instance = host.AddComponent<PauseMenu>();
        DontDestroyOnLoad(host);
    }

    void Awake()
    {
        GameObject canvasPrefab = Resources.Load<GameObject>(CanvasPrefabResourcePath);
        GameObject canvasInstance = Instantiate(canvasPrefab);
        DontDestroyOnLoad(canvasInstance);

        panel = FindChild(canvasInstance.transform, "PausePanel").gameObject;
        panel.SetActive(false);

        FindChild(canvasInstance.transform, "ResumeButton").GetComponent<Button>().onClick.AddListener(Resume);
        FindChild(canvasInstance.transform, "ResetButton").GetComponent<Button>().onClick.AddListener(ResetLevel);
        FindChild(canvasInstance.transform, "MainMenuButton").GetComponent<Button>().onClick.AddListener(GoToMainMenu);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        if (IsPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        panel.SetActive(true);
    }

    private void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        panel.SetActive(false);
    }

    private void ResetLevel()
    {
        Resume();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        Resume();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }
}
