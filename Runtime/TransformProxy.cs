using System;
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

        [Header("Copy Position")]
        [SerializeField] private bool copyPositionX = true;
        [SerializeField] private bool copyPositionY = true;
        [SerializeField] private bool copyPositionZ = true;

        [Header("Copy Rotation")]
        [SerializeField] private bool copyRotationX = true;
        [SerializeField] private bool copyRotationY = true;
        [SerializeField] private bool copyRotationZ = true;

        [Header("Relative Position Movement")]
        [SerializeField] private bool useRelativePositionMovement = false;
        [SerializeField] private Vector3 relativeBasePosition;
        [SerializeField] private Vector3 relativeBaseRotation;
        [SerializeField] private Vector3 relativePositionScale = Vector3.one;

        private Transform targetTransform;
        private Vector3 previousTargetPosition;
        private Vector3 accumulatedOffset;
        private bool isFirstFrame = true;
        private Action applyMethod;

        private void Start()
        {
            ResolveTarget();
            applyMethod = useRelativePositionMovement ? ApplyRelative : ApplyAbsolute;
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
                            targetTransform = targetObj.transform;
                        else
                            Debug.LogWarning($"TransformProxy: '{targetName}' が見つかりません");
                    }
                    break;

                case SearchMethod.ByTag:
                    if (!string.IsNullOrEmpty(targetTag))
                    {
                        GameObject targetObj = GameObject.FindGameObjectWithTag(targetTag);
                        if (targetObj != null)
                            targetTransform = targetObj.transform;
                        else
                            Debug.LogWarning($"TransformProxy: タグ '{targetTag}' が見つかりません");
                    }
                    break;
            }
        }

        private void LateUpdate()
        {
            if (targetTransform == null)
                return;

            applyMethod();
        }

        private void ApplyAbsolute()
        {
            Vector3 targetPos = targetTransform.position;
            transform.position = new Vector3(
                copyPositionX ? targetPos.x : 0f,
                copyPositionY ? targetPos.y : 0f,
                copyPositionZ ? targetPos.z : 0f
            );

            Vector3 targetEuler = targetTransform.eulerAngles;
            transform.eulerAngles = new Vector3(
                copyRotationX ? targetEuler.x : 0f,
                copyRotationY ? targetEuler.y : 0f,
                copyRotationZ ? targetEuler.z : 0f
            );
        }

        private void ApplyRelative()
        {
            Vector3 targetPos = targetTransform.position;
            if (isFirstFrame)
            {
                previousTargetPosition = targetPos;
                accumulatedOffset = Vector3.zero;
                isFirstFrame = false;
            }
            else
            {
                accumulatedOffset += Vector3.Scale(
                    targetPos - previousTargetPosition,
                    relativePositionScale
                );
                previousTargetPosition = targetPos;
            }

            transform.position = new Vector3(
                relativeBasePosition.x + (copyPositionX ? accumulatedOffset.x : 0f),
                relativeBasePosition.y + (copyPositionY ? accumulatedOffset.y : 0f),
                relativeBasePosition.z + (copyPositionZ ? accumulatedOffset.z : 0f)
            );

            Vector3 targetEuler = targetTransform.eulerAngles;
            transform.eulerAngles = new Vector3(
                relativeBaseRotation.x + (copyRotationX ? targetEuler.x : 0f),
                relativeBaseRotation.y + (copyRotationY ? targetEuler.y : 0f),
                relativeBaseRotation.z + (copyRotationZ ? targetEuler.z : 0f)
            );
        }
    }
}
