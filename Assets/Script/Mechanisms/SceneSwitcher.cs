using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

// 通关判定：玩家必须以符文面朝下的姿态在此停留 requiredDwellSeconds 秒才算通关。
// 停留时长只在符文朝下时累积（IsRuneFaceDown()）；朝向不对时不推进。玩家站在本
// 格子上时无法通过翻滚改变朝向（翻滚必然离开当前格），所以不需要处理"停留中途
// 从朝下变为不朝下"的过渡——真实发生的只有"从进入到离开全程朝下/全程不朝下"。
public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private float requiredDwellSeconds = 2f;

    private bool isTriggered;
    private float dwellSeconds;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;

        isTriggered = true;
        dwellSeconds = 0f;
    }

    void OnTriggerStay(Collider other)
    {
        if (!isTriggered) return;

        Player player = other.GetComponent<Player>();
        if (player == null || !player.IsRuneFaceDown()) return;

        dwellSeconds += Time.deltaTime;
        if (dwellSeconds < requiredDwellSeconds) return;

        isTriggered = false;

        LevelContext context = GetComponentInParent<LevelContext>();
        if (GameFlowController.Instance != null)
        {
            if (context == null)
            {
                Debug.LogError("SceneSwitcher is not parented under a LevelContext.", this);
                return;
            }

            GameFlowController.Instance.CompleteLevel(context);
            return;
        }

        string currentSceneName = gameObject.scene.name;
        int chapter;
        int scene;
        if (context != null)
        {
            chapter = context.ChapterNumber;
            scene = context.LevelNumber;
        }
        else
        {
            Match match = Regex.Match(currentSceneName, @"^Chapter(\d+)_Scene(\d+)$");
            if (!match.Success) return;

            chapter = int.Parse(match.Groups[1].Value);
            scene = int.Parse(match.Groups[2].Value);
        }

        LevelProgress.Instance.RegisterCompletion(chapter, scene);

        // Next scene in the same chapter if it's registered in Build Settings,
        // otherwise roll over to the first scene of the next chapter.
        string nextInChapter = $"Chapter{chapter}_Scene{scene + 1}";
        string nextChapterFirst = $"Chapter{chapter + 1}_Scene1";

        string target;
        if (Application.CanStreamedLevelBeLoaded(nextInChapter))
            target = nextInChapter;
        else if (Application.CanStreamedLevelBeLoaded(nextChapterFirst))
            target = nextChapterFirst;
        else
        {
            Debug.LogWarning($"SceneSwitcher: neither '{nextInChapter}' nor '{nextChapterFirst}' is registered in Build Settings (tried from '{currentSceneName}').");
            return;
        }

        // Standalone flow replaces the whole scene. Kill scene-owned tweens
        // before their Transform targets are destroyed by the single load.
        DOTween.KillAll();
        SceneManager.sceneLoaded -= KillStandaloneUnloadTweens;
        SceneManager.sceneLoaded += KillStandaloneUnloadTweens;
        SceneManager.LoadScene(target);
    }

    private static void KillStandaloneUnloadTweens(Scene loadedScene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= KillStandaloneUnloadTweens;
        // Unload callbacks from the previous scene can enqueue reset tweens
        // after the pre-load KillAll. sceneLoaded runs before the new
        // LevelContext.Start creates its entry ripple, so this second cleanup
        // removes only stale scene-owned work.
        DOTween.KillAll();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;
        isTriggered = false;
        dwellSeconds = 0f;
    }
}
