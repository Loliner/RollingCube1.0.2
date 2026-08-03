using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class LevelCatalogTests
{
    private LevelCatalog catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = ScriptableObject.CreateInstance<LevelCatalog>();

        LevelDefinition level11 = CreateLevel("Chapter1_Scene1", 1, 1);
        LevelDefinition level12 = CreateLevel("Chapter1_Scene2", 1, 2);
        LevelDefinition level21 = CreateLevel("Chapter2_Scene1", 2, 1);

        ChapterDefinition chapter1 = new ChapterDefinition();
        chapter1.Configure(1, "Chapter One", null, new List<LevelDefinition> { level11, level12 });
        ChapterDefinition chapter2 = new ChapterDefinition();
        chapter2.Configure(2, "Chapter Two", null, new List<LevelDefinition> { level21 });
        catalog.Configure(new List<ChapterDefinition> { chapter1, chapter2 });
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void FindLevel_UsesStableIdAndCoordinates()
    {
        LevelDefinition byId = catalog.FindLevel("Chapter1_Scene2");
        LevelDefinition byCoordinates = catalog.FindLevel(1, 2);

        Assert.That(byId, Is.SameAs(byCoordinates));
        Assert.That(byId.SceneName, Is.EqualTo("Chapter1_Scene2"));
        Assert.That(catalog.FindLevel("Missing"), Is.Null);
    }

    [Test]
    public void FindNext_CrossesChapterBoundaryAndStopsAtCatalogEnd()
    {
        LevelDefinition level11 = catalog.FindLevel(1, 1);
        LevelDefinition level12 = catalog.FindNext(level11);
        LevelDefinition level21 = catalog.FindNext(level12);

        Assert.That(level12.LevelId, Is.EqualTo("Chapter1_Scene2"));
        Assert.That(level21.LevelId, Is.EqualTo("Chapter2_Scene1"));
        Assert.That(catalog.FindNext(level21), Is.Null);
    }

    [Test]
    public void FindHighestUnlocked_WithoutProgressReturnsLastCatalogEntry()
    {
        Assert.That(
            catalog.FindHighestUnlocked(null).LevelId,
            Is.EqualTo("Chapter2_Scene1"));
    }

    private static LevelDefinition CreateLevel(string id, int chapter, int level)
    {
        LevelDefinition definition = new LevelDefinition();
        definition.Configure(id, chapter, level, $"Level {level}", id, null);
        return definition;
    }
}
