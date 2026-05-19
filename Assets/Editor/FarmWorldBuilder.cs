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

    // Quest asset paths
    private const string AppleQuestPath    = "Assets/_Project/ScriptableObjects/Quests/AppleHarvest.asset";
    private const string ChickenQuestPath  = "Assets/_Project/ScriptableObjects/Quests/CollectChickens.asset";
    private const string HearthTeaPath     = "Assets/_Project/ScriptableObjects/Items/HearthTea.asset";
    private const string EchoShardPath     = "Assets/_Project/ScriptableObjects/Items/EchoShard.asset";
    private const string ItemRegistryPath  = "Assets/_Project/ScriptableObjects/Items/ItemRegistry.asset";

    // ── Generated root names — used by both builder and cleanup ───────────────
    private static readonly string[] GeneratedRoots =
        { "Environment", "Interactables", "NPCs", "Farm Visual Dressing" };

    [MenuItem("Tools/World/Build Farm World")]
    public static void BuildFarmWorld()
    {
        EnsureFolders();
        GenerateSprites();
        ConfigureWorldSpriteImporters();
        EnsureAppleHarvestQuest();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Remove legacy objects (first-time run) and regenerated roots (re-run safety)
        RemoveGameObject("Collision Demo Bounds");
        RemoveGameObject("Mountain Village Playfield");
        RemoveGameObject("Gameplay HUD");
        RemoveGameObject("Old North Shrine");
        ClearGeneratedRoots();

        BuildFloors();
        BuildBoundaryWalls();
        BuildLandmarks();
        BuildVegetation();
        BuildFences();
        BuildPaths();
        PlaceInteractables();
        PlaceMischiefProps();
        PlaceExtraNPCs();
        RepositionGameElements();
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
        EnsureAppleHarvestQuest();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

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

        var col     = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.7f;

        go.AddComponent<ShakeableTree>();
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

        // Keep sprite at natural 1-unit-per-tile scale; only collider covers the base.
        var col = go.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.offset = new Vector2(0f, -colliderSize.y * 0.1f);
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
        MoveGameObject("Shade", new Vector3(6.5f, 13.2f, 0f));

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

        // Spawn point marker
        if (GameObject.Find("Player Spawn Point") == null)
        {
            var sp = new GameObject("Player Spawn Point");
            sp.transform.position = new Vector3(spawnX, spawnY, 0f);
            sp.AddComponent<PlayerSpawnPoint>();
        }
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
