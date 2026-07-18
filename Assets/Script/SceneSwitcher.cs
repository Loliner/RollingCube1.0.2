using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private float requiredDwellSeconds = 2f;

    private bool isTriggered;
    private long triggerTimeMs;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;

        isTriggered = true;
        triggerTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    void OnTriggerStay(Collider other)
    {
        if (!isTriggered || other.GetComponent<Player>() == null) return;

        long elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - triggerTimeMs;
        if (elapsedMs < requiredDwellSeconds * 1000)
            return;

        isTriggered = false;

        string currentSceneName = SceneManager.GetActiveScene().name;
        Match match = Regex.Match(currentSceneName, @"^Chapter(\d+)_Scene(\d+)$");
        if (!match.Success) return;

        int chapter = int.Parse(match.Groups[1].Value);
        int scene = int.Parse(match.Groups[2].Value);

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
            return; // TODO: last level of the game — should return to a chapter-select screen once one exists

        SceneManager.LoadScene(target);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;
        isTriggered = false;
    }
}
