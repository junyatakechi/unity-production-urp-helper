using UnityEngine;
using UnityEditor;
using System.IO;

namespace JayT.UnityProductionUrpHelper.Editor
{
    public class ExtractAnimationClips : EditorWindow
    {
        private string sourceFolderPath = "Assets/FBX";
        private string outputFolderPath = "Assets/Animations";

        // 命名モードを定義
        private enum NamingMode
        {
            ClipName, // 元のアニメーションクリップ名を使用
            FBXName   // FBXのファイル名を使用
        }

        private NamingMode namingMode = NamingMode.ClipName;

        [MenuItem("Tools/Extract Animation Clips")]
        static void ShowWindow()
        {
            GetWindow<ExtractAnimationClips>("Extract Animations");
        }

        void OnGUI()
        {
            GUILayout.Label("FBXからAnimationClipを抽出", EditorStyles.boldLabel);
            
            sourceFolderPath = EditorGUILayout.TextField("FBXフォルダ", sourceFolderPath);
            outputFolderPath = EditorGUILayout.TextField("出力フォルダ", outputFolderPath);

            // UIに選択肢を追加
            namingMode = (NamingMode)EditorGUILayout.EnumPopup("命名規則", namingMode);

            if (GUILayout.Button("抽出実行"))
            {
                ExtractAll();
            }
        }

        string GetRigTypeName(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return "Unknown";

            return importer.animationType switch
            {
                ModelImporterAnimationType.Human => "Huma",
                ModelImporterAnimationType.Generic => "Gene",
                ModelImporterAnimationType.Legacy => "Lega",
                _ => "None"
            };
        }

        void ExtractAll()
        {
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
                AssetDatabase.Refresh();
            }

            string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { sourceFolderPath });
            int count = 0;

            foreach (string guid in fbxGuids)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // FBXのファイル名（拡張子なし）を取得
                string fbxFileName = Path.GetFileNameWithoutExtension(fbxPath);
                string folderName = Path.GetFileName(Path.GetDirectoryName(fbxPath));
                string rigType = GetRigTypeName(fbxPath);
                
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

                foreach (Object asset in assets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        AnimationClip newClip = Object.Instantiate(clip);
                        
                        // 選択されたモードに応じて名前を決定
                        string targetName = (namingMode == NamingMode.ClipName) ? clip.name : fbxFileName;
                        newClip.name = $"{folderName}-{rigType}-{targetName}";
                        
                        string outputPath = $"{outputFolderPath}/{newClip.name}.anim";
                        outputPath = AssetDatabase.GenerateUniqueAssetPath(outputPath);
                        
                        AssetDatabase.CreateAsset(newClip, outputPath);
                        count++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完了", $"{count}個のAnimationClipを抽出しました", "OK");
        }
    } 
}
