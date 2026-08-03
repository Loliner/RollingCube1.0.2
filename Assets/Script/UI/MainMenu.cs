using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public sealed class MainMenu : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.07f, 0.18f, 0.25f, 0.72f);
    private static readonly Color CardColor = new Color(0.32f, 0.58f, 0.68f, 0.78f);
    private static readonly Color SelectedColor = new Color(0.82f, 0.94f, 0.82f, 0.95f);
    private static readonly Color LockedColor = new Color(0.23f, 0.34f, 0.4f, 0.72f);
    private static readonly Color AccentColor = new Color(0.44f, 0.72f, 0.34f, 1f);

    private const float MenuWidth = 700f;
    private const float FadeDuration = 0.3f;
    private const float PanelFadeDuration = 0.18f;
    private const string UiRoot = "UI/MainMenu/";

    private static readonly string[] ChapterTitles =
    {
        "春之平原",
        "冬之雪境",
        "夏之海岛",
        "秋之林地"
    };

    private static readonly string[] ChapterLabels =
    {
        "第一章",
        "第二章",
        "第三章",
        "第四章"
    };

    private static readonly string[] ChapterThumbnailPaths =
    {
        "shared_assets/chapter_thumbnails/chapter_thumbnail_spring",
        "shared_assets/chapter_thumbnails/chapter_thumbnail_winter",
        "shared_assets/chapter_thumbnails/chapter_thumbnail_summer",
        "shared_assets/chapter_thumbnails/chapter_thumbnail_autumn"
    };

    private readonly Dictionary<string, Button> levelButtons = new Dictionary<string, Button>();

    private GameFlowController flow;
    private CanvasGroup canvasGroup;
    private RectTransform menuContent;
    private GameObject chapterPanel;
    private GameObject levelPanel;
    private CanvasGroup chapterPanelGroup;
    private CanvasGroup levelPanelGroup;
    private Sequence panelTransition;
    private Button startButton;
    private Text selectionText;
    private Text loadingText;
    private Font font;
    private Sprite logoSprite;
    private Sprite unlockedChapterCardSprite;
    private Sprite lockedChapterCardSprite;
    private Sprite chapterThumbnailFrameSprite;
    private Sprite lockSprite;
    private Sprite arrowSprite;
    private Sprite starSprite;
    private Sprite progressPillSprite;
    private Sprite notificationDotSprite;
    private bool levelPanelVisible;
    private int visibleChapter;

    void Start()
    {
        flow = GameFlowController.Instance;
        if (flow == null)
        {
            Debug.LogError("MainMenu requires a GameFlowController in the GameShell scene.", this);
            return;
        }

        PrepareAuthoredCanvas();
        LoadVisualAssets();
        BuildLayout();

        flow.StateChanged += OnFlowStateChanged;
        flow.PreviewChanged += OnPreviewChanged;
        flow.FlowFailed += OnFlowFailed;

        ShowChapterSelect();
        flow.Initialize();
    }

    void OnDestroy()
    {
        if (canvasGroup != null) canvasGroup.DOKill();
        if (menuContent != null) menuContent.DOKill();
        panelTransition?.Kill();

        if (flow == null) return;
        flow.StateChanged -= OnFlowStateChanged;
        flow.PreviewChanged -= OnPreviewChanged;
        flow.FlowFailed -= OnFlowFailed;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
        if (!flow.IsMenuVisible || !levelPanelVisible) return;
        ShowChapterSelect();
    }

    private void PrepareAuthoredCanvas()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Text title = FindNamedComponent<Text>("TitleText");
        font = title != null ? title.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Image background = FindNamedComponent<Image>("Background");
        if (background != null) background.gameObject.SetActive(false);
        if (title != null) title.gameObject.SetActive(false);
    }

    private void LoadVisualAssets()
    {
        logoSprite = LoadSprite("shared_assets/branding/logo_rolling_cube");
        unlockedChapterCardSprite =
            LoadSprite("shared_assets/chapter_cards/chapter_card_unlocked_9slice");
        lockedChapterCardSprite =
            LoadSprite("shared_assets/chapter_cards/chapter_card_locked_9slice");
        chapterThumbnailFrameSprite =
            LoadSprite("shared_assets/chapter_cards/chapter_thumbnail_frame");
        lockSprite = LoadSprite("shared_assets/icons/icon_lock");
        arrowSprite = LoadSprite("shared_assets/icons/icon_arrow_right");
        starSprite = LoadSprite("shared_assets/icons/icon_star");
        progressPillSprite = LoadSprite("shared_assets/icons/progress_pill_empty");
        notificationDotSprite =
            LoadSprite("shared_assets/notifications/notification_dot");
    }

    private void BuildLayout()
    {
        GameObject content = new GameObject("MenuContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        content.transform.SetParent(transform, false);
        menuContent = content.GetComponent<RectTransform>();
        menuContent.anchorMin = new Vector2(0f, 0f);
        menuContent.anchorMax = new Vector2(0f, 1f);
        menuContent.pivot = new Vector2(0f, 0.5f);
        menuContent.anchoredPosition = Vector2.zero;
        menuContent.sizeDelta = new Vector2(MenuWidth, 0f);
        content.GetComponent<Image>().color = Color.clear;

        CreateImage(menuContent, "GameLogo", logoSprite, new Vector2(30f, -8f),
            new Vector2(450f, 315f), true);

        chapterPanel = CreatePanel(menuContent, "ChapterPanel");
        levelPanel = CreatePanel(menuContent, "LevelPanel");
        chapterPanelGroup = chapterPanel.GetComponent<CanvasGroup>();
        levelPanelGroup = levelPanel.GetComponent<CanvasGroup>();

        BuildChapterPanel();
        BuildLevelPanelShell();
        BuildBottomActions();
        BuildTopActions();
        CreateVersionLabel();
        SetPanelImmediate(chapterPanel, chapterPanelGroup, true);
        SetPanelImmediate(levelPanel, levelPanelGroup, false);
    }

    private void BuildChapterPanel()
    {
        ChapterDefinition playableChapter = null;
        foreach (ChapterDefinition chapter in flow.Catalog.Chapters)
            if (chapter.ChapterNumber == 1)
                playableChapter = chapter;

        for (int index = 0; index < ChapterTitles.Length; index++)
        {
            int chapterNumber = index + 1;
            bool unlocked = chapterNumber == 1 && playableChapter != null;
            CreateChapterCard(
                chapterNumber,
                ChapterTitles[index],
                LoadSprite(ChapterThumbnailPaths[index]),
                unlocked,
                unlocked ? playableChapter : null,
                new Vector2(55f, -274f - index * 118f));
        }
    }

    private void CreateChapterCard(
        int chapterNumber,
        string chapterTitle,
        Sprite thumbnail,
        bool unlocked,
        ChapterDefinition chapter,
        Vector2 position)
    {
        GameObject cardObject = new GameObject(
            $"Chapter{chapterNumber}Button",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        cardObject.transform.SetParent(chapterPanel.transform, false);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0f, 1f);
        cardRect.pivot = new Vector2(0f, 1f);
        cardRect.anchoredPosition = position;
        cardRect.sizeDelta = new Vector2(560f, 102f);

        Image cardImage = cardObject.GetComponent<Image>();
        cardImage.sprite = unlocked ? unlockedChapterCardSprite : lockedChapterCardSprite;
        cardImage.color = Color.white;

        Button button = cardObject.GetComponent<Button>();
        button.targetGraphic = cardImage;
        button.interactable = unlocked;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = unlocked
            ? new Color(0.92f, 1f, 0.92f, 1f)
            : Color.white;
        colors.pressedColor = new Color(0.84f, 0.95f, 0.84f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Color.white;
        button.colors = colors;
        if (unlocked)
            button.onClick.AddListener(() => ShowLevelSelect(chapterNumber));

        Image thumbnailImage = CreateImage(
            cardObject.transform,
            "Thumbnail",
            thumbnail,
            new Vector2(8f, -8f),
            new Vector2(86f, 86f),
            true);
        thumbnailImage.raycastTarget = false;

        Image frameImage = CreateImage(
            cardObject.transform,
            "ThumbnailFrame",
            chapterThumbnailFrameSprite,
            new Vector2(6f, -6f),
            new Vector2(90f, 90f),
            true);
        frameImage.raycastTarget = false;

        Color chapterColor = unlocked
            ? new Color(0.37f, 0.65f, 0.25f, 1f)
            : Color.white;
        Text chapterLabel = CreateLabel(
            cardObject.transform,
            "ChapterLabel",
            ChapterLabels[chapterNumber - 1],
            new Vector2(120f, -13f),
            new Vector2(250f, 28f),
            19,
            TextAnchor.MiddleLeft,
            chapterColor);
        chapterLabel.raycastTarget = false;

        Text titleLabel = CreateLabel(
            cardObject.transform,
            "ChapterTitle",
            chapterTitle,
            new Vector2(120f, -40f),
            new Vector2(270f, 48f),
            31,
            TextAnchor.MiddleLeft,
            chapterColor);
        titleLabel.raycastTarget = false;

        if (unlocked)
        {
            int completed = GetCompletedLevelCount(chapter);
            int total = chapter != null ? chapter.Levels.Count : 0;
            Image pill = CreateImage(
                cardObject.transform,
                "ProgressPill",
                progressPillSprite,
                new Vector2(385f, -39f),
                new Vector2(104f, 39f),
                false);
            pill.raycastTarget = false;

            Text progress = CreateLabel(
                cardObject.transform,
                "ProgressText",
                $"{completed}/{total}",
                new Vector2(397f, -43f),
                new Vector2(60f, 31f),
                19,
                TextAnchor.MiddleCenter,
                Color.white);
            progress.raycastTarget = false;

            Image star = CreateImage(
                cardObject.transform,
                "ProgressStar",
                starSprite,
                new Vector2(460f, -47f),
                new Vector2(23f, 23f),
                true);
            star.raycastTarget = false;

            Image arrow = CreateImage(
                cardObject.transform,
                "Arrow",
                arrowSprite,
                new Vector2(512f, -36f),
                new Vector2(29f, 29f),
                true);
            arrow.raycastTarget = false;
        }
        else
        {
            Image locked = CreateImage(
                cardObject.transform,
                "Lock",
                lockSprite,
                new Vector2(497f, -34f),
                new Vector2(34f, 34f),
                true);
            locked.raycastTarget = false;
        }
    }

    private static int GetCompletedLevelCount(ChapterDefinition chapter)
    {
        if (chapter == null || LevelProgress.Instance == null) return 0;

        int count = 0;
        foreach (LevelDefinition level in chapter.Levels)
            if (LevelProgress.Instance.IsCompleted(level.ChapterNumber, level.LevelNumber))
                count++;
        return count;
    }

    private void BuildBottomActions()
    {
        GameObject actions = CreateAnchoredContainer(
            menuContent,
            "BottomActions",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(55f, -805f),
            new Vector2(340f, 120f));

        CreateStateButtonWithLabel(
            actions.transform,
            "SkinButton",
            "皮肤",
            "skin",
            new Vector2(0f, 0f));
        CreateStateButtonWithLabel(
            actions.transform,
            "AchievementsButton",
            "成就",
            "achievements",
            new Vector2(108f, 0f));
        CreateStateButtonWithLabel(
            actions.transform,
            "GalleryButton",
            "图鉴",
            "gallery",
            new Vector2(216f, 0f));
    }

    private void BuildTopActions()
    {
        GameObject actions = CreateAnchoredContainer(
            transform,
            "TopActions",
            Vector2.one,
            Vector2.one,
            new Vector2(-48f, -38f),
            new Vector2(176f, 82f));

        CreateStateButton(
            actions.transform,
            "SettingsButton",
            "settings",
            new Vector2(0f, 0f),
            new Vector2(74f, 74f));
        Button trophy = CreateStateButton(
            actions.transform,
            "TrophyButton",
            "trophy",
            new Vector2(94f, 0f),
            new Vector2(74f, 74f));

        Image dot = CreateImage(
            trophy.transform,
            "NotificationDot",
            notificationDotSprite,
            new Vector2(59f, 2f),
            new Vector2(18f, 18f),
            true);
        dot.raycastTarget = false;
    }

    private void CreateVersionLabel()
    {
        Text version = CreateLabel(
            transform,
            "VersionText",
            $"v{Application.version}",
            Vector2.zero,
            new Vector2(160f, 40f),
            20,
            TextAnchor.MiddleRight,
            Color.white);
        RectTransform rect = version.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-38f, 22f);
    }

    private void BuildLevelPanelShell()
    {
        CreateButton(levelPanel.transform, "BackButton", "‹ 返回章节",
            new Vector2(45f, -205f), new Vector2(260f, 58f), CardColor, ShowChapterSelect);

        selectionText = CreateLabel(levelPanel.transform, "SelectionText", "选择关卡",
            new Vector2(55f, -280f), new Vector2(560f, 65f), 34,
            TextAnchor.MiddleLeft, Color.white);

        loadingText = CreateLabel(levelPanel.transform, "LoadingText", string.Empty,
            new Vector2(55f, -830f), new Vector2(560f, 45f), 24,
            TextAnchor.MiddleCenter, Color.white);

        startButton = CreateButton(levelPanel.transform, "StartButton", "开始",
            new Vector2(55f, -890f), new Vector2(560f, 82f), AccentColor,
            () => flow.StartSelectedLevel());
        startButton.interactable = false;
    }

    private void RebuildLevelCards(int chapterNumber)
    {
        Transform oldGrid = levelPanel.transform.Find("LevelGrid");
        if (oldGrid != null) Destroy(oldGrid.gameObject);
        levelButtons.Clear();

        ChapterDefinition chapter = null;
        foreach (ChapterDefinition candidate in flow.Catalog.Chapters)
            if (candidate.ChapterNumber == chapterNumber)
                chapter = candidate;
        if (chapter == null) return;

        GameObject grid = new GameObject("LevelGrid", typeof(RectTransform));
        grid.transform.SetParent(levelPanel.transform, false);
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 1f);
        gridRect.anchoredPosition = new Vector2(55f, -365f);
        gridRect.sizeDelta = new Vector2(560f, 420f);

        const float size = 160f;
        const float gap = 40f;
        for (int index = 0; index < chapter.Levels.Count; index++)
        {
            LevelDefinition level = chapter.Levels[index];
            int row = index / 3;
            int column = index % 3;
            Vector2 position = new Vector2(column * (size + gap), -row * (size + 28f));
            string capturedId = level.LevelId;
            Button button = CreateButton(grid.transform, $"LevelButton{level.LevelNumber}",
                level.LevelNumber.ToString(), position, new Vector2(size, size), CardColor,
                () => SelectLevel(capturedId));
            levelButtons[level.LevelId] = button;
        }

        RefreshLevelCards();
    }

    private void SelectLevel(string levelId)
    {
        flow.SelectLevel(levelId);
        RefreshLevelCards();

        LevelDefinition level = flow.Catalog.FindLevel(levelId);
        if (level != null)
            selectionText.text = $"第 {level.LevelNumber} 关  {level.DisplayName}";
    }

    private void ShowChapterSelect()
    {
        if (panelTransition != null && panelTransition.IsActive())
            return;

        levelPanelVisible = false;
        if (!levelPanel.activeSelf)
        {
            SetPanelImmediate(chapterPanel, chapterPanelGroup, true);
            SetPanelImmediate(levelPanel, levelPanelGroup, false);
            return;
        }

        CrossFadePanels(levelPanel, levelPanelGroup, chapterPanel, chapterPanelGroup);
    }

    private void ShowLevelSelect(int chapterNumber)
    {
        if (panelTransition != null && panelTransition.IsActive())
            return;

        visibleChapter = chapterNumber;
        levelPanelVisible = true;
        RebuildLevelCards(chapterNumber);
        if (!chapterPanel.activeSelf)
        {
            SetPanelImmediate(chapterPanel, chapterPanelGroup, false);
            SetPanelImmediate(levelPanel, levelPanelGroup, true);
            return;
        }

        CrossFadePanels(chapterPanel, chapterPanelGroup, levelPanel, levelPanelGroup);
    }

    private void CrossFadePanels(
        GameObject outgoing,
        CanvasGroup outgoingGroup,
        GameObject incoming,
        CanvasGroup incomingGroup)
    {
        if (panelTransition != null && panelTransition.IsActive())
            return;

        outgoingGroup.interactable = false;
        outgoingGroup.blocksRaycasts = false;
        incoming.SetActive(true);
        incomingGroup.alpha = 0f;
        incomingGroup.interactable = false;
        incomingGroup.blocksRaycasts = false;

        Tween fadeOut = DOTween.To(
                () => outgoingGroup.alpha,
                value => outgoingGroup.alpha = value,
                0f,
                PanelFadeDuration)
            .SetUpdate(true)
            .SetTarget(outgoingGroup);
        Tween fadeIn = DOTween.To(
                () => incomingGroup.alpha,
                value => incomingGroup.alpha = value,
                1f,
                PanelFadeDuration)
            .SetUpdate(true)
            .SetTarget(incomingGroup);

        panelTransition = DOTween.Sequence()
            .SetUpdate(true)
            .Append(fadeOut)
            .AppendCallback(() => outgoing.SetActive(false))
            .Append(fadeIn)
            .OnComplete(() =>
            {
                incomingGroup.interactable = true;
                incomingGroup.blocksRaycasts = true;
                panelTransition = null;
            });
    }

    private static void SetPanelImmediate(
        GameObject panel,
        CanvasGroup group,
        bool visible)
    {
        panel.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void RefreshLevelCards()
    {
        foreach (KeyValuePair<string, Button> pair in levelButtons)
        {
            LevelDefinition level = flow.Catalog.FindLevel(pair.Key);
            bool unlocked = LevelProgress.Instance == null ||
                LevelProgress.Instance.IsUnlocked(level.ChapterNumber, level.LevelNumber);
            bool selected = flow.SelectedLevel != null && flow.SelectedLevel.LevelId == level.LevelId;

            pair.Value.interactable = unlocked;
            pair.Value.GetComponent<Image>().color =
                !unlocked ? LockedColor : selected ? SelectedColor : CardColor;
            Text label = pair.Value.GetComponentInChildren<Text>();
            label.color = selected ? new Color(0.15f, 0.35f, 0.18f) : Color.white;

            if (LevelProgress.Instance != null &&
                LevelProgress.Instance.IsCompleted(level.ChapterNumber, level.LevelNumber))
                label.text = $"{level.LevelNumber}\n✓";
            else if (!unlocked)
                label.text = $"{level.LevelNumber}\n锁定";
            else
                label.text = level.LevelNumber.ToString();
        }
    }

    private void OnFlowStateChanged(GameFlowState state)
    {
        bool ready = state == GameFlowState.PreviewReady;
        if (startButton != null) startButton.interactable = ready;

        if (loadingText != null)
            loadingText.text = ready ? string.Empty :
                state == GameFlowState.Error ? "加载失败" :
                flow.IsMenuVisible || state == GameFlowState.Transitioning ? "正在准备关卡…" : string.Empty;

        if (state == GameFlowState.StartingGameplay)
            FadeMenu(false);
        else if (state == GameFlowState.ReturningToMenu ||
                 state == GameFlowState.PreviewLoading ||
                 state == GameFlowState.PreviewReady)
            FadeMenu(true);

        RefreshLevelCards();
    }

    private void OnPreviewChanged(LevelDefinition level)
    {
        if (level == null) return;
        selectionText.text = $"第 {level.LevelNumber} 关  {level.DisplayName}";
        if (visibleChapter != level.ChapterNumber && levelPanelVisible)
            ShowLevelSelect(level.ChapterNumber);
        RefreshLevelCards();
    }

    private void OnFlowFailed(string message)
    {
        if (loadingText != null) loadingText.text = message;
    }

    private void FadeMenu(bool visible)
    {
        canvasGroup.DOKill();
        menuContent.DOKill();

        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
        float targetX = visible ? 0f : -45f;

        DOTween.To(
                () => canvasGroup.alpha,
                value => canvasGroup.alpha = value,
                visible ? 1f : 0f,
                FadeDuration)
            .SetUpdate(true)
            .SetTarget(canvasGroup);

        DOTween.To(
                () => menuContent.anchoredPosition.x,
                value =>
                {
                    Vector2 position = menuContent.anchoredPosition;
                    position.x = value;
                    menuContent.anchoredPosition = position;
                },
                targetX,
                FadeDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetTarget(menuContent);
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private static GameObject CreateAnchoredContainer(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject container = new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return container;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchoredPosition,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        return image;
    }

    private Button CreateStateButton(
        Transform parent,
        string name,
        string assetKey,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        Sprite normal = LoadSprite($"normal/btn_{assetKey}_normal");
        Sprite hover = LoadSprite($"hover/btn_{assetKey}_hover");

        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = normal;
        image.color = Color.white;
        image.preserveAspect = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = button.spriteState;
        state.highlightedSprite = hover;
        state.pressedSprite = hover;
        state.selectedSprite = hover;
        button.spriteState = state;
        return button;
    }

    private void CreateStateButtonWithLabel(
        Transform parent,
        string name,
        string label,
        string assetKey,
        Vector2 anchoredPosition)
    {
        CreateStateButton(
            parent,
            name,
            assetKey,
            anchoredPosition,
            new Vector2(84f, 84f));
        Text text = CreateLabel(
            parent,
            $"{name}Label",
            label,
            anchoredPosition + new Vector2(-3f, -82f),
            new Vector2(90f, 34f),
            22,
            TextAnchor.MiddleCenter,
            Color.white);
        text.raycastTarget = false;
    }

    private static Sprite LoadSprite(string relativePath)
    {
        Sprite sprite = Resources.Load<Sprite>(UiRoot + relativePath);
        if (sprite == null)
            Debug.LogWarning($"Main Menu sprite is missing: {UiRoot}{relativePath}");
        return sprite;
    }

    private Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color,
        UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = CreateLabel(buttonObject.transform, "Label", label, Vector2.zero, Vector2.zero,
            28, TextAnchor.MiddleCenter, Color.white, true);
        text.raycastTarget = false;
        return button;
    }

    private Text CreateLabel(
        Transform parent,
        string name,
        string value,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        TextAnchor alignment,
        Color color,
        bool stretch = false)
    {
        GameObject labelObject = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();

        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        Text text = labelObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        return text;
    }

    private T FindNamedComponent<T>(string objectName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
            if (component.gameObject.name == objectName)
                return component;
        return null;
    }
}
