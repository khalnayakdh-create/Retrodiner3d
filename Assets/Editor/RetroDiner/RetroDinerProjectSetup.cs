using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RetroDiner.EditorTools
{
    /// <summary>
    /// Applies the handful of project settings the build prompt requires (portrait, product name,
    /// Boot scene in the build list) once per editor session, so no ProjectSettings YAML needs hand-editing.
    /// </summary>
    [InitializeOnLoad]
    public static class RetroDinerProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Boot.unity";
        const string SessionKey = "RetroDiner.ProjectSetup.Done";

        static RetroDinerProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionKey, false)) return;
                SessionState.SetBool(SessionKey, true);
                ApplySettings();
            };
        }

        [MenuItem("Retro Diner/Apply Project Settings (portrait, product name, build scenes)", false, 40)]
        public static void ApplySettings()
        {
            PlayerSettings.productName = "Retro Diner 3D: Order Up!";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "Project Blue Plate";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool present = false;
            foreach (var s in scenes) if (s.path == ScenePath) present = true;
            if (!present && System.IO.File.Exists(ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log("[RetroDiner] Project settings applied (portrait, product name, Boot scene in build list).");
        }

        /// <summary>Fallback if Boot.unity fails to import: rebuilds it from code.</summary>
        [MenuItem("Retro Diner/Recreate Boot Scene", false, 41)]
        public static void RecreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<RetroDiner.Boot.GameBootstrap>();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            ApplySettings();
            Debug.Log("[RetroDiner] Boot scene recreated at " + ScenePath);
        }

        [MenuItem("Retro Diner/Open Boot Scene", false, 42)]
        public static void OpenBootScene()
        {
            if (System.IO.File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else RecreateBootScene();
        }
    }
}
