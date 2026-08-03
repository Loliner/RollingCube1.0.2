using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class GameShellFlowTests
{
    private string savePath;
    private bool saveExisted;
    private byte[] savedProgress;
    private bool originalRunInBackground;

    [SetUp]
    public void PreserveRuntimeState()
    {
        originalRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        savePath = Path.Combine(Application.persistentDataPath, "levelprogress.json");
        saveExisted = File.Exists(savePath);
        if (saveExisted) savedProgress = File.ReadAllBytes(savePath);
    }

    [TearDown]
    public void RestoreRuntimeState()
    {
        Time.timeScale = 1f;
        Application.runInBackground = originalRunInBackground;

        if (saveExisted)
            File.WriteAllBytes(savePath, savedProgress);
        else if (File.Exists(savePath))
            File.Delete(savePath);
    }

    [UnityTest]
    public IEnumerator MainMenu_PreviewsStartsAndReturnsWithoutReplacingGameShell()
    {
        GameFlowController flow = null;
        yield return LoadGameShell(value => flow = value);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
        Assert.That(flow.CurrentLevel, Is.Not.Null);
        Assert.That(
            Object.FindObjectsByType<Player>(FindObjectsInactive.Include).Length,
            Is.EqualTo(1));

        string selectedLevelId = flow.SelectedLevel.LevelId;
        flow.StartSelectedLevel();
        yield return WaitForState(flow, GameFlowState.Playing, 4f);

        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(flow.CurrentLevel.LevelId, Is.EqualTo(selectedLevelId));

        flow.ReturnToMenu();
        yield return WaitForState(flow, GameFlowState.PreviewReady, 12f);

        Assert.That(Time.timeScale, Is.EqualTo(0f));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
        Assert.That(flow.CurrentLevel.LevelId, Is.EqualTo(selectedLevelId));
        Assert.That(
            Object.FindObjectsByType<Player>(FindObjectsInactive.Include).Length,
            Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RapidPreviewSelection_ResolvesToLastRequestedLevel()
    {
        GameFlowController flow = null;
        yield return LoadGameShell(value => flow = value);
        LevelProgress.Instance.RegisterCompletion(1, 1);

        flow.SelectLevel("Chapter1_Scene1");
        flow.SelectLevel("Chapter1_Scene2");

        yield return WaitForCurrentLevel(flow, "Chapter1_Scene2", GameFlowState.PreviewReady, 20f);
        Assert.That(flow.SelectedLevel.LevelId, Is.EqualTo("Chapter1_Scene2"));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator PreviewSelection_HidesIncomingLevelUntilItsRippleStarts()
    {
        GameFlowController flow = null;
        yield return LoadGameShell(value => flow = value);
        LevelProgress.Instance.RegisterCompletion(1, 1);

        LevelContext outgoing = flow.CurrentLevel;
        string targetLevelId =
            outgoing.LevelId == "Chapter1_Scene1" ? "Chapter1_Scene2" : "Chapter1_Scene1";
        flow.SelectLevel(targetLevelId);

        LevelContext incoming = null;
        float loadDeadline = Time.realtimeSinceStartup + 8f;
        while (incoming == null && Time.realtimeSinceStartup < loadDeadline)
        {
            foreach (LevelContext context in
                     Object.FindObjectsByType<LevelContext>(
                         FindObjectsInactive.Include))
            {
                if (context != outgoing && context.LevelId == targetLevelId)
                {
                    incoming = context;
                    break;
                }
            }

            if (incoming == null) yield return null;
        }

        Assert.That(incoming, Is.Not.Null, "The additive level never registered.");
        Assert.That(
            AllTransitionTargetsAreZero(incoming.LevelRoot),
            Is.True,
            "The incoming level became visible before its entry ripple started.");

        float entryDeadline = Time.realtimeSinceStartup + 3f;
        while (AllTransitionTargetsAreZero(incoming.LevelRoot) &&
               Time.realtimeSinceStartup < entryDeadline)
        {
            yield return null;
        }

        Assert.That(
            AnyTransitionTargetIsVisible(incoming.LevelRoot),
            Is.True,
            "The incoming entry ripple never started.");
        Assert.That(
            AnyTransitionTargetIsVisible(outgoing.LevelRoot),
            Is.True,
            "The outgoing level finished before the incoming entry ripple started.");

        yield return WaitForCurrentLevel(flow, targetLevelId, GameFlowState.PreviewReady, 20f);
    }

    [UnityTest]
    public IEnumerator ChapterSelection_FadesOutBeforeLevelSelectionFadesIn()
    {
        GameFlowController flow = null;
        yield return LoadGameShell(value => flow = value);

        MainMenu menu = Object.FindAnyObjectByType<MainMenu>();
        Assert.That(menu, Is.Not.Null);

        Transform menuContent = menu.transform.Find("MenuContent");
        Transform chapterPanel = menuContent.Find("ChapterPanel");
        Transform levelPanel = menuContent.Find("LevelPanel");
        CanvasGroup chapterGroup = chapterPanel.GetComponent<CanvasGroup>();
        CanvasGroup levelGroup = levelPanel.GetComponent<CanvasGroup>();
        Button chapterButton = chapterPanel.Find("Chapter1Button").GetComponent<Button>();

        chapterButton.onClick.Invoke();
        Assert.That(chapterPanel.gameObject.activeSelf, Is.True);
        Assert.That(levelPanel.gameObject.activeSelf, Is.True);
        Assert.That(levelGroup.alpha, Is.EqualTo(0f).Within(0.001f));
        Assert.That(chapterGroup.interactable, Is.False);
        Assert.That(levelGroup.interactable, Is.False);

        float fadeOutDeadline = Time.realtimeSinceStartup + 1f;
        while (chapterPanel.gameObject.activeSelf &&
               Time.realtimeSinceStartup < fadeOutDeadline)
        {
            yield return null;
        }

        Assert.That(chapterPanel.gameObject.activeSelf, Is.False);

        float fadeInDeadline = Time.realtimeSinceStartup + 1f;
        while ((levelGroup.alpha < 0.999f || !levelGroup.interactable) &&
               Time.realtimeSinceStartup < fadeInDeadline)
            yield return null;

        Assert.That(levelGroup.alpha, Is.EqualTo(1f).Within(0.001f));
        Assert.That(levelGroup.interactable, Is.True);
        Assert.That(levelGroup.blocksRaycasts, Is.True);
    }

    [UnityTest]
    public IEnumerator MainMenu_UsesImportedVisualAssetSet()
    {
        GameFlowController flow = null;
        yield return LoadGameShell(value => flow = value);

        MainMenu menu = Object.FindAnyObjectByType<MainMenu>();
        Assert.That(menu, Is.Not.Null);

        Transform menuContent = menu.transform.Find("MenuContent");
        Image logo = menuContent.Find("GameLogo").GetComponent<Image>();
        Transform chapterPanel = menuContent.Find("ChapterPanel");
        Button chapterOne = chapterPanel.Find("Chapter1Button").GetComponent<Button>();
        Button chapterFour = chapterPanel.Find("Chapter4Button").GetComponent<Button>();
        Transform topActions = menu.transform.Find("TopActions");
        Button settings = topActions.Find("SettingsButton").GetComponent<Button>();
        Image notification = topActions
            .Find("TrophyButton/NotificationDot")
            .GetComponent<Image>();
        Transform bottomActions = menuContent.Find("BottomActions");
        Button skin = bottomActions.Find("SkinButton").GetComponent<Button>();

        Assert.That(logo.sprite, Is.Not.Null);
        Assert.That(chapterOne.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(chapterOne.transform.Find("Thumbnail").GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(chapterOne.interactable, Is.True);
        Assert.That(chapterFour.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(chapterFour.transform.Find("Lock").GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(chapterFour.interactable, Is.False);
        Assert.That(settings.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(settings.spriteState.highlightedSprite, Is.Not.Null);
        Assert.That(notification.sprite, Is.Not.Null);
        Assert.That(skin.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(skin.spriteState.highlightedSprite, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Completion_AdvancesAndFinalLevelReturnsToMenuPreview()
    {
        GameFlowController flow = null;
        yield return LoadGameShell(value => flow = value);
        for (int level = 1; level <= 8; level++)
            LevelProgress.Instance.RegisterCompletion(1, level);

        flow.SelectLevel("Chapter1_Scene1");
        yield return WaitForCurrentLevel(flow, "Chapter1_Scene1", GameFlowState.PreviewReady, 20f);
        flow.StartSelectedLevel();
        yield return WaitForState(flow, GameFlowState.Playing, 4f);

        flow.CompleteLevel(flow.CurrentLevel);
        yield return WaitForCurrentLevel(flow, "Chapter1_Scene2", GameFlowState.Playing, 20f);

        flow.ReturnToMenu();
        yield return WaitForCurrentLevel(flow, "Chapter1_Scene2", GameFlowState.PreviewReady, 15f);
        flow.SelectLevel("Chapter1_Scene9");
        yield return WaitForCurrentLevel(flow, "Chapter1_Scene9", GameFlowState.PreviewReady, 20f);
        flow.StartSelectedLevel();
        yield return WaitForState(flow, GameFlowState.Playing, 4f);

        flow.CompleteLevel(flow.CurrentLevel);
        yield return WaitForCurrentLevel(flow, "Chapter1_Scene9", GameFlowState.PreviewReady, 15f);
        Assert.That(flow.IsMenuVisible, Is.True);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
    }

    [UnityTest]
    public IEnumerator PauseResumeAndReset_StayInsideSeamlessFlow()
    {
        GameFlowController flow = null;
        yield return LoadGameShell(value => flow = value);
        flow.StartSelectedLevel();
        yield return WaitForState(flow, GameFlowState.Playing, 4f);

        string levelId = flow.CurrentLevel.LevelId;
        Assert.That(flow.PauseGameplay(), Is.True);
        Assert.That(flow.State, Is.EqualTo(GameFlowState.Paused));
        Assert.That(Time.timeScale, Is.EqualTo(0f));

        Assert.That(flow.ResumeGameplay(), Is.True);
        Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        flow.ResetCurrentLevel();
        yield return WaitForCurrentLevel(flow, levelId, GameFlowState.Playing, 15f);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
    }

    private static IEnumerator LoadGameShell(System.Action<GameFlowController> assign)
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        yield return null;

        GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
        Assert.That(flow, Is.Not.Null);
        assign(flow);
        yield return WaitForState(flow, GameFlowState.PreviewReady, 12f);
    }

    private static IEnumerator WaitForState(
        GameFlowController flow,
        GameFlowState expected,
        float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (flow != null && flow.State != expected && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(flow, Is.Not.Null);
        Assert.That(
            flow.State,
            Is.EqualTo(expected),
            $"Flow remained in {flow.State}/{flow.TransitionPhase}.");
    }

    private static IEnumerator WaitForCurrentLevel(
        GameFlowController flow,
        string levelId,
        GameFlowState expectedState,
        float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (flow != null &&
               (flow.State != expectedState ||
                flow.CurrentLevel == null ||
                flow.CurrentLevel.LevelId != levelId) &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.That(flow, Is.Not.Null);
        Assert.That(
            flow.State,
            Is.EqualTo(expectedState),
            $"Flow remained in {flow.State}/{flow.TransitionPhase}.");
        Assert.That(flow.CurrentLevel, Is.Not.Null);
        Assert.That(flow.CurrentLevel.LevelId, Is.EqualTo(levelId));
    }

    private static bool AllTransitionTargetsAreZero(Transform levelRoot)
    {
        foreach (Transform child in levelRoot)
        {
            if (child.GetComponent<LevelTransitionExclude>() != null) continue;
            if (child.localScale.sqrMagnitude > 0.000001f) return false;
        }

        return true;
    }

    private static bool AnyTransitionTargetIsVisible(Transform levelRoot)
    {
        foreach (Transform child in levelRoot)
        {
            if (child.GetComponent<LevelTransitionExclude>() != null) continue;
            if (child.localScale.sqrMagnitude > 0.000001f) return true;
        }

        return false;
    }
}
