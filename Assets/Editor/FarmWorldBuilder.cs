using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the expanded farm world on top of the existing Gameplay scene.
/// Menu: Tools > World > Build Farm World
/// </summary>
public static class FarmWorldBuilder
{
    // World extents
    private const float WorldMinX = -24f;
    private const float WorldMaxX =  24f;
    private const float WorldMinY = -16f;
    private const float WorldMaxY =  16f;

    // Sprite paths
    private const string SpriteFolder = "Assets/_Project/Art/World/";
    private const string ScenePath    = "Assets/_Project/Scenes/Gameplay.unity";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";

    // Quest asset paths
    private const string AppleQuestPath    = "Assets/_Project/ScriptableObjects/Quests/AppleHarvest.asset";
    private const string ChickenQuestPath  = "Assets/_Project/ScriptableObjects/Quests/CollectChickens.asset";
    private const string AppleItemPath     = "Assets/_Project/ScriptableObjects/Items/Apple.asset";
    private const string SoapboxPlankPath  = "Assets/_Project/ScriptableObjects/Items/SoapboxPlank.asset";
    private const string SoapboxWheelPath  = "Assets/_Project/ScriptableObjects/Items/SoapboxWheel.asset";
    private const string SoapboxAxlePath   = "Assets/_Project/ScriptableObjects/Items/SoapboxAxle.asset";
    private const string SoapboxBearingPath = "Assets/_Project/ScriptableObjects/Items/SoapboxBearings.asset";
    private const string HearthTeaPath     = "Assets/_Project/ScriptableObjects/Items/HearthTea.asset";
    private const string EchoShardPath     = "Assets/_Project/ScriptableObjects/Items/EchoShard.asset";
    private const string ItemRegistryPath  = "Assets/_Project/ScriptableObjects/Items/ItemRegistry.asset";

    // ── Generated root names — used by both builder and cleanup ───────────────
    private static readonly string[] GeneratedRoots =
        { "Environment", "Interactables", "NPCs", "Quest Guidance", "Soapbox Run", "Farm Visual Dressing" };

    [MenuItem("Tools/World/Build Farm World")]
    public static void BuildFarmWorld()
    {
        EnsureFolders();
        GenerateSprites();
        EnsureQuestMarkerSprite();
        EnsureSoapboxSprites();
        ConfigureWorldSpriteImporters();
        EnsureAppleItem();
        EnsureSoapboxItems();
        EnsureAppleHarvestQuest();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureSoapboxSystems();

        // Remove legacy objects (first-time run) and regenerated roots (re-run safety)
        RemoveGameObject("Collision Demo Bounds");
        RemoveGameObject("Mountain Village Playfield");
        RemoveGameObject("Gameplay HUD");
        RemoveGameObject("Old North Shrine");
        RemoveGameObject("Shade");
        CleanupPlayerPrefabCombat();
        ClearGeneratedRoots();

        BuildFloors();
        BuildBoundaryWalls();
        BuildLandmarks();
        BuildVegetation();
        BuildFences();
        BuildPaths();
        BuildSoapboxPrototype();
        PlaceInteractables();
        PlaceMischiefProps();
        PlaceExtraNPCs();
        RepositionGameElements();
        RemoveCombatFromActiveScene();
        BuildQuestGuidance();
        WireAppleQuestToFarmer();
        WireAppleQuestToQuestManager();
        PlaceGameHud();
        ConfigureCamera();

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[FarmWorldBuilder] Done. Open Gameplay.unity and press Play.");
    }

    /// <summary>
    /// Adds GameHud, PauseMenu, InventoryPanel, and camera bounds to an existing
    /// Gameplay scene without touching world geometry. Safe to run on any scene state.
    /// </summary>
    [MenuItem("Tools/World/Patch Scene Components")]
    public static void PatchSceneComponents()
    {
        EnsureFolders();
        EnsureQuestMarkerSprite();
        EnsureSoapboxSprites();
        EnsureAppleItem();
        EnsureSoapboxItems();
        EnsureAppleHarvestQuest();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureSoapboxSystems();

        // Replace legacy HUD
        RemoveGameObject("Gameplay HUD");

        PlaceGameHud();
        ConfigureCamera();
        WireAppleQuestToQuestManager();
        WireAppleQuestToFarmer();

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[FarmWorldBuilder] Scene patched — GameHud, PauseMenu, InventoryPanel, camera bounds applied.");
    }

    private static void ClearGeneratedRoots()
    {
        foreach (var rootName in GeneratedRoots)
            RemoveGameObject(rootName);
        // PlayerSpawnPoint is also generated but can live without a parent root
        RemoveGameObject("Player Spawn Point");
    }

    // -------------------------------------------------------------------------
    // Apple harvest quest
    // -------------------------------------------------------------------------

    private static void EnsureQuestMarkerSprite()
    {
        var path = SpriteFolder + "ui_quest_marker.png";
        if (File.Exists(path))
        {
            return;
        }

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color32(0, 0, 0, 0);
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
                tex.SetPixel(x, y, clear);

        var outline = new Color32(76, 45, 24, 255);
        var gold = new Color32(255, 218, 91, 255);
        var light = new Color32(255, 246, 157, 255);

        for (var y = 2; y <= 13; y++)
            for (var x = 3; x <= 12; x++)
            {
                var dx = Mathf.Abs(x - 8);
                var width = y < 8 ? y - 1 : 15 - y;
                if (dx <= width * 0.55f)
                {
                    var edge = dx >= width * 0.55f - 1f || y == 2 || y == 13;
                    tex.SetPixel(x, y, edge ? outline : gold);
                }
            }

        tex.SetPixel(7, 9, light);
        tex.SetPixel(8, 10, light);
        tex.Apply();
        WriteSprite(path, tex, 16f);
    }

    private static void EnsureSoapboxSprites()
    {
        MakeSimplePropSprite("item_plank", 18, 8, new Color32(169, 113, 55, 255), new Color32(75, 45, 25, 255));
        MakeWheelSprite("item_wheel");
        MakeSimplePropSprite("item_axle", 18, 6, new Color32(126, 132, 138, 255), new Color32(52, 56, 63, 255));
        MakeSimplePropSprite("item_bearings", 12, 10, new Color32(190, 194, 194, 255), new Color32(50, 55, 60, 255));
        MakeSoapboxCarSprite();
    }

    private static void MakeWheelSprite(string name)
    {
        var path = SpriteFolder + name + ".png";
        if (File.Exists(path)) return;

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color32(0, 0, 0, 0);
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
                tex.SetPixel(x, y, clear);

        var outline = new Color32(45, 39, 43, 255);
        var rubber = new Color32(83, 82, 87, 255);
        var hub = new Color32(207, 190, 112, 255);
        for (var y = 2; y <= 13; y++)
            for (var x = 2; x <= 13; x++)
            {
                var dx = x - 8;
                var dy = y - 8;
                var dist = dx * dx + dy * dy;
                if (dist <= 36) tex.SetPixel(x, y, dist >= 28 ? outline : rubber);
                if (dist <= 8) tex.SetPixel(x, y, hub);
            }

        tex.Apply();
        WriteSprite(path, tex, 16f);
    }

