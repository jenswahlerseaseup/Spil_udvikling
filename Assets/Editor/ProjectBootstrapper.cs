using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectBootstrapper
{
    private const string DoneKey = "NytSpil.ProjectBootstrapper.Done";

    static ProjectBootstrapper()
    {
        EditorApplication.delayCall += ConfigureOnce;
    }

    [MenuItem("Tools/Project Setup/Run 2D Project Setup")]
    public static void ConfigureOnce()
    {
        CreateFolders();
        ConfigureUnityDefaults();
        AddSceneToBuildSettings();

        EditorPrefs.SetBool(DoneKey, true);
        Debug.Log("2D project setup checked. Review Project Settings > Graphics and Quality after URP finishes importing.");
    }

    private static void CreateFolders()
    {
        string[] folders =
        {
            "Assets/_Project/Art/Sprites",
            "Assets/_Project/Art/Tiles",
            "Assets/_Project/Audio/Music",
            "Assets/_Project/Audio/SFX",
            "Assets/_Project/Materials",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/Scripts",
            "Assets/_Project/Settings"
        };

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(folder);
        }

        AssetDatabase.Refresh();
    }

    private static void ConfigureUnityDefaults()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        PlayerSettings.runInBackground = true;
        PlayerSettings.visibleInBackground = true;

        TrySetActiveInputHandling();
    }

    private static void TrySetActiveInputHandling()
    {
        var property = typeof(PlayerSettings).GetProperty(
            "activeInputHandling",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (property == null || !property.CanWrite)
        {
            Debug.Log("Set Active Input Handling manually to Input System Package in Project Settings > Player > Other Settings.");
            return;
        }

        try
        {
            var value = Enum.Parse(property.PropertyType, "InputSystemPackage");
            property.SetValue(null, value);
        }
        catch (Exception ex)
        {
            Debug.Log("Could not switch Active Input Handling automatically: " + ex.Message);
        }
    }

    private static void AddSceneToBuildSettings()
    {
        const string scenePath = "Assets/_Project/Scenes/TestScene.unity";
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        EditorSceneManager.OpenScene(scenePath);
    }
}
