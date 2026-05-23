using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;


namespace JayT.UnityProductionUrpHelper
{
    [InitializeOnLoad]
    static class AnimationClipExporterSelectionCache
    {
        static List<AnimationTrack> _cachedTracks = new();
        static float _cachedFrameRate = 60f;
        static UnityEngine.Object[] _lastKnownSelection = new UnityEngine.Object[0];

        static AnimationClipExporterSelectionCache()
        {
            EditorApplication.update += PollSelection;
        }

        static void PollSelection()
        {
            var current = Selection.objects;
            if (current == _lastKnownSelection) return;
            _lastKnownSelection = current;

            _cachedTracks = current
                .OfType<AnimationTrack>()
                .ToList();

            var asset = TimelineEditor.inspectedAsset;
            if (asset != null)
                _cachedFrameRate = (float)asset.editorSettings.frameRate;

            foreach (var w in Resources.FindObjectsOfTypeAll<ExportRangeDialog>())
                w.Repaint();
        }

        public static List<AnimationTrack> GetTracks() => _cachedTracks;
        public static float GetFrameRate() => _cachedFrameRate;
    }
}
