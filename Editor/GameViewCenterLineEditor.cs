using UnityEditor;
using UnityEngine;

namespace JayT.UnityProductionUrpHelper.Editor
{
    [CustomEditor(typeof(GameViewCenterLine))]
    public class GameViewCenterLineEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "任意の GameObject にアタッチして使用します。\n再生中・停止中どちらでも Gameビューへオーバーレイを描画します。",
                MessageType.Info
            );
            EditorGUILayout.Space(4);
            DrawDefaultInspector();
        }
    }
}
