using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The start screen's navigation: a chapter-select root panel and a
// level-select panel, built procedurally at runtime on top of the existing
// MainMenuCanvas (Background/TitleText already authored in the scene).
// Locked levels are disabled buttons; unlock state comes from LevelProgress.
// Chapter/level count is hardcoded (only one chapter exists). See
// design/gdd/ui-start-screen.md for the full design.
[RequireComponent(typeof(Canvas))]
public class MainMenu : MonoBehaviour
{
    private const int ChapterNumber = 1;
    private const int LevelCount = 9;
    private const int LevelGridColumns = 3;
    private const float LevelButtonSpacing = 170f;

    private static readonly Color ButtonColor = new Color(0.25f, 0.35f, 0.5f);

    private GameObject chapterSelectPanel;
    private GameObject levelSelectPanel;
    private bool isLevelSelectActive;

    // LevelProgress.Instance is only guaranteed to exist once every object's
    // Awake has run (it's created by another script's own bootstrap), so
    // building panels that depend on it happens in Start(), not Awake().
    void Start()
    {
        Text titleText = FindTitleText();
        Font font = titleText.font;

        BuildChapterSelectPanel(titleText, font);
        BuildLevelSelectPanel(font);

        ShowChapterSelect();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (isLevelSelectActive) ShowChapterSelect();
    }

    private void BuildChapterSelectPanel(Text titleText, Font font)
    {
        chapterSelectPanel = CreatePanel("ChapterSelectPanel");
        titleText.transform.SetParent(chapterSelectPanel.transform, worldPositionStays: false);

        CreateButton(chapterSelectPanel.transform, "Chapter1Button", "章节 1", font,
            new Vector2(0f, -50f), new Vector2(320f, 90f), () => ShowLevelSelect());
    }

    private void BuildLevelSelectPanel(Font font)
    {
        levelSelectPanel = CreatePanel("LevelSelectPanel");
        levelSelectPanel.SetActive(false);

        CreateLabel(levelSelectPanel.transform, "ChapterTitleText", "章节 1 - 选关", font,
            new Vector2(0f, 300f), new Vector2(700f, 120f), 48);

        for (int level = 1; level <= LevelCount; level++)
        {
            int index = level - 1;
            int row = index / LevelGridColumns;
            int col = index % LevelGridColumns;
            Vector2 pos = new Vector2((col - 1) * LevelButtonSpacing, (1 - row) * LevelButtonSpacing);

            CreateButton(levelSelectPanel.transform, $"LevelButton{level}", level.ToString(), font,
                pos, new Vector2(140f, 140f), CaptureSelectLevel(level));
        }

        CreateButton(levelSelectPanel.transform, "BackButton", "返回", font,
            new Vector2(0f, -300f), new Vector2(260f, 70f), () => ShowChapterSelect());
    }

    private UnityAction CaptureSelectLevel(int level) => () => SelectLevel(level);

    private void ShowChapterSelect()
    {
        isLevelSelectActive = false;
        chapterSelectPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
    }

    private void ShowLevelSelect()
    {
        isLevelSelectActive = true;
        RefreshLevelLocks();
        chapterSelectPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    private void RefreshLevelLocks()
    {
        for (int level = 1; level <= LevelCount; level++)
        {
            Transform buttonT = levelSelectPanel.transform.Find($"LevelButton{level}");
            buttonT.GetComponent<Button>().interactable = LevelProgress.Instance.IsUnlocked(ChapterNumber, level);
        }
    }

    private void SelectLevel(int level)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene($"Chapter{ChapterNumber}_Scene{level}");
    }

    private Text FindTitleText()
    {
        foreach (Text text in GetComponentsInChildren<Text>(true))
            if (text.gameObject.name == "TitleText") return text;
        return null;
    }

    private GameObject CreatePanel(string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(transform, worldPositionStays: false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return panel;
    }

    private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 anchoredPosition, Vector2 sizeDelta, UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, worldPositionStays: false);

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        Image image = buttonObj.GetComponent<Image>();
        image.color = ButtonColor;

        Button button = buttonObj.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        CreateLabel(buttonObj.transform, "Label", label, font, Vector2.zero, Vector2.zero, 34, stretch: true);

        return button;
    }

    private static void CreateLabel(Transform parent, string name, string text, Font font, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, bool stretch = false)
    {
        GameObject labelObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObj.transform.SetParent(parent, worldPositionStays: false);

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        Text textComp = labelObj.GetComponent<Text>();
        textComp.font = font;
        textComp.fontSize = fontSize;
        textComp.fontStyle = FontStyle.Bold;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;
        textComp.text = text;
    }
}
