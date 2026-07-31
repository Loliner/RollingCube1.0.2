using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter1SceneBuilder
{
    private const string SceneFolder = "Assets/Scenes/Chapter1";
    private const string TemplateScenePath = "Assets/Scenes/TemplateSceneCubic.unity";
    private const string TerrainPrefabPath = "Assets/Prefabs/CubicWorld/CubicTerrain.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string GoalPrefabPath = "Assets/Prefabs/SceneSwitcher.prefab";
    private const string BoxPrefabPath = "Assets/Prefabs/PushableBlock.prefab";

    private static Transform terrainRoot;
    private static GameObject terrainSeed;

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 1")]
    public static void BuildLevel1() => RebuildLevel(1, BuildLevel01);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 2")]
    public static void BuildLevel2() => RebuildLevel(2, BuildLevel02);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 3")]
    public static void BuildLevel3() => RebuildLevel(3, BuildLevel03);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 4")]
    public static void BuildLevel4() => RebuildLevel(4, BuildLevel04);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 5")]
    public static void BuildLevel5() => RebuildLevel(5, BuildLevel05);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 6")]
    public static void BuildLevel6() => RebuildLevel(6, BuildLevel06);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 7")]
    public static void BuildLevel7() => RebuildLevel(7, BuildLevel07);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 8")]
    public static void BuildLevel8() => RebuildLevel(8, BuildLevel08);

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1/Level 9")]
    public static void BuildLevel9() => RebuildLevel(9, BuildLevel09);

    private static void RebuildLevel(int level, System.Action build)
    {
        LoadExistingAssets();
        build();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene($"{SceneFolder}/Chapter1_Scene{level}.unity", OpenSceneMode.Single);
        Debug.Log($"[Chapter1SceneBuilder] Rebuilt Chapter1_Scene{level} from TemplateSceneCubic and RCMap 1.1.");
    }

    [MenuItem("Tools/RollingCube/Rebuild Chapter 1 Scenes")]
    public static void BuildAll()
    {
        LoadExistingAssets();

        BuildLevel01();
        BuildLevel02();
        BuildLevel03();
        BuildLevel04();
        BuildLevel05();
        BuildLevel06();
        BuildLevel07();
        BuildLevel08();
        BuildLevel09();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene($"{SceneFolder}/Chapter1_Scene1.unity", OpenSceneMode.Single);
        Debug.Log("[Chapter1SceneBuilder] Rebuilt Chapter1_Scene1 through Chapter1_Scene9 from RCMap 1.1.");
    }

    private static void BuildLevel01()
    {
        Scene scene = CreateScene(1, -5, 1, -1, 1);
        HashSet<string> cells = new HashSet<string>();

        for (int x = -5; x <= 1; x++) AddTile(cells, terrainRoot, x, 0, 0f);
        for (int x = -1; x <= 1; x++)
        for (int z = -1; z <= 1; z++)
            AddTile(cells, terrainRoot, x, z, 0f);

        AddPlayer(-5, 0, 0f);
        AddGoal(0, 0, 0f);
        Save(scene, 1);
    }

    private static void BuildLevel02()
    {
        Scene scene = CreateScene(2, -4, 2, -1, 1);
        HashSet<string> cells = new HashSet<string>();

        for (int x = -4; x <= -2; x++) AddTile(cells, terrainRoot, x, 0, 0.5f);
        for (int x = -1; x <= 2; x++)
        for (int z = -1; z <= 1; z++)
            AddTile(cells, terrainRoot, x, z, 0f);

        AddPlayer(-4, 0, 0.5f);
        AddGoal(1, 0, 0f);
        Save(scene, 2);
    }

    private static void BuildLevel03()
    {
        Scene scene = CreateScene(3, -4, 5, -1, 1);
        HashSet<string> cells = new HashSet<string>();

        AddRect(cells, terrainRoot, -4, -1, -1, 1, 0f);
        AddRect(cells, terrainRoot, 2, 5, -1, 1, 0f);

        Elevator west = AddElevator("bridge_west", 0, 0, -1f, new Vector3(0f, 1f, 0f),
            false, false, false, 2f, 3f, terrainRoot);
        Elevator east = AddElevator("bridge_east", 1, 0, -1f, new Vector3(0f, 1f, 0f),
            false, false, false, 2f, 3f, terrainRoot);
        AddSwitch("bridge_switch", -2, 1, 0f, 1f, new[] { west, east }, terrainRoot);

        AddPlayer(-4, 0, 0f);
        AddGoal(3, 0, 0f);
        Save(scene, 3);
    }

    private static void BuildLevel04()
    {
        Scene scene = CreateScene(4, -4, 6, -1, 1);
        HashSet<string> cells = new HashSet<string>();

        AddRect(cells, terrainRoot, -4, -1, -1, 1, 0f);
        AddRect(cells, terrainRoot, 3, 6, -1, 1, 0f);
        AddElevator("carrier_platform", 0, 0, 0f, new Vector3(2f, 0f, 0f),
            true, false, false, 2f, 3f, terrainRoot);

        AddPlayer(-4, 0, 0f);
        AddGoal(4, 0, 0f);
        Save(scene, 4);
    }

    private static void BuildLevel05()
    {
        Scene scene = CreateScene(5, -5, 8, -1, 1);
        HashSet<string> cells = new HashSet<string>();

        AddRect(cells, terrainRoot, -5, -1, -1, 1, 0f);
        AddRect(cells, terrainRoot, 3, 8, -1, 1, 0f);

        AddElevator("returning_platform", 0, 0, 0f, new Vector3(2f, 0f, 0f),
            true, true, true, 2f, 3f, terrainRoot);
        Elevator gate = AddMovingBlock("passage_gate", 3, 0, 0f, new Vector3(0f, -1f, 0f),
            false, false, 2f, terrainRoot);
        AddSwitch("prerequisite_switch", -3, 1, 0f, 1f, new[] { gate }, terrainRoot);

        AddPlayer(-5, 0, 0f);
        AddGoal(7, 0, 0f);
        Save(scene, 5);
    }

    private static void BuildLevel06()
    {
        Scene scene = CreateScene(6, 0, 8, 0, 4);
        HashSet<string> cells = new HashSet<string>();

        for (int x = 0; x <= 3; x++) AddTile(cells, terrainRoot, x, 0, 0f);
        for (int x = 2; x <= 5; x++) AddTile(cells, terrainRoot, x, 1, 0f);
        AddRect(cells, terrainRoot, 4, 8, 2, 4, 0f);
        AddObstacle("hill_lower", 4, 0, 0f, terrainRoot);
        AddObstacle("hill_upper", 6, 1, 0f, terrainRoot);

        AddPlayer(0, 0, 0f);
        AddBox("lower_box", 2, 0, 0f);
        AddBox("upper_box", 4, 1, 0f);
        AddGoal(6, 3, 0f);
        Save(scene, 6);
    }

    private static void BuildLevel07()
    {
        Scene scene = CreateScene(7, 0, 7, 0, 6);
        HashSet<string> cells = new HashSet<string>();

        for (int x = 0; x <= 2; x++) AddTile(cells, terrainRoot, x, 0, 0f);
        for (int z = 0; z <= 2; z++) AddTile(cells, terrainRoot, 4, z, 0f);
        AddRect(cells, terrainRoot, 3, 7, 4, 6, 0f);
        CreateTerrainInstance("ditch_horizontal_floor", 3, 0, -1f, terrainRoot);
        CreateTerrainInstance("ditch_vertical_floor", 4, 3, -1f, terrainRoot);

        AddPlayer(0, 0, 0f);
        AddBox("horizontal_box", 2, 0, 0f);
        AddBox("vertical_box", 4, 2, 0f);
        AddGoal(6, 5, 0f);
        Save(scene, 7);
    }

    private static void BuildLevel08()
    {
        Scene scene = CreateScene(8, 0, 9, 0, 3);
        HashSet<string> cells = new HashSet<string>();

        for (int x = 0; x <= 3; x++) AddTile(cells, terrainRoot, x, 3, 0f);
        for (int x = 1; x <= 3; x++) AddTile(cells, terrainRoot, x, 2, 0f);
        AddTile(cells, terrainRoot, 1, 1, 0f);
        AddTile(cells, terrainRoot, 3, 1, 0f);
        AddTile(cells, terrainRoot, 3, 0, 0f);
        AddRect(cells, terrainRoot, 5, 9, 0, 2, 0f);

        AddObstacle("box_limit_north_west", 0, 2, 0f, terrainRoot);
        AddObstacle("box_limit_west", 0, 1, 0f, terrainRoot);
        AddObstacle("box_limit_east", 2, 1, 0f, terrainRoot);
        AddObstacle("box_limit_south", 1, 0, 0f, terrainRoot);

        Elevator bridge = AddElevator("reversible_bridge", 4, 1, -1f, new Vector3(0f, 1f, 0f),
            false, true, false, 1f, 3f, terrainRoot);
        AddSwitch("bridge_pressure_plate", 1, 1, 0f, 1f, new[] { bridge }, terrainRoot);

        AddPlayer(0, 3, 0f);
        AddBox("holding_box", 1, 2, 0f);
        AddGoal(7, 1, 0f);
        Save(scene, 8);
    }

    private static void BuildLevel09()
    {
        Scene scene = CreateScene(9, 0, 11, 0, 3);
        HashSet<string> cells = new HashSet<string>();

        for (int x = 0; x <= 3; x++) AddTile(cells, terrainRoot, x, 0, 0f);
        AddTile(cells, terrainRoot, 3, 1, 0f);
        for (int x = 3; x <= 6; x++) AddTile(cells, terrainRoot, x, 2, 0f);
        AddRect(cells, terrainRoot, 8, 11, 1, 3, 0f);
        CreateTerrainInstance("hidden_switch_pit_floor", 4, 0, -1f, terrainRoot);

        Elevator bridge = AddElevator("chapter_exit_bridge", 7, 2, -1f, new Vector3(0f, 1f, 0f),
            false, false, false, 1f, 3f, terrainRoot);
        AddSwitch("hidden_switch", 4, 0, -1f, 0.4f, new[] { bridge }, terrainRoot);

        AddPlayer(0, 0, 0f);
        AddBox("key_box", 2, 0, 0f);
        AddGoal(10, 2, 0f);
        Save(scene, 9);
    }

    private static Scene CreateScene(int level, int minX, int maxX, int minZ, int maxZ)
    {
        Scene scene = EditorSceneManager.OpenScene(TemplateScenePath, OpenSceneMode.Single);
        RemoveTemplateGameplayObjects(scene);
        PrepareTerrainRoot(scene);
        SetupEnvironment(level, minX, maxX, minZ, maxZ);
        return scene;
    }

    private static void PrepareTerrainRoot(Scene scene)
    {
        terrainRoot = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Terrain")
            {
                terrainRoot = root.transform;
                break;
            }
        }

        if (terrainRoot == null)
            throw new System.InvalidOperationException(
                $"{TemplateScenePath} must contain a root GameObject named 'Terrain'.");

        terrainSeed = null;
        foreach (Transform child in terrainRoot)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == TerrainPrefabPath)
            {
                terrainSeed = child.gameObject;
                break;
            }
        }

        if (terrainSeed == null)
            throw new System.InvalidOperationException(
                "The 'Terrain' root must contain a CubicTerrain seed instance to duplicate from.");
    }

    private static void SetupEnvironment(int level, int minX, int maxX, int minZ, int maxZ)
    {
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        float width = maxX - minX + 1f;
        float depth = maxZ - minZ + 1f;

        Camera camera = Camera.main;
        if (camera == null)
            throw new System.InvalidOperationException(
                $"{TemplateScenePath} must contain a camera tagged MainCamera.");

        camera.orthographicSize = Mathf.Max(4.5f, (depth + 4f) * 0.7f, (width + 4f) / 3.2f);
        camera.transform.position = new Vector3(centerX - 3.73f, 4.23f, centerZ - 7.05f);

        GameObject metadata = new GameObject($"Chapter1_Level{level:00}_RCMap");
        metadata.transform.position = Vector3.zero;
    }

    private static readonly string[] DecorationPrefabPaths =
    {
        "Assets/Prefabs/CubicWorld/Water.prefab",
        "Assets/Prefabs/CubicWorld/Tree.prefab",
        "Assets/Prefabs/CubicWorld/Rock1.prefab",
        "Assets/Prefabs/CubicWorld/Rock2.prefab",
        "Assets/Prefabs/CubicWorld/Rock3.prefab",
    };

    private static void RemoveTemplateGameplayObjects(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (prefabPath == PlayerPrefabPath ||
                prefabPath == BoxPrefabPath ||
                System.Array.IndexOf(DecorationPrefabPaths, prefabPath) >= 0)
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static void Save(Scene scene, int level)
    {
        if (terrainSeed != null)
        {
            Object.DestroyImmediate(terrainSeed);
            terrainSeed = null;
        }
        terrainRoot = null;

        EditorSceneManager.MarkSceneDirty(scene);
        string path = $"{SceneFolder}/Chapter1_Scene{level}.unity";
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new System.InvalidOperationException($"Failed to save {path}");
    }

    private static GameObject DuplicateSeed(string name, Transform parent)
    {
        GameObject source = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(terrainSeed);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        instance.name = name;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    private static void AddRect(HashSet<string> cells, Transform parent, int minX, int maxX,
        int minZ, int maxZ, float surfaceY)
    {
        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
            AddTile(cells, parent, x, z, surfaceY);
    }

    private static void AddTile(HashSet<string> cells, Transform parent, int x, int z, float surfaceY)
    {
        string key = $"{x}:{z}:{surfaceY:0.00}";
        if (!cells.Add(key)) return;
        CreateTerrainInstance($"Tile_x{x:+00;-00;+00}_z{z:+00;-00;+00}",
            x, z, surfaceY, parent);
    }

    private static GameObject CreateTerrainInstance(string name, int x, int z, float surfaceY,
        Transform parent)
    {
        GameObject tile = DuplicateSeed(name, parent);
        tile.transform.position = new Vector3(x, surfaceY - 0.5f, z);
        tile.transform.rotation = Quaternion.identity;
        tile.isStatic = true;
        return tile;
    }

    private static void AddObstacle(string name, int x, int z, float surfaceY, Transform parent)
    {
        GameObject obstacle = DuplicateSeed(name, parent);
        obstacle.transform.position = new Vector3(x, surfaceY + 0.5f, z);
        obstacle.transform.rotation = Quaternion.identity;
        obstacle.isStatic = true;
    }

    private static Player AddPlayer(int x, int z, float surfaceY)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject playerObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        playerObject.name = "Player";
        playerObject.transform.position = new Vector3(x, surfaceY + 0.5f, z);
        playerObject.transform.rotation = Quaternion.identity;
        return playerObject.GetComponent<Player>();
    }

    private static SceneSwitcher AddGoal(int x, int z, float surfaceY)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        GameObject goalObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        goalObject.name = "SceneSwitcher";
        goalObject.transform.position = new Vector3(x, surfaceY + 0.1f, z);
        goalObject.transform.rotation = Quaternion.identity;
        return goalObject.GetComponent<SceneSwitcher>();
    }

    private static PushableBlock AddBox(string name, int x, int z, float surfaceY)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoxPrefabPath);
        GameObject boxObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        boxObject.name = name;
        boxObject.transform.position = new Vector3(x, surfaceY + 0.5f, z);
        boxObject.transform.rotation = Quaternion.identity;
        return boxObject.GetComponent<PushableBlock>();
    }

    private static Elevator AddElevator(string name, int x, int z, float surfaceY, Vector3 offset,
        bool selfTriggered, bool reset, bool resetOnArrival, float moveDuration, float resetDelay,
        Transform parent)
    {
        GameObject platform = DuplicateSeed(name, parent);
        platform.transform.position = new Vector3(x, surfaceY - 0.5f, z);
        platform.transform.rotation = Quaternion.identity;

        BoxCollider trigger = platform.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.75f, 0f);
        trigger.size = new Vector3(0.9f, 0.5f, 0.9f);

        Elevator elevator = platform.AddComponent<Elevator>();
        ConfigureElevator(elevator, platform, offset, selfTriggered, reset, resetOnArrival,
            moveDuration, resetDelay);
        return elevator;
    }

    private static Elevator AddMovingBlock(string name, int x, int z, float surfaceY, Vector3 offset,
        bool reset, bool resetOnArrival, float moveDuration, Transform parent)
    {
        GameObject block = DuplicateSeed(name, parent);
        block.transform.position = new Vector3(x, surfaceY + 0.5f, z);
        block.transform.rotation = Quaternion.identity;

        Elevator elevator = block.AddComponent<Elevator>();
        ConfigureElevator(elevator, block, offset, false, reset, resetOnArrival, moveDuration, 3f);
        return elevator;
    }

    private static void ConfigureElevator(Elevator elevator, GameObject movedObject, Vector3 offset,
        bool selfTriggered, bool reset, bool resetOnArrival, float moveDuration, float resetDelay)
    {
        SerializedObject serialized = new SerializedObject(elevator);
        serialized.FindProperty("elevator").objectReferenceValue = movedObject;
        serialized.FindProperty("offset").vector3Value = offset;
        serialized.FindProperty("reset").boolValue = reset;
        serialized.FindProperty("resetDelay").floatValue = resetDelay;
        serialized.FindProperty("resetOnArrival").boolValue = resetOnArrival;
        serialized.FindProperty("switcherFollow").boolValue = false;
        serialized.FindProperty("moveDuration").floatValue = moveDuration;
        serialized.FindProperty("selfTriggered").boolValue = selfTriggered;
        serialized.FindProperty("requireRuneDown").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ElevatorSwitch AddSwitch(string name, int x, int z, float surfaceY,
        float holdDuration, Elevator[] targets, Transform parent)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = name;
        plate.transform.SetParent(parent);
        plate.transform.position = new Vector3(x, surfaceY + 0.05f, z);
        plate.transform.localScale = new Vector3(0.78f, 0.1f, 0.78f);
        plate.GetComponent<BoxCollider>().isTrigger = true;

        ElevatorSwitch elevatorSwitch = plate.AddComponent<ElevatorSwitch>();
        SerializedObject serialized = new SerializedObject(elevatorSwitch);
        SerializedProperty targetProperty = serialized.FindProperty("targets");
        targetProperty.arraySize = targets.Length;
        for (int i = 0; i < targets.Length; i++)
        {
            SerializedProperty target = targetProperty.GetArrayElementAtIndex(i);
            target.FindPropertyRelative("elevator").objectReferenceValue = targets[i];
            target.FindPropertyRelative("delay").floatValue = 0f;
        }
        serialized.FindProperty("holdDuration").floatValue = holdDuration;
        serialized.FindProperty("requireRuneDown").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return elevatorSwitch;
    }

    private static void LoadExistingAssets()
    {
        RequirePrefab(TerrainPrefabPath);
        RequirePrefab(PlayerPrefabPath);
        RequirePrefab(GoalPrefabPath);
        RequirePrefab(BoxPrefabPath);
    }

    private static GameObject RequirePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new System.InvalidOperationException($"Required prefab was not found: {path}");
        return prefab;
    }

}
