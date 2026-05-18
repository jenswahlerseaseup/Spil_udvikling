using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GameBuildTools
{
    private const string ScenePath = "Assets/_Project/Scenes/Gameplay.unity";
    private const string BuildFolder = "Builds/Windows";
    private const string ExecutablePath = BuildFolder + "/NytSpil.exe";

    [MenuItem("Tools/Build/Build Windows Demo")]
    public static void BuildWindowsDemo()
    {
        if (!File.Exists(ScenePath))
        {
            InitialGameplaySceneBuilder.CreateInitialGameplayScene();
        }

        Directory.CreateDirectory(BuildFolder);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ExecutablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Windows demo build created: " + Path.GetFullPath(ExecutablePath));
            EditorUtility.RevealInFinder(Path.GetFullPath(ExecutablePath));
        }
        else
        {
            Debug.LogError("Build failed: " + report.summary.result);
        }
    }
}
