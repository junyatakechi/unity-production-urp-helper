using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace JayT.UnityProductionUrpHelper
{
    /// <summary>
    /// Timelineのアニメーショントラックの指定フレーム範囲を.animファイルに書き出すエディタ拡張。
    ///
    /// 使い方：
    /// TimelineエディタのAnimationトラック上のクリップを右クリック
    /// → "Export Range as AnimationClip" を選択
    /// → 開始・終了フレームを入力して実行
    /// → Assets/ExportedClips/ に保存される
    ///
    /// 制限：
    /// - Generic（Transformベース）のみ対応。
    /// - HumanoidのMuscle曲線はUnity公式APIで取得不可のため非対応。
    /// </summary>
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

    public static class GenericOnlyAnimationClipExporter
    {
        private const string OutputFolder = "Assets/ExportedClips";

        [MenuItem("Tools/JayT/ProductionUrpHelper/Export Range as AnimationClip")]
        public static void ExportRange()
        {
            EditorWindow.GetWindow<ExportRangeDialog>(false, "Export Range", true);
        }

        internal static void ExecuteExport(List<AnimationTrack> tracks, int startFrame, int endFrame, float frameRate)
        {
            // 出力フォルダを作成
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets", "ExportedClips");
            }

            float startTime = startFrame / frameRate;
            float endTime = endFrame / frameRate;

            foreach (var track in tracks)
            {
                ExportTrackRange(track, startTime, endTime, frameRate, startFrame, endFrame);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ExportTrackRange(
            AnimationTrack track,
            float startTime,
            float endTime,
            float frameRate,
            int startFrame,
            int endFrame)
        {
            var clips = track.GetClips()
                .Where(c => c.end > startTime && c.start < endTime)
                .OrderBy(c => c.start)
                .ToList();

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[GenericOnlyAnimationClipExporter] トラック '{track.name}' の指定範囲にクリップがありません。");
                return;
            }

            var outputClip = new AnimationClip();
            outputClip.frameRate = frameRate;

            foreach (var timelineClip in clips)
            {
                var sourceClip = timelineClip.animationClip;
                if (sourceClip == null)
                    continue;

                // Timeline上の時間情報
                float clipTimelineStart = (float)timelineClip.start;
                float clipIn = (float)timelineClip.clipIn;

                // 指定範囲とクリップの重複区間を計算
                float rangeStart = Mathf.Max(startTime, clipTimelineStart);
                float rangeEnd = Mathf.Min(endTime, (float)timelineClip.end);

                // ソースclip内での対応時間
                float sourceStart = clipIn + (rangeStart - clipTimelineStart);
                float sourceEnd = clipIn + (rangeEnd - clipTimelineStart);

                // 出力clip内での時間オフセット（範囲の先頭を0に正規化）
                float timeOffset = rangeStart - startTime;

                CopyCurvesWithOffset(sourceClip, outputClip, sourceStart, sourceEnd, timeOffset);
            }

            // ファイル名：トラック名_開始f_終了f
            string fileName = $"{SanitizeFileName(track.name)}_{startFrame}f_{endFrame}f.anim";
            string savePath = Path.Combine(OutputFolder, fileName);
            savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

            AssetDatabase.CreateAsset(outputClip, savePath);
            EditorGUIUtility.PingObject(outputClip);
            Debug.Log($"[GenericOnlyAnimationClipExporter] 出力完了: {savePath}");
        }

        private static void CopyCurvesWithOffset(
            AnimationClip sourceClip,
            AnimationClip outputClip,
            float sourceStart,
            float sourceEnd,
            float timeOffset)
        {
            // Float曲線
            var bindings = AnimationUtility.GetCurveBindings(sourceClip);
            foreach (var binding in bindings)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                if (sourceCurve == null)
                    continue;

                var newKeys = new List<Keyframe>();

                // 範囲開始の値をサンプリングして先頭キーを追加
                newKeys.Add(new Keyframe(timeOffset, sourceCurve.Evaluate(sourceStart)));

                // 範囲内のキーをコピー
                foreach (var key in sourceCurve.keys)
                {
                    if (key.time <= sourceStart + 0.0001f) continue;
                    if (key.time >= sourceEnd - 0.0001f) continue;

                    var newKey = key;
                    newKey.time = (key.time - sourceStart) + timeOffset;
                    newKeys.Add(newKey);
                }

                // 範囲終了の値をサンプリングして末尾キーを追加
                newKeys.Add(new Keyframe(timeOffset + (sourceEnd - sourceStart), sourceCurve.Evaluate(sourceEnd)));

                newKeys = newKeys.OrderBy(k => k.time).ToList();

                // 既存曲線にマージ
                var existingCurve = AnimationUtility.GetEditorCurve(outputClip, binding);
                if (existingCurve != null && existingCurve.keys.Length > 0)
                {
                    var mergedKeys = existingCurve.keys.ToList();
                    mergedKeys.AddRange(newKeys);
                    mergedKeys = mergedKeys.OrderBy(k => k.time).ToList();
                    AnimationUtility.SetEditorCurve(outputClip, binding, new AnimationCurve(mergedKeys.ToArray()));
                }
                else
                {
                    AnimationUtility.SetEditorCurve(outputClip, binding, new AnimationCurve(newKeys.ToArray()));
                }
            }

            // ObjectReference曲線（Sprite等）
            var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
            foreach (var binding in objBindings)
            {
                var sourceKeys = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                var newKeys = new List<ObjectReferenceKeyframe>();

                // 範囲開始をサンプリング
                var firstKey = sourceKeys.LastOrDefault(k => k.time <= sourceStart);
                if (firstKey.value != null)
                    newKeys.Add(new ObjectReferenceKeyframe { time = timeOffset, value = firstKey.value });

                foreach (var k in sourceKeys)
                {
                    if (k.time <= sourceStart + 0.0001f) continue;
                    if (k.time >= sourceEnd - 0.0001f) continue;
                    newKeys.Add(new ObjectReferenceKeyframe
                    {
                        time = (k.time - sourceStart) + timeOffset,
                        value = k.value
                    });
                }

                if (newKeys.Count > 0)
                    AnimationUtility.SetObjectReferenceCurve(outputClip, binding, newKeys.ToArray());
            }
        }

        internal static float GetFrameRate(AnimationTrack track)
        {
            var clip = track.GetClips().FirstOrDefault();
            return clip?.animationClip != null ? clip.animationClip.frameRate : 60f;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }

    /// <summary>
    /// フレーム範囲入力ダイアログ
    /// </summary>
    public class ExportRangeDialog : EditorWindow
    {
        private int _startFrame;
        private int _endFrame = 100;

        private void OnEnable()
        {
            minSize = new Vector2(300, 200);
        }

        private void OnGUI()
        {
            var tracks = AnimationClipExporterSelectionCache.GetTracks();
            bool hasDirector = TimelineEditor.inspectedDirector != null;
            bool hasTracks = tracks.Count > 0;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("状態", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Timeline を開いている", hasDirector);
                EditorGUILayout.Toggle("AnimationTrack を選択中", hasTracks);
            }

            if (hasTracks)
            {
                EditorGUILayout.Space(2);
                foreach (var t in tracks)
                    EditorGUILayout.LabelField($"  • {t.name}  ({AnimationClipExporterSelectionCache.GetFrameRate()} fps)", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("出力フレーム範囲", EditorStyles.boldLabel);

            _startFrame = EditorGUILayout.IntField("開始フレーム", _startFrame);
            _endFrame = EditorGUILayout.IntField("終了フレーム", _endFrame);

            if (_startFrame < 0) _startFrame = 0;
            if (_endFrame <= _startFrame) _endFrame = _startFrame + 1;

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("出力先: Assets/ExportedClips/", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(!hasDirector || !hasTracks))
            {
                if (GUILayout.Button("Export"))
                {
                    GenericOnlyAnimationClipExporter.ExecuteExport(
                        tracks, _startFrame, _endFrame,
                        AnimationClipExporterSelectionCache.GetFrameRate());
                }
            }
        }
    }
}