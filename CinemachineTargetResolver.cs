using UnityEngine;
using Cinemachine;

namespace JayT.UnityProductionUrpHelper
{
    public class CinemachineTargetResolver : MonoBehaviour
    {
        private CinemachineVirtualCamera virtualCamera;
        
        [Header("Follow Target")]
        [SerializeField] private bool setFollowTarget = false;
        [SerializeField] private string followTargetName;

        [Header("LookAt Target")]
        [SerializeField] private bool setLookAtTarget = false;
        [SerializeField] private string lookAtTargetName;

        private void Start()
        {
            if (virtualCamera == null)
            {
                virtualCamera = GetComponent<CinemachineVirtualCamera>();
            }

            if (virtualCamera == null)
            {
                Debug.LogError("CinemachineTargetResolver: CinemachineVirtualCamera が見つかりません");
                return;
            }

            ResolveTargets();
        }

        private void ResolveTargets()
        {
            if (setFollowTarget && !string.IsNullOrEmpty(followTargetName))
            {
                GameObject followObj = GameObject.Find(followTargetName);
                if (followObj != null)
                {
                    virtualCamera.Follow = followObj.transform;
                }
                else
                {
                    Debug.LogWarning($"CinemachineTargetResolver: '{followTargetName}' が見つかりません");
                }
            }

            if (setLookAtTarget && !string.IsNullOrEmpty(lookAtTargetName))
            {
                GameObject lookAtObj = GameObject.Find(lookAtTargetName);
                if (lookAtObj != null)
                {
                    virtualCamera.LookAt = lookAtObj.transform;
                }
                else
                {
                    Debug.LogWarning($"CinemachineTargetResolver: '{lookAtTargetName}' が見つかりません");
                }
            }
        }
    }
}