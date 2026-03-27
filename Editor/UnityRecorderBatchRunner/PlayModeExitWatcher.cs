// Assets/Scripts/Editor/JayT/PlayModeExitWatcher.cs
using UnityEngine;
using UnityEditor;

namespace JayT.UnityProductionUrpHelper.UnityRecorderBatchRunner
{
    /// <summary>
    /// PlayMode終了を検知して次のRenderキューを実行する。
    /// InitializeOnLoad で Editor起動時に自動登録される。
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeExitWatcher
    {
        static PlayModeExitWatcher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            if (!PlayerPrefs.HasKey("JayT_RenderIndex")) return;
            if (!PlayerPrefs.HasKey("JayT_ConfigPath")) return;

            // 次のキューを実行
            UnityRecorderBatchRunner.RunNext();
        }
    }
}