﻿using UnityEditor;

namespace StansAssets.SceneManagement.Build
{
    [InitializeOnLoad]
    public class BuildConfigurationSettingsValidator
    {
        public const string TAG = "[Build Configuration]";

        static BuildConfigurationSettingsValidator() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state) {
            switch (state) {
                case PlayModeStateChange.EnteredPlayMode:
                    if (BuildConfigurationSettings.Instance.HasValidConfiguration) {
                        BuildConfigurationSettings.Instance.Configuration.SetupBuildSettings(EditorUserBuildSettings.activeBuildTarget);
                    }
                    break;
            }
        }
    }
}
