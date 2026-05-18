using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class InitialGameplaySceneBuilder
{
    private const string GameplayScenePath = "Assets/_Project/Scenes/Gameplay.unity";
    private const string InputAssetPath = "Assets/_Project/Input/GameInput.inputactions";
    private const string MovementSettingsPath = "Assets/_Project/ScriptableObjects/Player/DefaultPlayerMovement.asset";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
    private const string PlayerSpritePath = "Assets/_Project/Art/Sprites/placeholder_player.png";
    private const string FloorSpritePath = "Assets/_Project/Art/Sprites/placeholder_floor.png";
    private const string WallSpritePath = "Assets/_Project/Art/Sprites/placeholder_wall.png";
    private const string NpcSpritePath = "Assets/_Project/Art/Sprites/placeholder_npc.png";
    private const string ShrineSpritePath = "Assets/_Project/Art/Sprites/placeholder_shrine.png";
    private const string EnemySpritePath = "Assets/_Project/Art/Sprites/placeholder_enemy.png";
    private const string LootSpritePath = "Assets/_Project/Art/Sprites/placeholder_echo_shard.png";
    private const string ShopSpritePath = "Assets/_Project/Art/Sprites/placeholder_shopkeeper.png";
    private const string AttackSpritePath = "Assets/_Project/Art/Sprites/placeholder_attack_flash.png";
    private const string EchoShardPath = "Assets/_Project/ScriptableObjects/Items/EchoShard.asset";
    private const string HearthTeaPath = "Assets/_Project/ScriptableObjects/Items/HearthTea.asset";
    private const string LanternQuestPath = "Assets/_Project/ScriptableObjects/Quests/LanternErrand.asset";
    private const string AutoSetupSessionKey = "NytSpil.InitialGameplaySceneBuilder.AutoSetupQueued";

    [InitializeOnLoadMethod]
    private static void QueueAutoSetup()
    {
        if (SessionState.GetBool(AutoSetupSessionKey, false) || File.Exists(GameplayScenePath))
        {
            return;
        }

        SessionState.SetBool(AutoSetupSessionKey, true);
        EditorApplication.delayCall += TryAutoSetup;
    }

    private static void TryAutoSetup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoSetup;
            return;
        }

        if (!File.Exists(GameplayScenePath))
        {
            CreateInitialGameplayScene();
            Debug.Log("Created initial playable scene at " + GameplayScenePath + ".");
        }
    }

    [MenuItem("Tools/Project Setup/Create Initial Gameplay Scene")]
    public static void CreateInitialGameplayScene()
    {
        EnsureFolders();
        EnsureLayer("Player", 8);
        EnsureLayer("Solid", 9);
        EnsureLayer("Interactable", 10);
        EnsureLayer("Enemy", 11);
        EnsurePlaceholderSprites();
        EnsureGameplayData();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Gameplay";

        CreateFloor();
        var player = CreatePlayer();
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);
        CreateCamera(player.transform);
        CreateCollisionDemo();
        CreateNpc();
        CreateShopkeeper();
        CreateEnemy();
        CreateShrine();
        CreateSaveController(player.transform);
        CreateGameplayHud();

        EditorSceneManager.SaveScene(scene, GameplayScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameplayScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreatePlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");
        player.transform.position = Vector3.zero;

        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.7f, 0.9f);
        collider.offset = new Vector2(0f, -0.1f);

        var interactionRange = player.AddComponent<CircleCollider2D>();
        interactionRange.isTrigger = true;
        interactionRange.radius = 1.15f;
        interactionRange.offset = new Vector2(0f, -0.05f);

#if ENABLE_INPUT_SYSTEM
        var input = player.AddComponent<PlayerInput>();
        input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        input.defaultActionMap = "Player";
        input.notificationBehavior = PlayerNotifications.SendMessages;
