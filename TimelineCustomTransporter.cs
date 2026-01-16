using UnityEngine;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JayT.UnityProductionUrpHelper
{
    /// <summary>
    /// Timelineのトランスポート機能を拡張するコンポーネント
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineCustomTransporter : MonoBehaviour
    {
        [Header("再生制御")]
        [Tooltip("停止時に自動的にタイムラインを先頭に戻す")]
        [SerializeField] private bool autoRewindOnStop = false;

        private PlayableDirector director;

#if UNITY_EDITOR
        private PlayState lastPlayState = PlayState.Paused;

        private void OnEnable()
        {
            director = GetComponent<PlayableDirector>();
            EditorApplication.update += OnEditorUpdate;
            
            // ランタイムイベントも登録
            if (director != null)
            {
                director.stopped += OnPlayableDirectorStopped;
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            
            if (director != null)
            {
                director.stopped -= OnPlayableDirectorStopped;
            }
        }

        private void OnEditorUpdate()
        {
            if (director == null || !autoRewindOnStop)
                return;

            // エディタモードとランタイムの両方で状態監視
            PlayState currentState = director.state;

            if (lastPlayState == PlayState.Playing && currentState == PlayState.Paused)
            {
                director.time = 0;
                director.Evaluate();
                Debug.Log($"[TimelineCustomTransporter] Rewound to start (isPlaying: {Application.isPlaying})");
            }

            lastPlayState = currentState;
        }

        private void OnPlayableDirectorStopped(PlayableDirector stoppedDirector)
        {
            if (autoRewindOnStop && stoppedDirector == director)
            {
                director.time = 0;
                director.Evaluate();
                Debug.Log("[TimelineCustomTransporter] Rewound to start (Runtime event)");
            }
        }
#else
        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.stopped += OnPlayableDirectorStopped;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.stopped -= OnPlayableDirectorStopped;
            }
        }

        private void OnPlayableDirectorStopped(PlayableDirector stoppedDirector)
        {
            if (autoRewindOnStop && stoppedDirector == director)
            {
                director.time = 0;
                director.Evaluate();
            }
        }
#endif
    }
}