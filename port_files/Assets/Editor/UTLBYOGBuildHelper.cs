#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Oculta temporariamente utl_bundles (e AssetBundles) durante a compilacao do APK BYOG leve
/// e restaura tudo automaticamente ao concluir.
/// </summary>
public static class UTLBYOGBuildHelper
{
    [MenuItem("UTL Port/Compilar APK Launcher BYOG (Leve, sem AssetBundles)")]
    public static void BuildBYOGLauncherAPK()
    {
        string savePath = EditorUtility.SaveFilePanel(
            "Salvar APK Launcher BYOG",
            "",
            "UTL_Android_Launcher_BYOG.apk",
            "apk"
        );
        if (string.IsNullOrEmpty(savePath)) return;

        string projectRoot = Directory.GetCurrentDirectory();
        string[] foldersToHide = { "utl_bundles", "AssetBundles" };
        List<string[]> movedPairs = new List<string[]>();

        try
        {
            foreach (string folder in foldersToHide)
            {
                string src = Path.Combine(projectRoot, "Assets", "StreamingAssets", folder);
                string bak = Path.Combine(projectRoot, "_UTL_Temp_Backup_" + folder);
                if (Directory.Exists(src))
                {
                    if (Directory.Exists(bak)) Directory.Delete(bak, true);
                    Directory.Move(src, bak);
                    if (File.Exists(src + ".meta")) File.Move(src + ".meta", bak + ".meta");
                    movedPairs.Add(new[] { src, bak });
                    Debug.Log("[UTL BYOG] Ocultado temporariamente: " + folder);
                }
            }
            if (movedPairs.Count > 0) AssetDatabase.Refresh();

            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path))
                    scenes.Add(s.path);
            }

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = savePath,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[UTL BYOG] SUCESSO! APK Launcher BYOG gerado ({report.summary.totalSize / (1024 * 1024)} MB): {savePath}");
                EditorUtility.DisplayDialog("UTL BYOG", "APK Launcher BYOG compilado com sucesso!\n\n" + savePath, "OK");
            }
            else
            {
                Debug.LogError("[UTL BYOG] Falha na compilacao: " + report.summary.result);
            }
        }
        finally
        {
            foreach (string[] pair in movedPairs)
            {
                string src = pair[0], bak = pair[1];
                if (Directory.Exists(bak) && !Directory.Exists(src))
                {
                    Directory.Move(bak, src);
                    if (File.Exists(bak + ".meta")) File.Move(bak + ".meta", src + ".meta");
                }
            }
            if (movedPairs.Count > 0) AssetDatabase.Refresh();
            Debug.Log("[UTL BYOG] Pastas de StreamingAssets restauradas no projeto.");
        }
    }
}
#endif