#endif

        var inputReader = player.AddComponent<PlayerInputReader>();
        var motor = player.AddComponent<TopDownPlayerMotor>();
        player.AddComponent<HealthSystem>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerQuestLog>();
        player.AddComponent<PlayerInteractor>();
        var attack = player.AddComponent<PlayerAttackController>();
        player.AddComponent<PlayerAnimationController>();
        player.AddComponent<TopDownSpriteAnimator>();

        var movementSettings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(MovementSettingsPath);
        var motorObject = new SerializedObject(motor);
        motorObject.FindProperty("movementSettings").objectReferenceValue = movementSettings;
        motorObject.FindProperty("inputReader").objectReferenceValue = inputReader;
        motorObject.ApplyModifiedPropertiesWithoutUndo();

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(player.transform);
        visuals.transform.localPosition = Vector3.zero;
        var renderer = visuals.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        renderer.sortingOrder = 20;
        visuals.AddComponent<Animator>();

        var attackFlash = new GameObject("Attack Flash");
        attackFlash.transform.SetParent(player.transform);
        attackFlash.transform.localPosition = Vector3.zero;
        var attackRenderer = attackFlash.AddComponent<SpriteRenderer>();
        attackRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AttackSpritePath);
        attackRenderer.sortingOrder = 30;
        attackRenderer.enabled = false;

        var attackObject = new SerializedObject(attack);
        attackObject.FindProperty("targetMask").intValue = 1 << LayerMask.NameToLayer("Enemy");
        attackObject.FindProperty("attackFlash").objectReferenceValue = attackRenderer;
        attackObject.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static void CreateFloor()
    {
        var floor = new GameObject("Placeholder Floor");
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(11f, 11f, 1f);

        var renderer = floor.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FloorSpritePath);
        renderer.sortingOrder = -10;
    }

    private static void CreateCamera(Transform target)
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.086f, 0.095f, 0.105f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();

        var follow = cameraObject.AddComponent<FollowCamera2D>();
        follow.SetTarget(target);
    }

    private static void CreateCollisionDemo()
    {
        var root = new GameObject("Collision Demo Bounds");
        var solidLayer = LayerMask.NameToLayer("Solid");

        CreateWall(root.transform, "North Wall", solidLayer, new Vector2(0f, 4.75f), new Vector2(10f, 0.5f));
        CreateWall(root.transform, "South Wall", solidLayer, new Vector2(0f, -4.75f), new Vector2(10f, 0.5f));
        CreateWall(root.transform, "West Wall", solidLayer, new Vector2(-4.75f, 0f), new Vector2(0.5f, 10f));
        CreateWall(root.transform, "East Wall", solidLayer, new Vector2(4.75f, 0f), new Vector2(0.5f, 10f));
    }

    private static void CreateNpc()
    {
        var npc = new GameObject("Mira - Lantern Keeper");
        npc.layer = LayerMask.NameToLayer("Interactable");
        npc.transform.position = new Vector3(1.7f, 1.2f, 0f);

        var renderer = npc.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NpcSpritePath);
        renderer.sortingOrder = 15;

        var collider = npc.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.75f, 0.95f);

        var questGiver = npc.AddComponent<QuestGiverInteractable>();
        var questObject = new SerializedObject(questGiver);
        questObject.FindProperty("quest").objectReferenceValue = AssetDatabase.LoadAssetAtPath<QuestDefinition>(LanternQuestPath);
        questObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateShopkeeper()
    {
        var shopkeeper = new GameObject("Pip - Travelling Tinker");
        shopkeeper.layer = LayerMask.NameToLayer("Interactable");
        shopkeeper.transform.position = new Vector3(2.8f, -1.6f, 0f);

        var renderer = shopkeeper.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritePath);
        renderer.sortingOrder = 15;

        var collider = shopkeeper.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.75f, 0.95f);

        var shop = shopkeeper.AddComponent<ShopInteractable>();
        var shopObject = new SerializedObject(shop);
        shopObject.FindProperty("itemForSale").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(HearthTeaPath);
        shopObject.FindProperty("price").intValue = 3;
        shopObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateEnemy()
    {
        var enemy = new GameObject("Shade");
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.transform.position = new Vector3(-2.8f, -1.8f, 0f);

        var renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath);
        renderer.sortingOrder = 18;

        var body = enemy.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var collider = enemy.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.75f, 0.9f);

        var health = enemy.AddComponent<HealthSystem>();
        var healthObject = new SerializedObject(health);
        healthObject.FindProperty("maxHealth").intValue = 3;
        healthObject.ApplyModifiedPropertiesWithoutUndo();

        enemy.AddComponent<EnemyBrain>();
        enemy.AddComponent<ContactDamage>();

        var dropper = enemy.AddComponent<LootDropper>();
        var dropperObject = new SerializedObject(dropper);
        dropperObject.FindProperty("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(EchoShardPath);
        dropperObject.FindProperty("lootSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(LootSpritePath);
        dropperObject.FindProperty("coins").intValue = 2;
        dropperObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateShrine()
    {
        var shrine = new GameObject("Old North Shrine");
        shrine.layer = LayerMask.NameToLayer("Solid");
        shrine.transform.position = new Vector3(-1.8f, 1.9f, 0f);

        var renderer = shrine.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShrineSpritePath);
        renderer.sortingOrder = 8;

        var collider = shrine.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.1f, 1.1f);
    }

    private static void CreateWall(Transform parent, string name, int layer, Vector2 position, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.layer = layer;
        wall.transform.SetParent(parent);
        wall.transform.position = position;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        var renderer = wall.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WallSpritePath);
        renderer.sortingOrder = 5;
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private static void CreateGameplayHud()
    {
        var hud = new GameObject("Gameplay HUD");
        hud.AddComponent<PrototypeHud>();
    }

    private static void CreateSaveController(Transform player)
    {
        var save = new GameObject("Save Game Controller");
        var controller = save.AddComponent<SaveGameController>();
        var saveObject = new SerializedObject(controller);
        saveObject.FindProperty("player").objectReferenceValue = player;
        saveObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/_Project/Animations",
            "Assets/_Project/Art",
            "Assets/_Project/Art/Sprites",
            "Assets/_Project/Audio",
            "Assets/_Project/Input",
            "Assets/_Project/Materials",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/ScriptableObjects",
            "Assets/_Project/ScriptableObjects/Items",
            "Assets/_Project/ScriptableObjects/Quests",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Combat",
            "Assets/_Project/Scripts/Enemies",
            "Assets/_Project/Scripts/Inventory",
            "Assets/_Project/Scripts/Interaction",
            "Assets/_Project/Scripts/Quests",
            "Assets/_Project/Scripts/Save",
            "Assets/_Project/Scripts/Shops",
            "Assets/_Project/Scripts/UI",
            "Assets/_Project/UI"
        };

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(folder);
        }
    }

    private static void EnsurePlaceholderSprites()
    {
        CreateSpriteAsset(PlayerSpritePath, 16, 24, new Color32(92, 224, 166, 255), new Color32(24, 43, 48, 255));
        CreateSpriteAsset(FloorSpritePath, 16, 16, new Color32(38, 54, 52, 255), new Color32(45, 66, 61, 255));
        CreateSpriteAsset(WallSpritePath, 16, 16, new Color32(101, 78, 93, 255), new Color32(42, 34, 48, 255));
        CreateSpriteAsset(NpcSpritePath, 16, 24, new Color32(228, 187, 102, 255), new Color32(49, 36, 42, 255));
        CreateSpriteAsset(ShrineSpritePath, 20, 20, new Color32(130, 116, 184, 255), new Color32(34, 31, 48, 255));
        CreateSpriteAsset(EnemySpritePath, 16, 20, new Color32(88, 51, 122, 255), new Color32(22, 19, 29, 255));
        CreateSpriteAsset(LootSpritePath, 12, 12, new Color32(112, 224, 242, 255), new Color32(28, 50, 68, 255));
        CreateSpriteAsset(ShopSpritePath, 16, 24, new Color32(236, 136, 90, 255), new Color32(54, 38, 42, 255));
        CreateSpriteAsset(AttackSpritePath, 14, 6, new Color32(255, 245, 146, 210), new Color32(255, 185, 81, 230));
    }

    private static void EnsureGameplayData()
    {
        var echoShard = EnsureItem(EchoShardPath, "echo_shard", "Echo Shard", 2, LootSpritePath);
        var hearthTea = EnsureItem(HearthTeaPath, "hearth_tea", "Hearth Tea", 3, ShopSpritePath);

        if (!File.Exists(LanternQuestPath))
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            AssetDatabase.CreateAsset(quest, LanternQuestPath);
        }

        var questAsset = AssetDatabase.LoadAssetAtPath<QuestDefinition>(LanternQuestPath);
        var questObject = new SerializedObject(questAsset);
        questObject.FindProperty("id").stringValue = "lantern_errand";
        questObject.FindProperty("title").stringValue = "Lantern Errand";
        questObject.FindProperty("description").stringValue = "A shade is circling the old shrine. Bring me one Echo Shard from it, and I can keep the village lantern lit.";
        questObject.FindProperty("requiredItem").objectReferenceValue = echoShard;
        questObject.FindProperty("requiredQuantity").intValue = 1;
        questObject.FindProperty("coinReward").intValue = 6;
        questObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(echoShard);
        EditorUtility.SetDirty(hearthTea);
        EditorUtility.SetDirty(questAsset);
    }

    private static ItemDefinition EnsureItem(string path, string id, string displayName, int value, string spritePath)
    {
        if (!File.Exists(path))
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ItemDefinition>(), path);
        }

        var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
        var itemObject = new SerializedObject(item);
        itemObject.FindProperty("id").stringValue = id;
        itemObject.FindProperty("displayName").stringValue = displayName;
        itemObject.FindProperty("value").intValue = value;
        itemObject.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        itemObject.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    private static void CreateSpriteAsset(string path, int width, int height, Color32 fill, Color32 outline)
    {
        if (File.Exists(path))
        {
            return;
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var isEdge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                texture.SetPixel(x, y, isEdge ? outline : fill);
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void EnsureLayer(string layerName, int preferredSlot)
    {
        if (LayerMask.NameToLayer(layerName) != -1)
        {
            return;
        }

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");

        var slot = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferredSlot).stringValue)
            ? preferredSlot
            : FindEmptyLayerSlot(layers);

        if (slot == -1)
        {
            Debug.LogWarning("No empty Unity layer slots are available for " + layerName + ".");
            return;
        }

        layers.GetArrayElementAtIndex(slot).stringValue = layerName;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int FindEmptyLayerSlot(SerializedProperty layers)
    {
        for (var i = 8; i < layers.arraySize; i++)
        {
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                return i;
            }
        }

        return -1;
    }
}
