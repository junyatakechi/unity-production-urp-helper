using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace JayT.UnityProductionUrpHelper
{
    [ExecuteAlways]
    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineBindingResolver : MonoBehaviour
    {
        [Serializable]
        public class BindingEntry
        {
            public string trackName;
            public string targetObjectName;
        }

        [SerializeField] private List<BindingEntry> bindings = new();

        private PlayableDirector _director;

        void OnEnable()
        {
            _director = GetComponent<PlayableDirector>();

            // PlayMode / Editor Preview 両方で反映
            ResolveBindings();

            // Editor Preview で即反映させるため
            if (!Application.isPlaying)
            {
                _director.Evaluate();
            }
        }

        public void ResolveBindings()
        {
            if (_director == null || _director.playableAsset == null) return;
            if (_director.playableAsset is not TimelineAsset timelineAsset) return;

            foreach (var output in timelineAsset.outputs)
            {
                // MarkerTrackは除外
                if (output.sourceObject is MarkerTrack) continue;

                var entry = bindings.Find(b => b.trackName == output.streamName);
                if (entry == null || string.IsNullOrEmpty(entry.targetObjectName)) continue;

                var targetObject = GameObject.Find(entry.targetObjectName);
                if (targetObject == null)
                {
                    Debug.LogWarning($"[TimelineBindingResolver] '{entry.targetObjectName}' not found");
                    continue;
                }

                var sourceObject = output.sourceObject;

                switch (sourceObject)
                {
                    case AnimationTrack:
                        var animator = targetObject.GetComponent<Animator>();
                        if (animator != null)
                            _director.SetGenericBinding(sourceObject, animator);
                        break;

                    case ActivationTrack:
                        _director.SetGenericBinding(sourceObject, targetObject);
                        break;

                    default:
                        _director.SetGenericBinding(sourceObject, targetObject);
                        break;
                }
            }
        }

        [ContextMenu("Auto Populate Track Names")]
        private void AutoPopulateTrackNames()
        {
            if (_director == null || _director.playableAsset == null) return;
            if (_director.playableAsset is not TimelineAsset timelineAsset) return;

            // 1. 現在のデータを一時的に辞書に保存
            var existingBindings = new Dictionary<string, string>();
            foreach (var b in bindings)
            {
                if (!string.IsNullOrEmpty(b.trackName))
                {
                    existingBindings[b.trackName] = b.targetObjectName;
                }
            }

            // 2. リストを再構成
            bindings.Clear();
            foreach (var output in timelineAsset.outputs)
            {
                // MarkerTrackは除外
                if (output.sourceObject is MarkerTrack) continue;
                if (output.streamName is "Markers") continue;

                string trackName = output.streamName;
                if (string.IsNullOrEmpty(trackName)) continue;

                // 以前のバインディング情報を引き継ぐ
                string targetName = existingBindings.ContainsKey(trackName) 
                    ? existingBindings[trackName] 
                    : "";

                bindings.Add(new BindingEntry 
                { 
                    trackName = trackName, 
                    targetObjectName = targetName 
                });
            }

            Debug.Log($"[TimelineBindingResolver] Updated {bindings.Count} tracks (Preserved existing names)");
        }
    }
}