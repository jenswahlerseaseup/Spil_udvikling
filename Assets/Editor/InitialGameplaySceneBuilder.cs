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
    private const string EmilSpriteSetPath = "Assets/_Project/ScriptableObjects/Player/EmilSpriteSet.asset";
    private const string EmilSpriteFolder = "Assets/emil_sprites_v2/";
    private const string ItemRegistryPath = "Assets/_Project/ScriptableObjects/Items/ItemRegistry.asset";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
    private const string PlayerSpritePath = "Assets/_Project/Art/Sprites/placeholder_player.png";
    private const string PlayfieldBackgroundPath = "Assets/_Project/Art/Backgrounds/mountain_village_playfield.png";
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
    private const string ChickenQuestPath = "Assets/_Project/ScriptableObjects/Quests/CollectChickens.asset";
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

        CreatePlayfieldBackground();
        CreateCoreSystems();
        var player = CreatePlayer();
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);
        CreateCamera(player.transform);
        CreateCollisionDemo();
        CreateNpc();
        CreateShopkeeper();
        CreateEnemy();
        CreateFarmInteractions();
        CreateShrine();
        CreateGameplayHud();

        EditorSceneManager.SaveScene(scene, GameplayScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameplayScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateCoreSystems()
    {
        var systems = new GameObject("Core Systems");

        systems.AddComponent<GameManager>();
        systems.AddComponent<DialogueManager>();
        systems.AddComponent<MischiefSystem>();

        var questManager = systems.AddComponent<QuestManager>();
        var questManagerObject = new SerializedObject(questManager);
        var quests = questManagerObject.FindProperty("allQuests");
        quests.arraySize = 1;
        quests.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<QuestDefinition>(ChickenQuestPath);
        questManagerObject.ApplyModifiedPropertiesWithoutUndo();

        var saveManager = systems.AddComponent<SaveManager>();
        var saveManagerObject = new SerializedObject(saveManager);
        saveManagerObject.FindProperty("itemRegistry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
        saveManagerObject.ApplyModifiedPropertiesWithoutUndo();
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
        player.AddComponent<PlayerInteractor>();
        player.AddComponent<PlayerJump>();
        var attack = player.AddComponent<PlayerAttackController>();

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

        var spriteAnimator = player.AddComponent<DirectionalSpriteAnimator>();
        var spriteAnimatorObject = new SerializedObject(spriteAnimator);
        spriteAnimatorObject.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        spriteAnimatorObject.FindProperty("spriteSet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(EmilSpriteSetPath);
        spriteAnimatorObject.ApplyModifiedPropertiesWithoutUndo();

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

    private static void CreatePlayfieldBackground()
    {
        var floor = new GameObject("Mountain Village Playfield");
        floor.transform.position = Vector3.zero;

        var renderer = floor.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayfieldBackgroundPath);
        renderer.sortingOrder = -30;

        if (renderer.sprite == null)
        {
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FloorSpritePath);
            floor.transform.localScale = new Vector3(11f, 11f, 1f);
        }
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
        var npc = new GameObject("Gaardejer - Chicken Quest");
        npc.layer = LayerMask.NameToLayer("Interactable");
        npc.transform.position = new Vector3(1.7f, 1.2f, 0f);

        var renderer = npc.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NpcSpritePath);
        renderer.sortingOrder = 15;

        var collider = npc.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.75f, 0.95f);

        var questNpc = npc.AddComponent<NPCInteractable>();
        var questObject = new SerializedObject(questNpc);
        questObject.FindProperty("speakerName").stringValue = "Gaardejer";
        questObject.FindProperty("questToGive").objectReferenceValue = AssetDatabase.LoadAssetAtPath<QuestDefinition>(ChickenQuestPath);
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

    private static void CreateFarmInteractions()
    {
        CreateChicken("Chicken A", new Vector3(-2.4f, 0.95f, 0f));
        CreateChicken("Chicken B", new Vector3(-1.1f, -2.1f, 0f));
        CreateChicken("Chicken C", new Vector3(2.25f, 0.25f, 0f));

        CreateBucket("Loose Bucket", new Vector3(-3.25f, -0.35f, 0f));
    }

    private static void CreateChicken(string name, Vector3 position)
    {
        var chicken = new GameObject(name);
        chicken.layer = LayerMask.NameToLayer("Interactable");
        chicken.transform.position = position;

        var renderer = chicken.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LootSpritePath);
        renderer.color = new Color(1f, 0.92f, 0.68f);
        renderer.sortingOrder = 16;

        var collider = chicken.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        chicken.AddComponent<ChickenInteractable>();
    }

    private static void CreateBucket(string name, Vector3 position)
    {
        var bucket = new GameObject(name);
        bucket.layer = LayerMask.NameToLayer("Interactable");
        bucket.transform.position = position;

        var renderer = bucket.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritePath);
        renderer.color = new Color(0.72f, 0.66f, 0.82f);
        renderer.sortingOrder = 14;

        var collider = bucket.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.6f, 0.6f);

        bucket.AddComponent<BucketInteractable>();
    }

    private static void CreateShrine()
    {
        var shrine = new GameObject("Old North Shrine");
        shrine.layer = LayerMask.NameToLayer("Solid");
        shrine.transform.position = new Vector3(0f, 2.65f, 0f);

        var collider = shrine.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.45f, 1.2f);
    }

    private static void CreateWall(Transform parent, string name, int layer, Vector2 position, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.layer = layer;
        wall.transform.SetParent(parent);
        wall.transform.position = position;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        // The high-detail playfield already contains edge art; walls stay invisible and only provide collision.
    }

    private static void CreateGameplayHud()
    {
        var hud = new GameObject("Gameplay HUD");
        hud.AddComponent<PrototypeHud>();
    }

    private static void CreateSaveController(Transform player)
    {
        var save = new GameObject("Save Game Controller");
        var saveManager = save.AddComponent<SaveManager>();
        var saveObject = new SerializedObject(saveManager);
        saveObject.FindProperty("itemRegistry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
        saveObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/_Project/Animations",
            "Assets/_Project/Art",
            "Assets/_Project/Art/Backgrounds",
            "Assets/_Project/Art/Sprites",
            "Assets/_Project/Audio",
            "Assets/_Project/Input",
            "Assets/_Project/Materials",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/ScriptableObjects",
            "Assets/_Project/ScriptableObjects/Items",
            "Assets/_Project/ScriptableObjects/Player",
            "Assets/_Project/ScriptableObjects/Quests",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Combat",
            "Assets/_Project/Scripts/Core",
            "Assets/_Project/Scripts/Dialogue",
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
        ConfigureSpriteImporter(PlayfieldBackgroundPath, 128f);
        ConfigureEmilSpriteImporters();
    }

    private static void EnsureGameplayData()
    {
        var echoShard = EnsureItem(EchoShardPath, "echo_shard", "Echo Shard", 2, LootSpritePath);
        var hearthTea = EnsureItem(HearthTeaPath, "hearth_tea", "Hearth Tea", 3, ShopSpritePath);
        var emilSpriteSet = EnsureEmilSpriteSet();
        var itemRegistry = EnsureItemRegistry(echoShard, hearthTea);
        var chickenQuest = EnsureChickenQuest(hearthTea);

        if (!File.Exists(LanternQuestPath))
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            AssetDatabase.CreateAsset(quest, LanternQuestPath);
        }

        var questAsset = AssetDatabase.LoadAssetAtPath<QuestDefinition>(LanternQuestPath);
        var questObject = new SerializedObject(questAsset);
        questObject.FindProperty("questId").stringValue = "lantern_errand";
        questObject.FindProperty("title").stringValue = "Lantern Errand";
        questObject.FindProperty("description").stringValue = "A shade is circling the old shrine. Bring me one Echo Shard from it, and I can keep the village lantern lit.";
        SetQuestSteps(questObject, ("Bring back one Echo Shard.", 1));
        SetRewardItems(questObject);
        questObject.FindProperty("rewardCoins").intValue = 6;
        questObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(echoShard);
        EditorUtility.SetDirty(hearthTea);
        EditorUtility.SetDirty(questAsset);
        EditorUtility.SetDirty(itemRegistry);
        EditorUtility.SetDirty(chickenQuest);
        if (emilSpriteSet != null)
        {
            EditorUtility.SetDirty(emilSpriteSet);
        }
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

    private static ItemRegistry EnsureItemRegistry(params ItemDefinition[] items)
    {
        if (!File.Exists(ItemRegistryPath))
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ItemRegistry>(), ItemRegistryPath);
        }

        var registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
        var registryObject = new SerializedObject(registry);
        var itemArray = registryObject.FindProperty("items");
        itemArray.arraySize = items.Length;
        for (var i = 0; i < items.Length; i++)
        {
            itemArray.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        registryObject.ApplyModifiedPropertiesWithoutUndo();
        return registry;
    }

    private static QuestDefinition EnsureChickenQuest(ItemDefinition rewardItem)
    {
        if (!File.Exists(ChickenQuestPath))
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<QuestDefinition>(), ChickenQuestPath);
        }

        var quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(ChickenQuestPath);
        var questObject = new SerializedObject(quest);
        questObject.FindProperty("questId").stringValue = "collect_chickens";
        questObject.FindProperty("title").stringValue = "Chicken Roundup";
        questObject.FindProperty("description").stringValue = "Three chickens have escaped into the village yard. Catch them before they cause chaos.";
        SetQuestSteps(questObject, ("Catch three escaped chickens.", 3));
        SetRewardItems(questObject, rewardItem);
        questObject.FindProperty("rewardCoins").intValue = 5;
        questObject.ApplyModifiedPropertiesWithoutUndo();
        return quest;
    }

    private static void SetQuestSteps(SerializedObject questObject, params (string description, int requiredCount)[] steps)
    {
        var stepArray = questObject.FindProperty("steps");
        stepArray.arraySize = steps.Length;
        for (var i = 0; i < steps.Length; i++)
        {
            var step = stepArray.GetArrayElementAtIndex(i);
            step.FindPropertyRelative("description").stringValue = steps[i].description;
            step.FindPropertyRelative("requiredCount").intValue = steps[i].requiredCount;
        }
    }

    private static void SetRewardItems(SerializedObject questObject, params ItemDefinition[] items)
    {
        var rewards = questObject.FindProperty("rewardItems");
        rewards.arraySize = items.Length;
        for (var i = 0; i < items.Length; i++)
        {
            rewards.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }

    private static CharacterSpriteSet EnsureEmilSpriteSet()
    {
        if (!File.Exists(EmilSpriteFolder + "front_idle.png"))
        {
            return null;
        }

        if (!File.Exists(EmilSpriteSetPath))
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CharacterSpriteSet>(), EmilSpriteSetPath);
        }

        var spriteSet = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(EmilSpriteSetPath);
        var spriteSetObject = new SerializedObject(spriteSet);

        SetSprite(spriteSetObject, "frontIdle", "front_idle.png");
        SetSpriteArray(spriteSetObject, "frontWalk", "front_walk1.png", "front_walk2.png");
        SetSprite(spriteSetObject, "frontJump", "front_jump.png");

        SetSprite(spriteSetObject, "backIdle", "back_idle.png");
        SetSpriteArray(spriteSetObject, "backWalk", "back_walk1.png", "back_walk2.png");
        SetSprite(spriteSetObject, "backJump", "back_jump.png");

        SetSprite(spriteSetObject, "sideRightIdle", "side_right_idle.png");
        SetSpriteArray(spriteSetObject, "sideRightWalk", "side_right_walk1.png", "side_right_walk2.png");
        SetSprite(spriteSetObject, "sideRightJump", "side_right_jump.png");

        SetSprite(spriteSetObject, "sideLeftIdle", "side_left_idle.png");
        SetSpriteArray(spriteSetObject, "sideLeftWalk", "side_left_walk1.png", "side_left_walk2.png");
        SetSprite(spriteSetObject, "sideLeftJump", "side_left_jump.png");

        spriteSetObject.ApplyModifiedPropertiesWithoutUndo();
        return spriteSet;
    }

    private static void SetSprite(SerializedObject target, string propertyName, string fileName)
    {
        target.FindProperty(propertyName).objectReferenceValue = LoadEmilSprite(fileName);
    }

    private static void SetSpriteArray(SerializedObject target, string propertyName, params string[] fileNames)
    {
        var array = target.FindProperty(propertyName);
        array.arraySize = fileNames.Length;
        for (var i = 0; i < fileNames.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = LoadEmilSprite(fileNames[i]);
        }
    }

    private static Sprite LoadEmilSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(EmilSpriteFolder + fileName);
    }

    private static void ConfigureEmilSpriteImporters()
    {
        if (!Directory.Exists(EmilSpriteFolder))
        {
            return;
        }

        var spritePaths = Directory.GetFiles(EmilSpriteFolder, "*.png", SearchOption.TopDirectoryOnly);
        foreach (var spritePath in spritePaths)
        {
            ConfigureSpriteImporter(spritePath.Replace('\\', '/'), 32f);
        }
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

    private static void ConfigureSpriteImporter(string path, float pixelsPerUnit)
    {
        if (!File.Exists(path))
        {
            return;
        }

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
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
