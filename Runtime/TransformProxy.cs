using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
    public class TransformProxy : MonoBehaviour
    {
        public enum SearchMethod
        {
            DirectReference,  // 直接アサイン（同一シーン用）
            ByName,          // 名前検索
            ByTag            // タグ検索
        }

        [SerializeField] private SearchMethod searchMethod = SearchMethod.DirectReference;
        
        [SerializeField] private Transform point;  // DirectReference用
        [SerializeField] private string targetName;  // ByName用
        [SerializeField] private string targetTag;   // ByTag用
        
        [SerializeField] private bool copyPosition = true;
        [SerializeField] private bool copyRotation = true;

        private Transform targetTransform;

        private void Start()
        {
            ResolveTarget();
        }

        private void ResolveTarget()
        {
            switch (searchMethod)
            {
                case SearchMethod.DirectReference:
                    targetTransform = point;
                    break;
                    
                case SearchMethod.ByName:
                    if (!string.IsNullOrEmpty(targetName))
                    {
                        GameObject targetObj = GameObject.Find(targetName);
                        if (targetObj != null)
                        {
                            targetTransform = targetObj.transform;
                        }
                        else
                        {
                            Debug.LogWarning($"TransformProxy: '{targetName}' が見つかりません");
                        }
                    }
                    break;
                    
                case SearchMethod.ByTag:
                    if (!string.IsNullOrEmpty(targetTag))
                    {
                        GameObject targetObj = GameObject.FindGameObjectWithTag(targetTag);
                        if (targetObj != null)
                        {
                            targetTransform = targetObj.transform;
                        }
                        else
                        {
                            Debug.LogWarning($"TransformProxy: タグ '{targetTag}' が見つかりません");
                        }
                    }
                    break;
            }
        }

        private void LateUpdate()
        {
            if (targetTransform != null)
            {
                if (copyPosition)
                    transform.position = targetTransform.position;
                
                if (copyRotation)
                    transform.rotation = targetTransform.rotation;
            }
        }
    }
}