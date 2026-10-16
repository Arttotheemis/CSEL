using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class MonsterBuildChecks
{
    public static void BuildMac()
    {
        Directory.CreateDirectory("Builds/macOS");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Monster.unity" },
            locationPathName = "Builds/macOS/PocketPet.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        var summary = report.summary;
        File.WriteAllText("Docs/BUILD-CHECK.txt", $"macOS player build: {summary.result}\nErrors: {summary.totalErrors}\nWarnings: {summary.totalWarnings}\nBytes: {summary.totalSize}\n");
        if (summary.result != BuildResult.Succeeded) throw new Exception("Player build failed: " + summary.result);
        UnityEngine.Debug.Log("MONSTER_PLAYER_BUILD_PASSED: " + summary.totalErrors + " errors, " + summary.totalWarnings + " warnings");
    }
}
