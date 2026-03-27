using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine.SceneManagement;
using System.IO;

namespace JayT.UnityProductionUrpHelper.UnityRecorderBatchRunner
{
    /// <summary>
    /// PlayMode開始を検知して録画を開始し、完了後にPlayModeを終了する静的クラス。
    /// Editor/フォルダ内ではMonoBehaviourをGameObjectにアタッチできないため、
    /// [InitializeOnLoad] + EditorApplication.playModeStateChanged で代替する。
    /// </summary>
    [InitializeOnLoad]
    public static class BatchRecordingSession
    {
        private static RecorderController _recorder;

        static BatchRecordingSession()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            if (!PlayerPrefs.HasKey("JayT_ConfigPath")) return;

            string configPath = PlayerPrefs.GetString("JayT_ConfigPath");
            int index = PlayerPrefs.GetInt("JayT_RenderIndex", 0);

            string resolved = File.Exists(configPath) ? configPath : Path.GetFullPath(configPath);
            if (!File.Exists(resolved)) return;

            var config = JsonUtility.FromJson<RenderQueueConfig>(File.ReadAllText(resolved));
            if (index >= config.renderingList.Length) return;

            var item = config.renderingList[index];

            // backgroundシーンをActive Sceneに設定（環境ライト有効化）
            var bgScene = SceneManager.GetSceneByName(item.scene.background);
            if (bgScene.IsValid())
                SceneManager.SetActiveScene(bgScene);

            StartRecording(item, config.settings);
        }

        private static void StartRecording(RenderingItem item, RenderQueueSettings settings)
        {
            string outputFolder = PlayerPrefs.GetString("JayT_OutputPath", "");
            var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.OutputFile = Path.Combine(outputFolder, item.renderingId);
            movieSettings.name = item.renderingId;

            var cameraInput = new CameraInputSettings
            {
                Source = ImageSource.MainCamera,
                CaptureUI = false,
            };

            switch (settings.resolution)
            {
                case "4K":   cameraInput.OutputWidth = 3840; cameraInput.OutputHeight = 2160; break;
                case "2K":   cameraInput.OutputWidth = 2560; cameraInput.OutputHeight = 1440; break;
                case "720p": cameraInput.OutputWidth = 1280; cameraInput.OutputHeight = 720;  break;
                default:     cameraInput.OutputWidth = 1920; cameraInput.OutputHeight = 1080; break;
            }

            movieSettings.ImageInputSettings = cameraInput;
            movieSettings.FrameRate = settings.targetFPS;

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.AddRecorderSettings(movieSettings);
            controllerSettings.SetRecordModeToFrameInterval(item.frameInterval.start, item.frameInterval.end);
            controllerSettings.CapFrameRate = settings.capFPS;

            _recorder = new RecorderController(controllerSettings);
            _recorder.PrepareRecording();
            _recorder.StartRecording();

            Debug.Log($"[BatchRecordingSession] Recording started: {item.renderingId} ({item.frameInterval.start}-{item.frameInterval.end})");
        }

        private static void OnEditorUpdate()
        {
            if (_recorder == null || !EditorApplication.isPlaying) return;
            if (_recorder.IsRecording()) return;

            _recorder.StopRecording();
            _recorder = null;

            if (PlayerPrefs.HasKey("JayT_ConfigPath"))
            {
                int nextIndex = PlayerPrefs.GetInt("JayT_RenderIndex", 0) + 1;
                PlayerPrefs.SetInt("JayT_RenderIndex", nextIndex);
                PlayerPrefs.Save();
            }

            EditorApplication.ExitPlaymode();
        }
    }
}
