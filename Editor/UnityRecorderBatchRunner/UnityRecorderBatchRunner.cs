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
        private string _outputPath = "";
        private RenderQueueConfig _config;
        private int _rangeFrom = 0;
        private int _rangeTo = 1;
        private Vector2 _scrollPos;

        [MenuItem("Tools/JayT/ProductionUrpHelper/UnityRecorderBatchRunner")]
        public static void ShowWindow()
        {
            GetWindow<UnityRecorderBatchRunner>("UnityRecorderBatchRunner");
        }

        private void OnEnable()
        {
            _outputPath = PlayerPrefs.GetString("JayT_OutputPath", "");
            if (IsBatchRunning)
            {
                string path = PlayerPrefs.GetString("JayT_ConfigPath", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _jsonPath = path;
                    TryLoadConfig(path);
                }
                _rangeFrom = PlayerPrefs.GetInt("JayT_RenderIndexStart", 1);
                _rangeTo   = PlayerPrefs.GetInt("JayT_RenderIndexEnd", 1);
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
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_jsonPath)))
                {
                    if (GUILayout.Button("Load Config"))
                        TryLoadConfig(_jsonPath);
                }

                GUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _outputPath = EditorGUILayout.TextField("Output Folder", _outputPath);
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("Select Output Folder", _outputPath, "");
                        if (!string.IsNullOrEmpty(selected))
                            _outputPath = selected;
                    }
                }
            }

            if (_config != null)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Settings", $"FPS: {_config.settings.targetFPS}  |  {_config.settings.resolution}  |  {_config.settings.encoder}  |  Audio: {_config.settings.includeAudio}");

                GUILayout.Space(4);
                EditorGUILayout.LabelField($"Queue: {_config.renderingList.Length} items", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(IsBatchRunning))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Range", GUILayout.Width(50));
                        EditorGUILayout.LabelField("From", GUILayout.Width(38));
                        _rangeFrom = EditorGUILayout.IntField(_rangeFrom, GUILayout.Width(45));
                        EditorGUILayout.LabelField("To", GUILayout.Width(20));
                        _rangeTo = EditorGUILayout.IntField(_rangeTo, GUILayout.Width(45));
                    }
                }

                if (true)
                {
                    GUILayout.Space(2);
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(110));
                    for (int i = 0; i < _config.renderingList.Length; i++)
                    {
                        var item = _config.renderingList[i];
                        bool inRange = i >= _rangeFrom && i < _rangeTo;

                        if (IsBatchRunning)
                        {
                            int currentIdx = PlayerPrefs.GetInt("JayT_RenderIndex", 0);
                            string status = i < currentIdx ? "✓" : i == currentIdx ? "▶" : " ";
                            EditorGUILayout.LabelField($"{status} {i}. {item.renderingId}  [{item.frameInterval.start}-{item.frameInterval.end}]");
                        }
                        else if (inRange)
                        {
                            EditorGUILayout.LabelField($"  {i}. {item.renderingId}  [{item.frameInterval.start}-{item.frameInterval.end}]");
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

            GUILayout.Space(10);

            if (IsBatchRunning)
            {
                int currentIdx = PlayerPrefs.GetInt("JayT_RenderIndex", 0);
                int endIdx     = PlayerPrefs.GetInt("JayT_RenderIndexEnd", _rangeTo);
                int total      = endIdx - PlayerPrefs.GetInt("JayT_RenderIndexStart", _rangeFrom);
                int progress   = currentIdx - PlayerPrefs.GetInt("JayT_RenderIndexStart", _rangeFrom) + 1;
                EditorGUILayout.HelpBox($"Rendering {Mathf.Max(1, progress)} / {total}  ({_config?.renderingList[currentIdx].renderingId ?? ""})", MessageType.Info);

                if (GUILayout.Button("Stop Batch"))
                    StopBatch();
            }
            else
            {
                using (new EditorGUI.DisabledScope(_config == null || string.IsNullOrEmpty(_outputPath)))
                {
                    if (GUILayout.Button("Start Batch Render"))
                    {
                        PlayerPrefs.SetInt("JayT_RenderIndex",      _rangeFrom);
                        PlayerPrefs.SetInt("JayT_RenderIndexStart",  _rangeFrom);
                        PlayerPrefs.SetInt("JayT_RenderIndexEnd",    _rangeTo);
                        PlayerPrefs.SetString("JayT_ConfigPath",     _jsonPath);
                        PlayerPrefs.SetString("JayT_OutputPath",     _outputPath);
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
            _rangeFrom = 0;
            _rangeTo = _config.renderingList?.Length ?? 0;
            Debug.Log($"[UnityRecorderBatchRunner] Config loaded: {_rangeTo} items");
        }

        public static void StopBatch()
        {
            PlayerPrefs.DeleteKey("JayT_RenderIndex");
            PlayerPrefs.DeleteKey("JayT_RenderIndexStart");
            PlayerPrefs.DeleteKey("JayT_RenderIndexEnd");
            PlayerPrefs.DeleteKey("JayT_ConfigPath");
            PlayerPrefs.DeleteKey("JayT_OutputPath");
            PlayerPrefs.Save();
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            Debug.Log("[UnityRecorderBatchRunner] Batch stopped.");
        }

        public static void RunNext()
        {
            string configPath = PlayerPrefs.GetString("JayT_ConfigPath", "");
            int index         = PlayerPrefs.GetInt("JayT_RenderIndex", 0);
            int endIndex      = PlayerPrefs.GetInt("JayT_RenderIndexEnd", int.MaxValue);

            if (string.IsNullOrEmpty(configPath)) return;

            string resolved = File.Exists(configPath) ? configPath : Path.GetFullPath(configPath);
            if (!File.Exists(resolved))
            {
                Debug.LogError($"[UnityRecorderBatchRunner] Config file not found: {configPath}");
                StopBatch();
                return;
            }

            var config = JsonUtility.FromJson<RenderQueueConfig>(File.ReadAllText(resolved));

            if (index >= endIndex || index >= config.renderingList.Length)
            {
                Debug.Log("[UnityRecorderBatchRunner] All renders complete.");
                PlayerPrefs.DeleteKey("JayT_RenderIndex");
                PlayerPrefs.DeleteKey("JayT_RenderIndexStart");
                PlayerPrefs.DeleteKey("JayT_RenderIndexEnd");
                PlayerPrefs.DeleteKey("JayT_ConfigPath");
                PlayerPrefs.DeleteKey("JayT_OutputPath");
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
