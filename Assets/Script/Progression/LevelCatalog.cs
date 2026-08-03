using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelDefinition
{
    [SerializeField] private string levelId;
    [SerializeField] private int chapterNumber;
    [SerializeField] private int levelNumber;
    [SerializeField] private string displayName;
    [SerializeField] private string sceneName;
    [SerializeField] private Sprite previewImage;

    public string LevelId => levelId;
    public int ChapterNumber => chapterNumber;
    public int LevelNumber => levelNumber;
    public string DisplayName => displayName;
    public string SceneName => sceneName;
    public Sprite PreviewImage => previewImage;

#if UNITY_EDITOR
    public void Configure(string id, int chapter, int level, string label, string scene, Sprite image)
    {
        levelId = id;
        chapterNumber = chapter;
        levelNumber = level;
        displayName = label;
        sceneName = scene;
        previewImage = image;
    }
#endif
}

[Serializable]
public sealed class ChapterDefinition
{
    [SerializeField] private int chapterNumber;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite previewImage;
    [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

    public int ChapterNumber => chapterNumber;
    public string DisplayName => displayName;
    public Sprite PreviewImage => previewImage;
    public IReadOnlyList<LevelDefinition> Levels => levels;

#if UNITY_EDITOR
    public void Configure(int number, string label, Sprite image, List<LevelDefinition> chapterLevels)
    {
        chapterNumber = number;
        displayName = label;
        previewImage = image;
        levels = chapterLevels;
    }
#endif
}

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "RollingCube/Level Catalog")]
public sealed class LevelCatalog : ScriptableObject
{
    [SerializeField] private List<ChapterDefinition> chapters = new List<ChapterDefinition>();

    public IReadOnlyList<ChapterDefinition> Chapters => chapters;

    /// <summary>Finds a configured level by its stable ID.</summary>
    public LevelDefinition FindLevel(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId)) return null;

        foreach (ChapterDefinition chapter in chapters)
        foreach (LevelDefinition level in chapter.Levels)
            if (string.Equals(level.LevelId, levelId, StringComparison.Ordinal))
                return level;

        return null;
    }

    /// <summary>Finds a configured level by chapter and level number.</summary>
    public LevelDefinition FindLevel(int chapterNumber, int levelNumber)
    {
        foreach (ChapterDefinition chapter in chapters)
        {
            if (chapter.ChapterNumber != chapterNumber) continue;
            foreach (LevelDefinition level in chapter.Levels)
                if (level.LevelNumber == levelNumber)
                    return level;
        }

        return null;
    }

    /// <summary>Returns the level following the supplied level, or null at the end of the catalog.</summary>
    public LevelDefinition FindNext(LevelDefinition current)
    {
        if (current == null) return null;

        for (int chapterIndex = 0; chapterIndex < chapters.Count; chapterIndex++)
        {
            IReadOnlyList<LevelDefinition> levels = chapters[chapterIndex].Levels;
            for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
            {
                if (!ReferenceEquals(levels[levelIndex], current) &&
                    levels[levelIndex].LevelId != current.LevelId)
                    continue;

                if (levelIndex + 1 < levels.Count)
                    return levels[levelIndex + 1];

                for (int nextChapter = chapterIndex + 1; nextChapter < chapters.Count; nextChapter++)
                    if (chapters[nextChapter].Levels.Count > 0)
                        return chapters[nextChapter].Levels[0];

                return null;
            }
        }

        return null;
    }

    /// <summary>Returns the furthest level currently unlocked by the supplied progress data.</summary>
    public LevelDefinition FindHighestUnlocked(LevelProgress progress)
    {
        LevelDefinition highest = null;

        foreach (ChapterDefinition chapter in chapters)
        foreach (LevelDefinition level in chapter.Levels)
        {
            if (progress != null && !progress.IsUnlocked(level.ChapterNumber, level.LevelNumber))
                continue;
            highest = level;
        }

        return highest;
    }

#if UNITY_EDITOR
    public void Configure(List<ChapterDefinition> configuredChapters)
    {
        chapters = configuredChapters;
    }
#endif
}
