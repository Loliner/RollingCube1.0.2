using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SeamlessFlowSetup
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string TemplateScenePath = "Assets/Scenes/TemplateSceneCubic.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string CatalogPath = "Assets/Resources/LevelCatalog.asset";
    private const string TemplateSkyboxGuid = "6193bcab9eda1437ca714e75b7865249";

    private static readonly string[] LevelNames =
    {
        "符文终点",
        "落差之路",
        "双桥机关",
        "载运平台",
        "返回平台",
        "双箱推移",
        "坠落之箱",
        "压力桥",
        "隐藏开关",
    };

    [MenuItem("Tools/RollingCube/Setup Seamless GameShell")]
    public static void SetupGameShell()
    {
        LevelCatalog catalog = BuildCatalog();
        Scene mainMenu = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

        GameObject shell = FindRoot(mainMenu, "GameShell");
        if (shell == null) shell = new GameObject("GameShell");

        Camera camera = Camera.main;
        if (camera == null)
            throw new System.InvalidOperationException("MainMenu must contain a MainCamera.");
        camera.transform.SetParent(shell.transform, true);

        Light mainLight = Object.FindAnyObjectByType<Light>();
        if (mainLight != null) mainLight.transform.SetParent(shell.transform, true);

        CopyTemplateEnvironmentIfMissing(mainMenu, shell.transform);
        ApplyTemplateSkybox();

        Player player = Object.FindAnyObjectByType<Player>();
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject playerObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mainMenu);
            playerObject.name = "Player";
            playerObject.transform.position = new Vector3(0f, 0.5f, 0f);
            playerObject.transform.rotation = Quaternion.identity;
            player = playerObject.GetComponent<Player>();
        }
        player.transform.SetParent(shell.transform, true);

        GameFlowController flow = shell.GetComponent<GameFlowController>();
        if (flow == null) flow = shell.AddComponent<GameFlowController>();
        SerializedObject flowSerialized = new SerializedObject(flow);
        flowSerialized.FindProperty("catalog").objectReferenceValue = catalog;
        flowSerialized.FindProperty("player").objectReferenceValue = player;
        flowSerialized.FindProperty("mainCamera").objectReferenceValue = camera;
        flowSerialized.ApplyModifiedPropertiesWithoutUndo();

        MainMenu menu = Object.FindAnyObjectByType<MainMenu>();
        if (menu == null)
            throw new System.InvalidOperationException("MainMenu scene must contain a MainMenu component.");
        if (menu.GetComponent<CanvasGroup>() == null)
            menu.gameObject.AddComponent<CanvasGroup>();

        EditorSceneManager.MarkSceneDirty(mainMenu);
        EditorSceneManager.SaveScene(mainMenu);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SeamlessFlowSetup] Configured MainMenu GameShell and Chapter 1 LevelCatalog.");
    }

    private static LevelCatalog BuildCatalog()
    {
        LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        List<LevelDefinition> levels = new List<LevelDefinition>();
        for (int level = 1; level <= 9; level++)
        {
            LevelDefinition definition = new LevelDefinition();
            definition.Configure(
                $"Chapter1_Scene{level}",
                1,
                level,
                LevelNames[level - 1],
                $"Chapter1_Scene{level}",
                null);
            levels.Add(definition);
        }

        ChapterDefinition chapter = new ChapterDefinition();
        chapter.Configure(1, "第一章", null, levels);
        catalog.Configure(new List<ChapterDefinition> { chapter });
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static void CopyTemplateEnvironmentIfMissing(Scene targetScene, Transform parent)
    {
        if (GameObject.Find("Global Volume") != null) return;

        Scene template = EditorSceneManager.OpenScene(TemplateScenePath, OpenSceneMode.Additive);
        GameObject source = FindRoot(template, "Global Volume");
        if (source != null)
        {
            GameObject copy = Object.Instantiate(source);
            copy.name = "Global Volume";
            SceneManager.MoveGameObjectToScene(copy, targetScene);
            copy.transform.SetParent(parent, true);
        }

        EditorSceneManager.CloseScene(template, true);
    }

    private static void ApplyTemplateSkybox()
    {
        string path = AssetDatabase.GUIDToAssetPath(TemplateSkyboxGuid);
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (skybox != null) RenderSettings.skybox = skybox;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true)
        };

        for (int level = 1; level <= 9; level++)
            scenes.Add(new EditorBuildSettingsScene(
                $"Assets/Scenes/Chapter1/Chapter1_Scene{level}.unity", true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
