using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine.Playables;
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

            // timelineシーンのdirectorのみ Play() する。
            // サブタイムライン(ControlTrackで制御)のdirectorを独立にPlay()すると
            // 親と競合してモーションが早送りになるため除外する。
            if (config.settings.includeAudio)
            {
                int sr = AudioSettings.outputSampleRate;
                if (sr != 44100 && sr != 48000)
                    Debug.LogWarning($"[BatchRecordingSession] Audio sample rate is {sr} Hz. Recommend 44100 or 48000 for standard video output.");
            }

            string timelineSceneName = item.scene.timeline;
            foreach (var director in Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None))
            {
                if (director.gameObject.scene.name != timelineSceneName) continue;
                // DSPClockはリアルタイム再生での音声同期用。オフラインレンダリングでは
                // Recorderが制御するゲーム時間(Time.captureFramerate)と乖離し、
                // 重いフレームでDSP時間がゲーム時間より進んでモーションが早送りになる。
                director.timeUpdateMode = DirectorUpdateMode.GameTime;
                director.Play();
            }
        }

        private static void StartRecording(RenderingItem item, RenderQueueSettings settings)
        {
            string outputFolder = PlayerPrefs.GetString("JayT_OutputPath", "");
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.OutputFile = Path.Combine(outputFolder, $"{item.renderingId}_{timestamp}");
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
            movieSettings.AudioInputSettings.PreserveAudio = settings.includeAudio;

            if (settings.encoder == "ProRes")
            {
                var proRes = new ProResEncoderSettings();
                proRes.Format = settings.proResCodec switch
                {
                    "ap4x" => ProResEncoderSettings.OutputFormat.ProRes4444XQ,
                    "ap4h" => ProResEncoderSettings.OutputFormat.ProRes4444,
                    "apch" => ProResEncoderSettings.OutputFormat.ProRes422HQ,
                    "apcn" => ProResEncoderSettings.OutputFormat.ProRes422,
                    "apcs" => ProResEncoderSettings.OutputFormat.ProRes422LT,
                    "apco" => ProResEncoderSettings.OutputFormat.ProRes422Proxy,
                    _      => ProResEncoderSettings.OutputFormat.ProRes4444XQ,
                };
                movieSettings.EncoderSettings = proRes;
            }
            else
            {
                movieSettings.EncoderSettings = new CoreEncoderSettings
                {
                    Codec = CoreEncoderSettings.OutputCodec.MP4,
                };
            }

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.AddRecorderSettings(movieSettings);
            // directorはinitialTimeからスタート済みのため、Recorderは0から(end-start)フレームを録画する
            // SetRecordModeToFrameInterval の end は inclusive なので -1 して正確なフレーム数にする
            int durationFrames = item.frameInterval.end - item.frameInterval.start;
            controllerSettings.SetRecordModeToFrameInterval(0, durationFrames - 1);
            controllerSettings.FrameRate = settings.targetFPS;
            // 録画中は Time.deltaTime を 強制的に固定値 に置き換える
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            // includeAudio=true の場合は必ず CapFrameRate=true にする。
            // 音声はDSPクロック(実時間)で動くため、CapFrameRate=false で描画が実時間より
            // 速く進むと映像と音声が大きくずれる。
            controllerSettings.CapFrameRate = settings.includeAudio || settings.capFPS;

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
