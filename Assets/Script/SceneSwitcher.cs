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
        Match match = Regex.Match(currentSceneName, @"Scene(\d+)");
        if (!match.Success) return;

        int number = int.Parse(match.Groups[1].Value) + 1;
        SceneManager.LoadScene("Scene" + number);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() == null) return;
        isTriggered = false;
    }
}