    private static void MakeSoapboxCarSprite()
    {
        var path = SpriteFolder + "soapbox_car.png";
        if (File.Exists(path)) return;

        var tex = new Texture2D(48, 24, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color32(0, 0, 0, 0);
        for (var y = 0; y < 24; y++)
            for (var x = 0; x < 48; x++)
                tex.SetPixel(x, y, clear);

        var outline = new Color32(54, 35, 29, 255);
        var wood = new Color32(187, 103, 54, 255);
        var bright = new Color32(232, 151, 77, 255);
        var wheel = new Color32(42, 42, 46, 255);
        var hub = new Color32(223, 205, 119, 255);

        for (var y = 8; y <= 14; y++)
            for (var x = 7; x <= 39; x++)
                tex.SetPixel(x, y, y == 8 || y == 14 || x == 7 || x == 39 ? outline : (y > 11 ? bright : wood));

        for (var y = 15; y <= 19; y++)
            for (var x = 14; x <= 27; x++)
                tex.SetPixel(x, y, y == 19 || x == 14 || x == 27 ? outline : bright);

        DrawWheel(tex, 13, 6, wheel, hub);
        DrawWheel(tex, 34, 6, wheel, hub);
        tex.Apply();
        WriteSprite(path, tex, 16f);
    }

    private static void DrawWheel(Texture2D tex, int cx, int cy, Color32 wheel, Color32 hub)
    {
        for (var y = cy - 4; y <= cy + 4; y++)
            for (var x = cx - 4; x <= cx + 4; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                var dist = dx * dx + dy * dy;
                if (dist <= 16) tex.SetPixel(x, y, wheel);
                if (dist <= 4) tex.SetPixel(x, y, hub);
            }
    }

    private static void EnsureAppleItem()
    {
        var iconPath = SpriteFolder + "item_apple.png";
        if (!File.Exists(iconPath))
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color32(0, 0, 0, 0);
            for (var y = 0; y < 16; y++)
                for (var x = 0; x < 16; x++)
                    tex.SetPixel(x, y, clear);

            var red = new Color32(205, 63, 59, 255);
            var darkRed = new Color32(122, 37, 43, 255);
            var shine = new Color32(246, 130, 105, 255);
            var leaf = new Color32(68, 148, 72, 255);
            var stem = new Color32(91, 61, 33, 255);

            for (var y = 4; y <= 12; y++)
                for (var x = 4; x <= 12; x++)
                {
                    var dx = x - 8;
                    var dy = y - 8;
                    if (dx * dx + dy * dy <= 22)
                        tex.SetPixel(x, y, red);
                }

            tex.SetPixel(6, 11, darkRed);
            tex.SetPixel(5, 9, darkRed);
            tex.SetPixel(10, 5, darkRed);
            tex.SetPixel(7, 10, shine);
            tex.SetPixel(8, 11, shine);
            tex.SetPixel(8, 13, stem);
            tex.SetPixel(9, 13, stem);
            tex.SetPixel(10, 12, leaf);
            tex.SetPixel(11, 12, leaf);
            tex.Apply();
            WriteSprite(iconPath, tex, 16f);
            AssetDatabase.ImportAsset(iconPath);
        }

        var apple = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AppleItemPath);
        if (apple == null)
        {
            apple = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(apple, AppleItemPath);
        }

        var appleIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        var so = new SerializedObject(apple);
        so.FindProperty("id").stringValue = "apple";
        so.FindProperty("displayName").stringValue = "Aeble";
        so.FindProperty("value").intValue = 2;
        so.FindProperty("icon").objectReferenceValue = appleIcon;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(apple);

        AddItemToRegistry(apple);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureSoapboxItems()
    {
        EnsureItem(SoapboxPlankPath, "soapbox_plank", "Traebraet", 3, "item_plank");
        EnsureItem(SoapboxWheelPath, "soapbox_wheel", "Hjul", 5, "item_wheel");
        EnsureItem(SoapboxAxlePath, "soapbox_axle", "Aksel", 6, "item_axle");
        EnsureItem(SoapboxBearingPath, "soapbox_bearings", "Lejer", 8, "item_bearings");
        AssetDatabase.SaveAssets();
    }

