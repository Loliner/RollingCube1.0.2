using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Tracks which levels have been completed and derives per-level unlock state
// from it (level 1 of a chapter is always unlocked; level N unlocks once
// level N-1 of the same chapter is completed). Bootstrapped the same way as
// StepCounter/PauseMenu so it exists regardless of which scene is opened and
// played directly. Backed by a JSON save file so unlock progress survives
// across game restarts. See design/gdd/ui-start-screen.md for the full design.
public class LevelProgress : MonoBehaviour
{
    private const string SaveFileName = "levelprogress.json";

    public static LevelProgress Instance { get; private set; }

    [Serializable]
    private class LevelEntry
    {
        public string levelId;
        public bool completed;
    }

    [Serializable]
    private class SaveData
    {
        public List<LevelEntry> levels = new List<LevelEntry>();
    }

    private readonly Dictionary<string, bool> completed = new Dictionary<string, bool>();
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [RuntimeInitializeOnLoadMethod]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject host = new GameObject("LevelProgress");
        Instance = host.AddComponent<LevelProgress>();
        DontDestroyOnLoad(host);
    }

    void Awake()
    {
        Load();
    }

    public bool IsUnlocked(int chapter, int scene)
    {
        if (scene <= 1) return true;
        return completed.TryGetValue(LevelId(chapter, scene - 1), out bool done) && done;
    }

    // Called by SceneSwitcher when the player reaches the end-of-level trigger.
    public void RegisterCompletion(int chapter, int scene)
    {
        completed[LevelId(chapter, scene)] = true;
        Save();
    }

    private static string LevelId(int chapter, int scene) => $"Chapter{chapter}_Scene{scene}";

    private void Load()
    {
        completed.Clear();

        if (!File.Exists(SavePath)) return;

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data?.levels == null) return;
            foreach (LevelEntry entry in data.levels)
                completed[entry.levelId] = entry.completed;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelProgress] Failed to parse save file at {SavePath}, starting with an empty save: {e.Message}");
            completed.Clear();
        }
    }

    private void Save()
    {
        SaveData data = new SaveData();
        foreach (KeyValuePair<string, bool> pair in completed)
            data.levels.Add(new LevelEntry { levelId = pair.Key, completed = pair.Value });

        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
    }
}
