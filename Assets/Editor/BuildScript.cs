using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SteelTempest.EditorTools
{
    /// <summary>
    /// Headless build entrypoint for CI (GitHub Actions). Builds Android
    /// debug/release APKs and a Windows desktop build, and attaches a release
    /// keystore from environment variables when signing is requested.
    ///
    /// Expected env vars (injected from GitHub secrets):
    ///   STEEL_TEMPEST_KEYSTORE_B64 - base64 keystore
    ///   STEEL_TEMPEST_KEYSTORE_PASS, STEEL_TEMPEST_KEY_ALIAS,
    ///   STEEL_TEMPEST_KEY_PASS - signing passwords
    /// </summary>
    public static class BuildScript
    {
        private const string ReleaseEnvB64 = "STEEL_TEMPEST_KEYSTORE_B64";
        private const string EnvStorePass = "STEEL_TEMPEST_KEYSTORE_PASS";
        private const string EnvAlias = "STEEL_TEMPEST_KEY_ALIAS";
        private const string EnvKeyPass = "STEEL_TEMPEST_KEY_PASS";

        [MenuItem("Tools/Steel Tempest/Build Android Debug")]
        public static void BuildAndroidDebug() => BuildAndroid(BuildOptions.Development, false);

        [MenuItem("Tools/Steel Tempest/Build Android Release")]
        public static void BuildAndroidRelease() => BuildAndroid(BuildOptions.None, true);

        /// <summary>CI entrypoint: unity -batchmode -quit -executeMethod SteelTempest.EditorTools.BuildScript.CI_AndroidDebug</summary>
        public static void CI_AndroidDebug() => BuildAndroid(BuildOptions.Development, false);

        /// <summary>CI entrypoint for the release APK.</summary>
        public static void CI_AndroidRelease() => BuildAndroid(BuildOptions.None, true);

        /// <summary>CI entrypoint for the Windows build.</summary>
        public static void CI_Windows() => BuildWindows();

        private static void BuildAndroid(BuildOptions options, bool signed)
        {
            if (signed) ApplySigningFromEnv();

            var scenes = SceneList();
            var output = Path.Combine(BuildRoot(), signed ? "AndroidRelease.apk" : "AndroidDebug.apk");
            Directory.CreateDirectory(BuildRoot());

            var report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.Android, options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Android build failed: {report.summary.result}");
            }
            Debug.Log($"[SteelTempest] Android build OK -> {output}");
        }

        private static void BuildWindows()
        {
            var scenes = SceneList();
            var output = Path.Combine(BuildRoot(), "Windows/SteelTempest.exe");
            Directory.CreateDirectory(BuildRoot());

            var report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Windows build failed: {report.summary.result}");
            }
            Debug.Log($"[SteelTempest] Windows build OK -> {output}");
        }

        private static void ApplySigningFromEnv()
        {
            var b64 = Environment.GetEnvironmentVariable(ReleaseEnvB64);
            var storePass = Environment.GetEnvironmentVariable(EnvStorePass);
            var alias = Environment.GetEnvironmentVariable(EnvAlias);
            var keyPass = Environment.GetEnvironmentVariable(EnvKeyPass);

            if (string.IsNullOrEmpty(b64) || string.IsNullOrEmpty(storePass))
            {
                Debug.LogWarning("[SteelTempest] Release signing env vars missing; building unsigned.");
                return;
            }

            var keystore = Path.Combine(Path.GetTempPath(), "steel_tempest.keystore");
            File.WriteAllBytes(keystore, Convert.FromBase64String(b64));

            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = string.IsNullOrEmpty(alias) ? "steeltempest" : alias;
            PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyPass) ? storePass : keyPass;
        }

        private static string[] SceneList()
        {
            var scenes = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path)) list.Add(s.path);
            }
            if (list.Count == 0)
            {
                Debug.LogWarning("[SteelTempest] No enabled scenes in build settings; scanning Assets for scenes.");
                foreach (var sceneGuid in UnityEditor.AssetDatabase.FindAssets("t:Scene"))
                {
                    var scenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
                    if (scenePath.StartsWith("Assets/")) list.Add(scenePath);
                }
            }
            if (list.Count == 0)
            {
                Debug.LogWarning("[SteelTempest] No scenes found; building with an empty scene list.");
            }
            return list.ToArray();
        }

        private static string BuildRoot() =>
            Environment.GetEnvironmentVariable("STEEL_TEMPEST_BUILD_DIR") ?? "build";
    }
}