    private static ItemDefinition EnsureItem(string assetPath, string id, string displayName, int value, string spriteName)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(item, assetPath);
        }

        var so = new SerializedObject(item);
        so.FindProperty("id").stringValue = id;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("value").intValue = value;
        so.FindProperty("icon").objectReferenceValue = LoadSprite(spriteName);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        AddItemToRegistry(item);
        return item;
    }

    private static void ConfigureSoapboxSystems()
    {
        var systems = GameObject.Find("Core Systems");
        if (systems == null)
        {
            systems = new GameObject("Core Systems");
        }

        if (systems.GetComponent<SoapboxRunController>() == null)
        {
            systems.AddComponent<SoapboxRunController>();
        }

        var progress = systems.GetComponent<SoapboxProgress>();
        if (progress == null)
        {
            progress = systems.AddComponent<SoapboxProgress>();
        }

        var progressSo = new SerializedObject(progress);
        progressSo.FindProperty("plankItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(SoapboxPlankPath);
        progressSo.FindProperty("wheelItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(SoapboxWheelPath);
        progressSo.FindProperty("axleItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(SoapboxAxlePath);
        progressSo.FindProperty("bearingsItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(SoapboxBearingPath);
        progressSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(progress);

        var runController = systems.GetComponent<SoapboxRunController>();
        var runSo = new SerializedObject(runController);
        runSo.FindProperty("carSprite").objectReferenceValue = LoadSprite("soapbox_car");
        runSo.FindProperty("runStart").vector2Value = new Vector2(-60f, -31f);
        runSo.FindProperty("runCameraBounds").boundsValue = new Bounds(new Vector3(15f, -32f, 0f), new Vector3(165f, 24f, 0f));
        runSo.FindProperty("minimumRunTime").floatValue = 2.5f;
        runSo.FindProperty("stopSpeed").floatValue = 0.35f;
        runSo.FindProperty("jumpImpulse").floatValue = 6.2f;
        runSo.FindProperty("runBoostMultiplier").floatValue = 1.35f;
        runSo.FindProperty("failY").floatValue = -45f;
        runSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(runController);
    }

    private static void AddItemToRegistry(ItemDefinition item)
    {
        var registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
        if (registry == null || item == null)
        {
            return;
        }

        var so = new SerializedObject(registry);
        var items = so.FindProperty("items");
        for (var i = 0; i < items.arraySize; i++)
        {
            if (items.GetArrayElementAtIndex(i).objectReferenceValue == item)
            {
                return;
            }
        }

        items.InsertArrayElementAtIndex(items.arraySize);
        items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = item;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
    }

    private static void EnsureAppleHarvestQuest()
    {
        if (!File.Exists(AppleQuestPath))
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<QuestDefinition>(), AppleQuestPath);
        }

        var quest  = AssetDatabase.LoadAssetAtPath<QuestDefinition>(AppleQuestPath);
        var qObj   = new SerializedObject(quest);
        qObj.FindProperty("questId").stringValue    = "apple_harvest";
        qObj.FindProperty("title").stringValue      = "Aebletrae-hoest";
        qObj.FindProperty("description").stringValue =
            "Gaardejeren vil have aeblet hjem. Ryst tre aebletrae i frugtskoven.";

        var steps = qObj.FindProperty("steps");
        steps.arraySize = 1;
        var step = steps.GetArrayElementAtIndex(0);
        step.FindPropertyRelative("description").stringValue  = "Ryst 3 aebletrae.";
        step.FindPropertyRelative("requiredCount").intValue   = 3;

        var rewards = qObj.FindProperty("rewardItems");
        var echoShard = AssetDatabase.LoadAssetAtPath<ItemDefinition>(EchoShardPath);
        rewards.arraySize = echoShard != null ? 1 : 0;
        if (echoShard != null) rewards.GetArrayElementAtIndex(0).objectReferenceValue = echoShard;

        qObj.FindProperty("rewardCoins").intValue = 8;
        qObj.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(quest);
        AssetDatabase.SaveAssets();
    }

    private static void WireAppleQuestToQuestManager()
    {
        var systems = GameObject.Find("Core Systems");
        if (systems == null) return;
        var qm = systems.GetComponent<QuestManager>();
        if (qm == null) return;

        var chickenQuest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(ChickenQuestPath);
        var appleQuest   = AssetDatabase.LoadAssetAtPath<QuestDefinition>(AppleQuestPath);
        if (chickenQuest == null || appleQuest == null) return;

        var so = new SerializedObject(qm);
        var quests = so.FindProperty("allQuests");
        quests.arraySize = 2;
        quests.GetArrayElementAtIndex(0).objectReferenceValue = chickenQuest;
        quests.GetArrayElementAtIndex(1).objectReferenceValue = appleQuest;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(qm);
    }

    private static void WireAppleQuestToFarmer()
    {
        var farmer = GameObject.Find("Gaardejer - Chicken Quest");
        if (farmer == null) return;
        var npc = farmer.GetComponent<NPCInteractable>();
        if (npc == null) return;

        var appleQuest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(AppleQuestPath);
        if (appleQuest == null) return;

        var so = new SerializedObject(npc);
        so.FindProperty("secondQuestToGive").objectReferenceValue = appleQuest;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(npc);
    }

    // -------------------------------------------------------------------------
    // Interactables — shakeable trees, signs
    // -------------------------------------------------------------------------

    private static void PlaceInteractables()
    {
        var root  = GetOrCreate("Interactables");
        var layer = LayerMask.NameToLayer("Interactable");

        // Three shakeable apple trees in the orchard (spread out so player must explore)
        PlaceShakeableTree(root, "AppleTree_A", layer, new Vector2(-20f,  5f));
        PlaceShakeableTree(root, "AppleTree_B", layer, new Vector2(-16f,  9f));
        PlaceShakeableTree(root, "AppleTree_C", layer, new Vector2(-20f, 12f));

        // Signs at key navigation points
        PlaceSign(root, "Sign_Farmyard",  layer, new Vector2(  0f, -5.5f),
            "Gaardspladsen — Tal med gaardejeren for at faa arbejde.");
        PlaceSign(root, "Sign_Orchard",   layer, new Vector2( -8f,  2.5f),
            "Frugtskoven — Gaardejeren er stolt af sine aebletrae.");
        PlaceSign(root, "Sign_Barn",      layer, new Vector2( -6f, -1.5f),
            "Laden — Pas paa trinnet.");
        PlaceSign(root, "Sign_Garden",    layer, new Vector2(  6f, -1.5f),
            "Koekkenurternes Have — Hold stien ren.");
    }

    private static void PlaceShakeableTree(Transform parent, string name, int layer, Vector2 pos)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "tree_apple.png");
        sr.sortingOrder = 15;
        AddYSort(sr, true, -0.55f);

        var col     = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.7f;

        var tree = go.AddComponent<ShakeableTree>();
        var so = new SerializedObject(tree);
        so.FindProperty("appleItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AppleItemPath);
        so.FindProperty("pickupSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "item_apple.png");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PlaceSign(Transform parent, string name, int layer, Vector2 pos, string text)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "prop_signpost.png");
        sr.sortingOrder = 5;
        AddYSort(sr, false, -0.25f);

        var col     = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(0.8f, 1.2f);

        var sign = go.AddComponent<SignInteractable>();
        var so   = new SerializedObject(sign);
        so.FindProperty("signText").stringValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // -------------------------------------------------------------------------
    // HUD
    // -------------------------------------------------------------------------

    private static void PlaceGameHud()
    {
        // GameHud
        if (GameObject.Find("Game HUD") == null)
        {
            var go = new GameObject("Game HUD");
            go.AddComponent<GameHud>();
        }

        // PauseMenu and InventoryPanel live on the player (need PlayerInputReader)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.GetComponent<PauseMenu>()      == null) player.AddComponent<PauseMenu>();
            if (player.GetComponent<InventoryPanel>() == null) player.AddComponent<InventoryPanel>();
        }
    }

    // -------------------------------------------------------------------------
    // Floors
    // -------------------------------------------------------------------------

    private static void BuildFloors()
    {
        var root = GetOrCreate("Environment/Floors");

        // Farmyard hub — packed dirt, warm tan
        MakeFloor(root, "Floor_Farmyard",     "floor_dirt",    new Vector2(0f,   -3f),  new Vector2(14f, 10f), -10);
        // Barn area — dark earth
        MakeFloor(root, "Floor_Barn",         "floor_soil",    new Vector2(-16f, -5f),  new Vector2(16f, 18f), -10);
        // Vegetable garden — mid green
        MakeFloor(root, "Floor_Garden",       "floor_garden",  new Vector2(16f,  -5f),  new Vector2(16f, 18f), -10);
        // Orchard — light grass
        MakeFloor(root, "Floor_Orchard",      "floor_grass",   new Vector2(-13f,  9f),  new Vector2(22f, 14f), -10);
        // Hayfield — golden
        MakeFloor(root, "Floor_Hayfield",     "floor_hay",     new Vector2(13f,   9f),  new Vector2(22f, 14f), -10);
        // Forest edge — deep green
        MakeFloor(root, "Floor_Forest",       "floor_forest",  new Vector2(0f,   15f),  new Vector2(48f,  2f), -10);
        // Road — grey-brown
        MakeFloor(root, "Floor_Road",         "floor_road",    new Vector2(0f,  -11f),  new Vector2(4f,  10f), -9);
        // Base fill so no void shows
        MakeFloor(root, "Floor_Base",         "floor_grass",   new Vector2(0f,    0f),  new Vector2(48f, 32f), -11);
    }

    private static void MakeFloor(Transform parent, string objName, string spriteName, Vector2 center, Vector2 size, int sortOrder)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);
        sr.sortingOrder = sortOrder;

        // Scale the sprite to match the desired world size.
        // Sprites are 16x16 px at 16 PPU → 1 unit each.
        var spriteSize = sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
        go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
    }

    // -------------------------------------------------------------------------
    // Boundary walls (invisible colliders)
    // -------------------------------------------------------------------------

    private static void BuildBoundaryWalls()
    {
        var root = GetOrCreate("Environment/Boundary");
        var layer = LayerMask.NameToLayer("Solid");

        MakeWall(root, "Wall_N", layer, new Vector2(0f,       WorldMaxY + 0.25f), new Vector2(WorldMaxX - WorldMinX, 0.5f));
        MakeWall(root, "Wall_S", layer, new Vector2(0f,       WorldMinY - 0.25f), new Vector2(WorldMaxX - WorldMinX, 0.5f));
        MakeWall(root, "Wall_W", layer, new Vector2(WorldMinX - 0.25f, 0f),       new Vector2(0.5f, WorldMaxY - WorldMinY));
        MakeWall(root, "Wall_E", layer, new Vector2(WorldMaxX + 0.25f, 0f),       new Vector2(0.5f, WorldMaxY - WorldMinY));
    }

    private static void MakeWall(Transform parent, string name, int layer, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    // -------------------------------------------------------------------------
    // Landmarks — farmhouse, barn, well, chicken coop, ruins, signpost
    // -------------------------------------------------------------------------

    private static void BuildLandmarks()
    {
        var root = GetOrCreate("Environment/Landmarks");
        var solid = LayerMask.NameToLayer("Solid");

        // Farmhouse (NE corner of farmyard)
        MakeProp(root, "Farmhouse",    "prop_farmhouse",  new Vector2( 4f,   1.5f), new Vector2(4.4f, 1.6f), solid, 10);
        // Barn (west zone, centre)
        MakeProp(root, "Barn",         "prop_barn",       new Vector2(-16f,  0f),   new Vector2(5.2f, 1.7f), solid, 10);
        // Well (farmyard centre-left)
        MakeProp(root, "Well",         "prop_well",       new Vector2(-2f,   0f),   new Vector2(1.4f, 1f),   solid,  8);
        // Chicken coop (farmyard NW)
        MakeProp(root, "ChickenCoop",  "prop_coop",       new Vector2(-5f,   1.5f), new Vector2(3.1f, 1.1f), solid,  9);
        // Small market/meeting spot near the eastern garden
        MakeProp(root, "MarketStall",  "prop_market",     new Vector2( 13f,  0.8f), new Vector2(2.0f, 1.0f), solid,  9);
        MakeProp(root, "ToolShed",     "prop_shed",       new Vector2(-20f, -8.5f), new Vector2(2.2f, 1.3f), solid,  9);
        MakeProp(root, "Outhouse",     "prop_outhouse",   new Vector2( 20f, -8.5f), new Vector2(1.1f, 1.0f), solid,  9);
        // Old stone ruin (forest edge)
        MakeProp(root, "Ruin_Wall_A",  "prop_ruin",       new Vector2(-8f,  14.5f), new Vector2(3f,   1f),   solid,  5);
        MakeProp(root, "Ruin_Wall_B",  "prop_ruin",       new Vector2( 6f,  14.5f), new Vector2(2f,   1f),   solid,  5);
        // Large oak (orchard centre)
        MakeProp(root, "BigOak",       "tree_oak",        new Vector2(-10f,  8f),   new Vector2(2.5f, 3f),   solid, 12);
        // Signpost (road junction)
        MakeProp(root, "Signpost",     "prop_signpost",   new Vector2( 0f,  -6.5f), new Vector2(0.6f, 1.2f), solid,  5);
        // Scarecrow (vegetable garden)
        MakeProp(root, "Scarecrow",    "prop_scarecrow",  new Vector2(14f,  -3f),   new Vector2(0.8f, 1.6f), solid,  5);
        // Hay bales (barn area)
        MakeProp(root, "HayBale_A",   "prop_haybale",    new Vector2(-11f, -6f),   new Vector2(1.2f, 0.8f), solid,  3);
        MakeProp(root, "HayBale_B",   "prop_haybale",    new Vector2(-12.5f,-6f),  new Vector2(1.2f, 0.8f), solid,  3);
        // Cart (farmyard south)
        MakeProp(root, "OldCart",     "prop_cart",       new Vector2( 3f,  -5f),   new Vector2(1.8f, 1f),   solid,  3);
        // Water trough (barn area)
        MakeProp(root, "Trough",      "prop_trough",     new Vector2(-13f,  2f),   new Vector2(1.5f, 0.6f), solid,  3);

        MakeDecor(root, "Pond",       "decor_pond",      new Vector2(18f,  9f), -8);
        MakeDecor(root, "Bridge",     "prop_bridge",     new Vector2( 0f, -9f), -3);
        MakeDecor(root, "Dock",       "prop_dock",       new Vector2(20f,  7f), -2);
        MakeDecor(root, "Bench",      "prop_bench",      new Vector2( 7f, -2f),  6);
        MakeDecor(root, "LampPost",   "prop_lamp_post",  new Vector2( 1f, -6f),  7);
        MakeDecor(root, "AppleBasket","prop_apple_basket", new Vector2(-18f, 7f), 7);
        MakeDecor(root, "Mailbox",    "prop_mailbox",    new Vector2( 1.5f, 0.8f), 7);
    }

    private static void MakeProp(Transform parent, string name, string spriteName, Vector2 pos, Vector2 colliderSize, int layer, int sortOrder)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);
        sr.sortingOrder = sortOrder;
        AddYSort(sr, false, -colliderSize.y * 0.5f);

        // Only the object's foot/base blocks the player; roofs and treetops should not.
        AddFootprintCollider(go, sr, colliderSize);
    }

    // -------------------------------------------------------------------------
    // Vegetation — trees, tall grass, crop rows, apple trees
    // -------------------------------------------------------------------------

    private static void BuildVegetation()
    {
        var root = GetOrCreate("Environment/Vegetation");

        // Forest edge — dense row of trees
        float[] forestX = { -22f, -18f, -14f, -10f, -6f, -2f, 2f, 6f, 10f, 14f, 18f, 22f };
        foreach (var x in forestX)
            MakeDecor(root, "ForestTree", "tree_pine", new Vector2(x, 15f), 18);

        // Orchard apple trees — grid
        for (var row = 0; row < 2; row++)
            for (var col2 = 0; col2 < 4; col2++)
                MakeDecor(root, "AppleTree", "tree_apple",
                    new Vector2(-20f + col2 * 3.5f, 5f + row * 4f), 15);

        // Hayfield — tall grass clusters (add sway)
        float[] grassX = { 8f, 11f, 14f, 17f, 20f, 9f, 15f, 21f };
        float[] grassY = { 3f, 6f,  3f,  8f,  5f,  10f, 11f, 9f };
        for (var i = 0; i < grassX.Length; i++)
            MakeDecor(root, "TallGrass", "decor_tallgrass", new Vector2(grassX[i], grassY[i]), 6)
                .AddComponent<GrassSwayer>();

        // Garden crop rows
        for (var row = 0; row < 4; row++)
            MakeDecor(root, "CropRow", "decor_crops", new Vector2(13f + row * 2.5f, -8f), 5);

        // Farmyard scattered bushes
        MakeDecor(root, "Bush_A", "decor_bush", new Vector2(-6f, -5f), 5);
        MakeDecor(root, "Bush_B", "decor_bush", new Vector2( 5f,  2f), 5);
        MakeDecor(root, "Bush_C", "decor_bush", new Vector2(-3f,  3f), 5);

        // Rocks
        MakeDecor(root, "Rock_A", "decor_rock", new Vector2(-19f,  3f), 4);
        MakeDecor(root, "Rock_B", "decor_rock", new Vector2( 20f, -2f), 4);
        MakeDecor(root, "Rock_C", "decor_rock", new Vector2(  8f, 13f), 4);
        MakeDecor(root, "Log_A", "decor_log", new Vector2(-21f, 12.8f), 6);
        MakeDecor(root, "Stump_A", "decor_stump", new Vector2(-6f, 10.5f), 6);
        MakeDecor(root, "Mushroom_A", "decor_mushroom", new Vector2(-4.5f, 11.4f), 6);
        MakeDecor(root, "BerryBush_A", "decor_berry_bush", new Vector2(-17.5f, 6.8f), 6);
        MakeDecor(root, "BlueBush_A", "decor_blue_bush", new Vector2(16.5f, 8.2f), 6);
        MakeDecor(root, "Flowers_Yellow_A", "decor_flowers_yellow", new Vector2(5.5f, -4.8f), 6);
        MakeDecor(root, "Flowers_White_A", "decor_flowers_white", new Vector2(-2.5f, -5.1f), 6);
        MakeDecor(root, "Flowers_Blue_A", "decor_flowers_blue", new Vector2(10.5f, 2.8f), 6);

        // Stone wall along hayfield south edge
        for (var i = 0; i < 6; i++)
            MakeDecor(root, "StoneWall", "decor_stonewall", new Vector2(7f + i * 2.5f, 2.2f), 6);
    }

    private static GameObject MakeDecor(Transform parent, string name, string spriteName, Vector2 pos, int sortOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);
        sr.sortingOrder = sortOrder;
        AddYSort(sr, false, 0f);
        return go;
    }

    // -------------------------------------------------------------------------
    // Fences — garden border, barn yard
    // -------------------------------------------------------------------------

    private static void BuildFences()
    {
        var root   = GetOrCreate("Environment/Fences");
        var solid  = LayerMask.NameToLayer("Solid");

        // Garden west fence
        for (var i = 0; i < 10; i++)
            MakeFence(root, "FenceV", solid, new Vector2(6.5f, -14f + i * 2f), false);
        // Garden north fence
        for (var i = 0; i < 7; i++)
            MakeFence(root, "FenceH", solid, new Vector2(8f + i * 2f, 2.5f), true);
        // Barn yard south fence
        for (var i = 0; i < 5; i++)
            MakeFence(root, "FenceH", solid, new Vector2(-22f + i * 2f, -14.5f), true);
    }

    private static void MakeFence(Transform parent, string name, int layer, Vector2 pos, bool horizontal)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(horizontal ? "decor_fence_h" : "decor_fence_v");
        sr.sortingOrder = 5;
        AddYSort(sr, false, 0f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = horizontal ? new Vector2(2f, 0.3f) : new Vector2(0.3f, 2f);
    }

    // -------------------------------------------------------------------------
    // Dirt paths (decorative — no colliders)
    // -------------------------------------------------------------------------

    private static void BuildPaths()
    {
        var root = GetOrCreate("Environment/Paths");

        // Main road N-S through farmyard
        for (var i = 0; i < 6; i++)
            MakeDecor(root, "Road_NS", "floor_road", new Vector2(0f, -14f + i * 2f), -8);

        // Path east into garden
        for (var i = 1; i < 5; i++)
            MakeDecor(root, "Path_E", "floor_road", new Vector2(2f + i * 2f, -3f), -8);

        // Path west into barn
        for (var i = 1; i < 5; i++)
            MakeDecor(root, "Path_W", "floor_road", new Vector2(-2f - i * 2f, -1f), -8);

        // Path north into orchard
        for (var i = 1; i < 4; i++)
            MakeDecor(root, "Path_N", "floor_road", new Vector2(-4f, 2f + i * 2f), -8);
    }

    // -------------------------------------------------------------------------
    // Soapbox prototype - top-down garage + hidden side-view run lane
    // -------------------------------------------------------------------------

    private static void BuildSoapboxPrototype()
    {
        var interactableRoot = GetOrCreate("Interactables/Soapbox");
        var layer = LayerMask.NameToLayer("Interactable");

        PlaceGarage(interactableRoot, layer, new Vector2(-18.4f, -6.7f));
        PlaceRunStarter(interactableRoot, layer, new Vector2(0f, -12.8f));
        PlaceSoapboxPart(interactableRoot, "Part_Plank_A", SoapboxPlankPath, "item_plank", new Vector2(-21f, -7.2f));
        PlaceSoapboxPart(interactableRoot, "Part_Plank_B", SoapboxPlankPath, "item_plank", new Vector2(8.2f, -2.6f));
        PlaceSoapboxPart(interactableRoot, "Part_Wheel_A", SoapboxWheelPath, "item_wheel", new Vector2(3.8f, -5.1f));
        PlaceSoapboxPart(interactableRoot, "Part_Wheel_B", SoapboxWheelPath, "item_wheel", new Vector2(17.2f, 1.8f));
        PlaceSoapboxPart(interactableRoot, "Part_Axle", SoapboxAxlePath, "item_axle", new Vector2(-12.4f, 1.2f));
        PlaceSoapboxPart(interactableRoot, "Part_Bearings", SoapboxBearingPath, "item_bearings", new Vector2(20.8f, 7.4f));

        var runRoot = GetOrCreate("Soapbox Run");
        var solid = LayerMask.NameToLayer("Solid");
        MakeRunPlatform(runRoot, "StartHill", solid, new Vector2(-58f, -33.2f), new Vector2(16f, 0.6f), -9f);
        MakeRunPlatform(runRoot, "StraightA", solid, new Vector2(-42f, -34.6f), new Vector2(19f, 0.6f), 0f);
        MakeRunPlatform(runRoot, "DipDown", solid, new Vector2(-23f, -35.6f), new Vector2(18f, 0.6f), -6f);
        MakeRunPlatform(runRoot, "BumpyMiddle", solid, new Vector2(-4f, -35.2f), new Vector2(17f, 0.6f), 7f);
        MakeRunPlatform(runRoot, "LongFlat", solid, new Vector2(15f, -34.8f), new Vector2(22f, 0.6f), 0f);
        MakeRunPlatform(runRoot, "LateHill", solid, new Vector2(38f, -35.6f), new Vector2(18f, 0.6f), -8f);
        MakeRunPlatform(runRoot, "SpeedValley", solid, new Vector2(57f, -36.8f), new Vector2(20f, 0.6f), 5f);
        MakeRunPlatform(runRoot, "JumpTable", solid, new Vector2(78f, -35.4f), new Vector2(14f, 0.6f), -3f);
        MakeRunPlatform(runRoot, "FinalRoll", solid, new Vector2(96f, -35.8f), new Vector2(24f, 0.6f), 0f);
    }

    private static void PlaceGarage(Transform parent, int layer, Vector2 pos)
    {
        var go = new GameObject("Soapbox Garage");
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("prop_shed");
        sr.sortingOrder = 12;
        AddYSort(sr, false, -0.5f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.2f, 1.5f);

        go.AddComponent<SoapboxGarageInteractable>();
    }

    private static void PlaceRunStarter(Transform parent, int layer, Vector2 pos)
    {
        var go = new GameObject("Soapbox Start Ramp");
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("soapbox_car");
        sr.sortingOrder = 12;
        AddYSort(sr, false, -0.35f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.2f, 1.2f);

        go.AddComponent<SoapboxRunStarter>();
    }

    private static void PlaceSoapboxPart(Transform parent, string name, string itemPath, string spriteName, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);
        sr.sortingOrder = 12;
        AddYSort(sr, false, 0f);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        go.AddComponent<PickupItem>().Configure(AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath), 1, 0);
    }

    private static void MakeRunPlatform(Transform parent, string name, int layer, Vector2 pos, Vector2 size, float angle)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("floor_road");
        sr.sortingOrder = -5;
        var spriteSize = sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
        go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = spriteSize;
    }

    // -------------------------------------------------------------------------
    // Extra mischief opportunities
    // -------------------------------------------------------------------------

    private static void PlaceMischiefProps()
    {
        var root  = GetOrCreate("Interactables/MischiefProps");
        var layer = LayerMask.NameToLayer("Interactable");

        // Milk can in barn yard
        PlaceBucketAt(root, "MilkCan",      layer, new Vector2(-14f,  2.5f), "Spildte maelk",        2, "prop_milk_can");
        // Barrel near garden gate
        PlaceBucketAt(root, "Barrel_Gate",  layer, new Vector2(  7f, -3.5f), "Vaeltet toendet",      1, "prop_bucket");
        // Wheelbarrow in farmyard
        PlaceBucketAt(root, "Wheelbarrow",  layer, new Vector2(  2f, -5.5f), "Skubbet trilleboe",    1, "prop_wheelbarrow");
        // Pot near farmhouse
        PlaceBucketAt(root, "FlowerPot",    layer, new Vector2(  4f,  0.5f), "Vaedet blomsterpotte", 1, "prop_seed_bag");
    }

    private static void PlaceBucketAt(Transform parent, string name, int layer, Vector2 pos,
        string reason, int mischief, string spriteName)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + spriteName + ".png");
        sr.sortingOrder = 6;
        AddYSort(sr, false, -0.2f);

        var col     = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(0.7f, 0.7f);

        var bucket = go.AddComponent<BucketInteractable>();
        var so     = new SerializedObject(bucket);
        so.FindProperty("mischiefAmount").intValue    = mischief;
        so.FindProperty("mischiefReason").stringValue = reason;
        so.FindProperty("tippedAngle").floatValue     = 85f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // -------------------------------------------------------------------------
    // Extra NPCs — farmhand in barn, cat in garden
    // -------------------------------------------------------------------------

    private static void PlaceExtraNPCs()
    {
        var root  = GetOrCreate("NPCs");
        var layer = LayerMask.NameToLayer("Interactable");

        // Farmhand Karl — just ambient dialogue in the barn area
        PlaceAmbientNPC(root, "Karl - Farmhand", layer,
            new Vector2(-17f, 2f), "Karl",
            "Laden er maaske lidt rod, men det er mit rod.");

        // Old woman near garden — hints about the orchard
        PlaceAmbientNPC(root, "Maren - Havekone", layer,
            new Vector2(11f, -2f), "Maren",
            "Aebletrae? Ja, de staar i frugtskoven deromme. Saesonen er god i aar.");
    }

    private static void PlaceAmbientNPC(Transform parent, string name, int layer,
        Vector2 pos, string speakerName, string idleText)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Art/Sprites/placeholder_npc.png");
        sr.sortingOrder = 15;
        AddYSort(sr, true, -0.35f);

        var col  = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.75f, 0.95f);

        var npc = go.AddComponent<NPCInteractable>();
        var so  = new SerializedObject(npc);
        so.FindProperty("speakerName").stringValue = speakerName;
        so.FindProperty("idleFallback").stringValue = idleText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // -------------------------------------------------------------------------
    // Reposition existing game elements so the quest requires exploration
    // -------------------------------------------------------------------------

    private static void RepositionGameElements()
    {
        // Farmer NPC stays in farmyard hub
        MoveGameObject("Gaardejer - Chicken Quest", new Vector3(3f, -1f, 0f));
        MoveGameObject("Pip - Travelling Tinker", new Vector3(13f, -0.6f, 0f));

        // Chickens spread across zones so player must explore
        MoveGameObject("Chicken A", new Vector3(-14f, -5f, 0f));  // barn area
        MoveGameObject("Chicken B", new Vector3( 15f, -4f, 0f));  // vegetable garden
        MoveGameObject("Chicken C", new Vector3(-10f,  9f, 0f));  // orchard

        // Bucket stays near farmhouse
        MoveGameObject("Loose Bucket", new Vector3(-1f, -2f, 0f));

        // Player spawns at farmyard hub, facing north
        const float spawnX = 0f;
        const float spawnY = -4f;
        MoveGameObject("Player", new Vector3(spawnX, spawnY, 0f));
        AddYSortToExisting("Player", true, -0.45f);
        AddYSortToExisting("Gaardejer - Chicken Quest", true, -0.35f);
        AddYSortToExisting("Pip - Travelling Tinker", true, -0.35f);
        AddYSortToExisting("Chicken A", true, -0.25f);
        AddYSortToExisting("Chicken B", true, -0.25f);
        AddYSortToExisting("Chicken C", true, -0.25f);
        AddYSortToExisting("Loose Bucket", false, -0.15f);

        // Spawn point marker
        if (GameObject.Find("Player Spawn Point") == null)
        {
            var sp = new GameObject("Player Spawn Point");
            sp.transform.position = new Vector3(spawnX, spawnY, 0f);
            sp.AddComponent<PlayerSpawnPoint>();
        }
    }

    private static void AddYSortToExisting(string name, bool dynamicSort, float footOffsetY)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            return;
        }

        var renderer = go.GetComponent<SpriteRenderer>() ?? go.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            AddYSort(renderer, dynamicSort, footOffsetY);
        }
    }

    private static void RemoveCombatFromActiveScene()
    {
        RemoveGameObject("Shade");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        var attackFlash = player.transform.Find("Attack Flash");
        if (attackFlash != null)
        {
            Undo.DestroyObjectImmediate(attackFlash.gameObject);
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);
        foreach (Transform child in player.transform)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }

        var health = player.GetComponent<HealthSystem>();
        if (health != null)
        {
            Undo.DestroyObjectImmediate(health);
        }
    }

    private static void CleanupPlayerPrefabCombat()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            return;
        }

        var attackFlash = prefab.transform.Find("Attack Flash");
        if (attackFlash != null)
        {
            Object.DestroyImmediate(attackFlash.gameObject, true);
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
        foreach (Transform child in prefab.transform)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }

        var health = prefab.GetComponent<HealthSystem>();
        if (health != null)
        {
            Object.DestroyImmediate(health, true);
        }

        PrefabUtility.SavePrefabAsset(prefab);
    }

    private static void BuildQuestGuidance()
    {
        var root = GetOrCreate("Quest Guidance");

        AddMarker(root, "Marker_Farmer_Start", new Vector2(3f, 0.45f),
            "collect_chickens", QuestGuidanceMarker.VisibilityRule.WhenQuestNotStarted, null,
            Color.white);

        AddMarker(root, "Marker_Farmer_ChickenReturn", new Vector2(3f, 0.45f),
            "collect_chickens", QuestGuidanceMarker.VisibilityRule.WhenQuestReadyToComplete, null,
            new Color(0.7f, 1f, 0.65f));

        AddMarker(root, "Marker_Farmer_AppleOffer", new Vector2(3f, 0.45f),
            "apple_harvest", QuestGuidanceMarker.VisibilityRule.WhenQuestNotStarted, null,
            Color.white, "collect_chickens", QuestState.Completed);

        AddMarker(root, "Marker_Farmer_AppleReturn", new Vector2(3f, 0.45f),
            "apple_harvest", QuestGuidanceMarker.VisibilityRule.WhenQuestReadyToComplete, null,
            new Color(0.7f, 1f, 0.65f));

        AddChickenMarker("Chicken A");
        AddChickenMarker("Chicken B");
        AddChickenMarker("Chicken C");

        AddTreeMarker(root, "Marker_AppleTree_A", "AppleTree_A", new Vector2(-20f, 6.1f));
        AddTreeMarker(root, "Marker_AppleTree_B", "AppleTree_B", new Vector2(-16f, 10.1f));
        AddTreeMarker(root, "Marker_AppleTree_C", "AppleTree_C", new Vector2(-20f, 13.1f));
    }

    private static void AddChickenMarker(string chickenName)
    {
        var chickenGo = GameObject.Find(chickenName);
        var chicken = chickenGo != null ? chickenGo.GetComponent<ChickenInteractable>() : null;
        if (chickenGo == null || chicken == null)
        {
            return;
        }

        RemoveChildIfExists(chickenGo.transform, "Quest Marker");

        var go = new GameObject("Quest Marker");
        go.transform.SetParent(chickenGo.transform);
        go.transform.localPosition = new Vector3(0f, 0.95f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("ui_quest_marker");
        sr.sortingOrder = 9000;
        sr.color = new Color(1f, 0.82f, 0.45f);

        var marker = go.AddComponent<QuestGuidanceMarker>();
        marker.ConfigureChicken("collect_chickens",
            QuestGuidanceMarker.VisibilityRule.WhenQuestActiveAndIncomplete, chicken);
    }

    private static void RemoveChildIfExists(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static void AddTreeMarker(Transform parent, string markerName, string treeName, Vector2 position)
    {
        var tree = GameObject.Find(treeName)?.GetComponent<ShakeableTree>();
        AddMarker(parent, markerName, position, "apple_harvest",
            QuestGuidanceMarker.VisibilityRule.WhenQuestActiveAndIncomplete, tree,
            new Color(1f, 0.82f, 0.45f));
    }

    private static void AddMarker(Transform parent, string name, Vector2 position, string questId,
        QuestGuidanceMarker.VisibilityRule rule, ShakeableTree linkedTree, Color tint,
        string prerequisiteQuestId = null, QuestState prerequisiteStatus = QuestState.Completed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(position.x, position.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("ui_quest_marker");
        sr.sortingOrder = 9000;
        sr.color = tint;

        var marker = go.AddComponent<QuestGuidanceMarker>();
        marker.Configure(questId, rule, linkedTree, prerequisiteQuestId, prerequisiteStatus);
    }

    private static void MoveGameObject(string name, Vector3 position)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            Undo.RecordObject(go.transform, "FarmWorldBuilder Move " + name);
            go.transform.position = position;
        }
        else
        {
            Debug.LogWarning("[FarmWorldBuilder] Could not find '" + name + "' to reposition.");
        }
    }

    // -------------------------------------------------------------------------
    // Camera — enable bounds, set orthographic size for exploration feel
    // -------------------------------------------------------------------------

    private static void ConfigureCamera()
    {
        var camGo = GameObject.FindWithTag("MainCamera");
        if (camGo == null) return;

        var cam = camGo.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographicSize = 6f;
        }

        var follow = camGo.GetComponent<FollowCamera2D>();
        if (follow != null)
        {
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("useBounds").boolValue = true;
            var boundsCenter = followSo.FindProperty("worldBounds").FindPropertyRelative("m_Center");
            var boundsExtent = followSo.FindProperty("worldBounds").FindPropertyRelative("m_Extent");
            boundsCenter.vector3Value = new Vector3((WorldMinX + WorldMaxX) * 0.5f, (WorldMinY + WorldMaxY) * 0.5f, 0f);
            boundsExtent.vector3Value = new Vector3((WorldMaxX - WorldMinX) * 0.5f, (WorldMaxY - WorldMinY) * 0.5f, 0f);
            followSo.ApplyModifiedPropertiesWithoutUndo();
        }

        if (camGo.GetComponent<DayNightTint>() == null)
            camGo.AddComponent<DayNightTint>();
    }

    // -------------------------------------------------------------------------
    // Sprite generation — pixel art placeholders for every world asset
    // -------------------------------------------------------------------------

    private static void GenerateSprites()
    {
        // Floors
        MakeFloorSprite("floor_grass",  new Color32( 88, 140,  80, 255), new Color32( 62, 108,  56, 255));
        MakeFloorSprite("floor_dirt",   new Color32(172, 138,  90, 255), new Color32(148, 114,  72, 255));
        MakeFloorSprite("floor_soil",   new Color32(110,  78,  54, 255), new Color32( 88,  60,  40, 255));
        MakeFloorSprite("floor_garden", new Color32( 96, 148,  72, 255), new Color32( 72, 118,  52, 255));
        MakeFloorSprite("floor_hay",    new Color32(210, 180,  90, 255), new Color32(188, 156,  72, 255));
        MakeFloorSprite("floor_forest", new Color32( 48,  80,  44, 255), new Color32( 36,  60,  32, 255));
        MakeFloorSprite("floor_road",   new Color32(148, 124,  94, 255), new Color32(128, 104,  76, 255));

        // Props
        MakePropSprite("prop_farmhouse", 32, 40,
            new Color32(210, 175, 130, 255),  // wall
            new Color32(168,  80,  56, 255),  // roof
            new Color32( 60,  40,  28, 255)); // outline

        MakePropSprite("prop_barn", 48, 40,
            new Color32(180,  72,  48, 255),
            new Color32(140,  52,  32, 255),
            new Color32( 44,  24,  16, 255));

        MakeSimplePropSprite("prop_well",     12, 16, new Color32(140, 130, 120, 255), new Color32( 60,  54,  48, 255));
        MakeSimplePropSprite("prop_coop",     20, 16, new Color32(190, 160, 110, 255), new Color32( 80,  60,  40, 255));
        MakeSimplePropSprite("prop_ruin",     24, 12, new Color32(140, 130, 115, 255), new Color32( 60,  54,  44, 255));
        MakeSimplePropSprite("prop_signpost",  8, 14, new Color32(170, 130,  80, 255), new Color32( 60,  40,  20, 255));
        MakeSimplePropSprite("prop_scarecrow", 10, 18, new Color32(200, 160,  90, 255), new Color32( 60,  40,  20, 255));
        MakeSimplePropSprite("prop_haybale",  14,  10, new Color32(220, 180,  80, 255), new Color32( 80,  60,  20, 255));
        MakeSimplePropSprite("prop_cart",     18,  10, new Color32(150, 110,  60, 255), new Color32( 60,  40,  20, 255));
        MakeSimplePropSprite("prop_trough",   16,   8, new Color32(100, 160, 180, 255), new Color32( 40,  80,  90, 255));

        // Trees
        MakeTreeSprite("tree_pine",  12, 20, new Color32( 52, 100,  52, 255), new Color32( 96,  64,  36, 255));
        MakeTreeSprite("tree_apple", 14, 18, new Color32( 64, 128,  56, 255), new Color32( 96,  64,  36, 255));
        MakeTreeSprite("tree_oak",   20, 26, new Color32( 72, 120,  60, 255), new Color32(100,  70,  38, 255));

        // Decorations
        MakeSimplePropSprite("decor_tallgrass",  6, 10, new Color32( 80, 140,  60, 255), new Color32( 48, 100,  36, 255));
        MakeSimplePropSprite("decor_crops",      8, 12, new Color32( 96, 160,  48, 255), new Color32( 60, 110,  28, 255));
        MakeSimplePropSprite("decor_bush",      10,  8, new Color32( 72, 130,  56, 255), new Color32( 44,  90,  36, 255));
        MakeSimplePropSprite("decor_rock",      10,  8, new Color32(150, 145, 138, 255), new Color32( 72,  68,  62, 255));
        MakeSimplePropSprite("decor_stonewall", 16,  8, new Color32(145, 138, 128, 255), new Color32( 72,  66,  58, 255));
        MakeSimplePropSprite("decor_fence_h",   16,  6, new Color32(180, 140,  80, 255), new Color32( 80,  56,  28, 255));
        MakeSimplePropSprite("decor_fence_v",    6, 16, new Color32(180, 140,  80, 255), new Color32( 80,  56,  28, 255));

        AssetDatabase.Refresh();
    }

    private static void MakeFloorSprite(string name, Color32 light, Color32 dark)
    {
        var path = SpriteFolder + name + ".png";
        if (File.Exists(path)) return;

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                // Checkerboard-ish variation for ground texture
                var checker = ((x / 4 + y / 4) % 2) == 0;
                tex.SetPixel(x, y, checker ? light : dark);
            }
        tex.Apply();
        WriteSprite(path, tex, 16f);
    }

    private static void MakeSimplePropSprite(string name, int w, int h, Color32 fill, Color32 outline)
    {
        var path = SpriteFolder + name + ".png";
        if (File.Exists(path)) return;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                tex.SetPixel(x, y, edge ? outline : fill);
            }
        tex.Apply();
        WriteSprite(path, tex, 16f);
    }

    private static void MakePropSprite(string name, int w, int h, Color32 wall, Color32 roof, Color32 outline)
    {
        var path = SpriteFolder + name + ".png";
        if (File.Exists(path)) return;

        var roofH = h / 3;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                Color32 col = edge ? outline : (y >= h - roofH ? roof : wall);
                // Simple window dots
                if (!edge && y > 2 && y < h - roofH - 1)
                {
                    var winX = w / 4;
                    if ((x == winX || x == w - winX) && y % 5 == 3)
                        col = new Color32(180, 220, 240, 255);
                }
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        WriteSprite(path, tex, 16f);
    }

    private static void MakeTreeSprite(string name, int w, int h, Color32 foliage, Color32 trunk)
    {
        var path = SpriteFolder + name + ".png";
        if (File.Exists(path)) return;

        var trunkW  = Mathf.Max(2, w / 4);
        var trunkH  = h / 3;
        var trunkX0 = (w - trunkW) / 2;
        var cx      = w / 2;
        var canopyR = w / 2 - 1;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color32(0, 0, 0, 0));

        // Trunk
        for (var y = 0; y < trunkH; y++)
            for (var x = trunkX0; x < trunkX0 + trunkW; x++)
                tex.SetPixel(x, y, trunk);

        // Canopy — filled circle
        var cy = trunkH + canopyR;
        for (var y = trunkH; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if (dx * dx + dy * dy <= canopyR * canopyR)
                {
                    var shade = (dx + dy) % 3 == 0 ? Darken(foliage, 20) : foliage;
                    tex.SetPixel(x, y, shade);
                }
            }

        tex.Apply();
        WriteSprite(path, tex, 16f);
    }

    private static Color32 Darken(Color32 c, int amount)
    {
        return new Color32(
            (byte)Mathf.Max(0, c.r - amount),
            (byte)Mathf.Max(0, c.g - amount),
            (byte)Mathf.Max(0, c.b - amount),
            c.a);
    }

    private static void WriteSprite(string path, Texture2D tex, float ppu)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + name + ".png");
    }

    private static void AddYSort(SpriteRenderer renderer, bool dynamicSort, float footOffsetY)
    {
        if (renderer == null)
        {
            return;
        }

        var sorter = renderer.GetComponent<AutoYSort2D>();
        if (sorter == null)
        {
            sorter = renderer.gameObject.AddComponent<AutoYSort2D>();
        }

        sorter.Configure(5000, 100, footOffsetY, dynamicSort);
        EditorUtility.SetDirty(sorter);
    }

    private static void AddFootprintCollider(GameObject go, SpriteRenderer renderer, Vector2 colliderSize)
    {
        var col = go.AddComponent<BoxCollider2D>();
        col.size = colliderSize;

        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        var bounds = renderer.sprite.bounds;
        col.offset = new Vector2(0f, bounds.min.y + colliderSize.y * 0.5f);
    }

    private static Transform GetOrCreate(string hierarchyPath)
    {
        var parts  = hierarchyPath.Split('/');
        Transform parent = null;
        foreach (var part in parts)
        {
            GameObject found = parent == null
                ? GameObject.Find(part)
                : parent.Find(part)?.gameObject;

            if (found == null)
            {
                found = new GameObject(part);
                if (parent != null) found.transform.SetParent(parent);
            }
            parent = found.transform;
        }
        return parent;
    }

    private static void RemoveGameObject(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            Undo.DestroyObjectImmediate(go);
        }
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(SpriteFolder);
        AssetDatabase.Refresh();
    }

    private static void ConfigureWorldSpriteImporters()
    {
        if (!Directory.Exists(SpriteFolder))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(SpriteFolder, "*.png", SearchOption.TopDirectoryOnly))
        {
            var path = file.Replace('\\', '/');
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = GetPixelsPerUnit(Path.GetFileNameWithoutExtension(path));
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static float GetPixelsPerUnit(string spriteName)
    {
        if (spriteName.StartsWith("floor_"))
        {
            return 128f;
        }

        if (spriteName.StartsWith("item_") || spriteName.StartsWith("soapbox_"))
        {
            return 16f;
        }

        if (spriteName.StartsWith("tree_"))
        {
            return 160f;
        }

        if (spriteName.StartsWith("prop_farmhouse") || spriteName.StartsWith("prop_barn") ||
            spriteName.StartsWith("prop_coop") || spriteName.StartsWith("prop_shed"))
        {
            return 160f;
        }

        return 180f;
    }
}
