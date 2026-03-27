using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace JayT.UnityProductionUrpHelper.UnityRecorderBatchRunner
{
    /// <summary>
    /// JSONキューを読み込み、1件ずつシーンを開いてPlayModeに入るEditorWindow。
    /// 録画の実際の制御はBatchRecordingSessionが行う。
    /// </summary>
    public class UnityRecorderBatchRunner : EditorWindow
    {
        private string _jsonPath = "";
        private RenderQueueConfig _config;

        [MenuItem("Tools/JayT/ProductionUrpHelper/UnityRecorderBatchRunner")]
        public static void ShowWindow()
        {
            GetWindow<UnityRecorderBatchRunner>("UnityRecorderBatchRunner");
        }

        private void OnEnable()
        {
            if (IsBatchRunning)
            {
                string path = PlayerPrefs.GetString("JayT_ConfigPath", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _jsonPath = path;
                    TryLoadConfig(path);
                }
            }
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        public static bool IsBatchRunning => PlayerPrefs.HasKey("JayT_ConfigPath");

        private void OnGUI()
        {
            GUILayout.Label("UnityRecorderBatchRunner", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(IsBatchRunning))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _jsonPath = EditorGUILayout.TextField("Config JSON", _jsonPath);
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        string selected = EditorUtility.OpenFilePanel("Select RenderQueueConfig", Application.dataPath, "json");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            _jsonPath = selected;
                            TryLoadConfig(_jsonPath);
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_jsonPath)))
                {
                    if (GUILayout.Button("Load Config"))
                        TryLoadConfig(_jsonPath);
                }
            }

            if (_config != null)
            {
                GUILayout.Space(8);
                GUILayout.Label($"Loaded: {_config.renderingList.Length} items  |  FPS: {_config.settings.targetFPS}  |  {_config.settings.resolution}");
            }

            GUILayout.Space(10);

            if (IsBatchRunning)
            {
                int currentIdx = PlayerPrefs.GetInt("JayT_RenderIndex", 0);
                int total = _config?.renderingList.Length ?? 0;
                EditorGUILayout.HelpBox($"Rendering {currentIdx + 1} / {total} ...", MessageType.Info);

                if (GUILayout.Button("Stop Batch"))
                    StopBatch();
            }
            else
            {
                using (new EditorGUI.DisabledScope(_config == null))
                {
                    if (GUILayout.Button("Start Batch Render"))
                    {
                        PlayerPrefs.SetInt("JayT_RenderIndex", 0);
                        PlayerPrefs.SetString("JayT_ConfigPath", _jsonPath);
                        PlayerPrefs.Save();
                        RunNext();
                    }
                }
            }
        }

        private void TryLoadConfig(string path)
        {
            string resolved = File.Exists(path) ? path : Path.GetFullPath(path);
            if (!File.Exists(resolved))
            {
                Debug.LogError($"[UnityRecorderBatchRunner] Config not found: {path}");
                return;
            }
            _config = JsonUtility.FromJson<RenderQueueConfig>(File.ReadAllText(resolved));
            Debug.Log($"[UnityRecorderBatchRunner] Config loaded: {_config.renderingList.Length} items");
        }

        public static void StopBatch()
        {
            PlayerPrefs.DeleteKey("JayT_RenderIndex");
            PlayerPrefs.DeleteKey("JayT_ConfigPath");
            PlayerPrefs.Save();
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            Debug.Log("[UnityRecorderBatchRunner] Batch stopped.");
        }

        public static void RunNext()
        {
            string configPath = PlayerPrefs.GetString("JayT_ConfigPath", "");
            int index = PlayerPrefs.GetInt("JayT_RenderIndex", 0);

            if (string.IsNullOrEmpty(configPath)) return;

            string resolved = File.Exists(configPath) ? configPath : Path.GetFullPath(configPath);
            if (!File.Exists(resolved))
            {
                Debug.LogError($"[UnityRecorderBatchRunner] Config file not found: {configPath}");
                StopBatch();
                return;
            }

            var config = JsonUtility.FromJson<RenderQueueConfig>(File.ReadAllText(resolved));

            if (index >= config.renderingList.Length)
            {
                Debug.Log("[UnityRecorderBatchRunner] All renders complete.");
                PlayerPrefs.DeleteKey("JayT_RenderIndex");
                PlayerPrefs.DeleteKey("JayT_ConfigPath");
                PlayerPrefs.Save();
                return;
            }

            var item = config.renderingList[index];
            string bgPath       = FindScenePath(item.scene.background);
            string mainPath     = FindScenePath(item.scene.main);
            string timelinePath = FindScenePath(item.scene.timeline);

            if (string.IsNullOrEmpty(bgPath) || string.IsNullOrEmpty(mainPath) || string.IsNullOrEmpty(timelinePath))
            {
                Debug.LogError($"[UnityRecorderBatchRunner] Aborting: one or more scenes not found for [{index}] {item.renderingId}");
                StopBatch();
                return;
            }

            Debug.Log($"[UnityRecorderBatchRunner] Starting render [{index}]: {item.renderingId}");

            EditorSceneManager.OpenScene(bgPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Additive);
            EditorSceneManager.OpenScene(timelinePath, OpenSceneMode.Additive);

            // 録画開始はBatchRecordingSessionがEnteredPlayModeイベントで行う
            EditorApplication.EnterPlaymode();
        }

        private static string FindScenePath(string sceneName)
        {
            var guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                    return path;
            }
            Debug.LogError($"[UnityRecorderBatchRunner] Scene not found: {sceneName}");
            return "";
        }
    }
}
