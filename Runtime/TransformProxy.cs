using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
    public class TransformProxy : MonoBehaviour
    {
        public enum SearchMethod
        {
            DirectReference,
            ByName,
            ByTag
        }

        [SerializeField] private SearchMethod searchMethod = SearchMethod.DirectReference;
        
        [SerializeField] private Transform point;
        [SerializeField] private string targetName;
        [SerializeField] private string targetTag;
        
        [SerializeField] private bool copyPosition = true;
        [SerializeField] private bool copyRotation = true;
        
        [SerializeField] private bool useRelativeMovement = false;
        [SerializeField] private Vector3 relativePositionScale = Vector3.one;

        private Transform targetTransform;
        private Vector3 initialTargetPosition;
        private Vector3 initialSelfPosition;
        private bool isInitialized = false;

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
                {
                    if (!useRelativeMovement)
                    {
                        // 単純コピー
                        transform.position = targetTransform.position;
                    }
                    else
                    {
                        // 倍率モード：初期位置からの差分に倍率を適用
                        if (!isInitialized)
                        {
                            initialTargetPosition = targetTransform.position;
                            initialSelfPosition = transform.position;
                            isInitialized = true;
                        }
                        
                        Vector3 offset = targetTransform.position - initialTargetPosition;
                        Vector3 scaledOffset = Vector3.Scale(offset, relativePositionScale);
                        transform.position = initialSelfPosition + scaledOffset;
                    }
                }
                
                if (copyRotation)
                    transform.rotation = targetTransform.rotation;
            }
        }
    }
}