using System;
using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
    public class TransformProxy : MonoBehaviour
    {
        public enum SearchMethod
        {
            DirectReference,
            ByName
        }

        [SerializeField] private SearchMethod searchMethod = SearchMethod.ByName;

        [SerializeField] private Transform point;
        [SerializeField] private string targetName;

        public enum MovingCopyMode { Absolute, Relative }
        [SerializeField] private MovingCopyMode movingCopyMode = MovingCopyMode.Absolute;

        [SerializeField] private bool copyPositionX = true;
        [SerializeField] private bool copyPositionY = true;
        [SerializeField] private bool copyPositionZ = true;
        [SerializeField] private Vector3 positionOffset;

        [SerializeField] private bool copyRotationX = true;
        [SerializeField] private bool copyRotationY = true;
        [SerializeField] private bool copyRotationZ = true;
        [SerializeField] private Vector3 rotationOffset;

        [SerializeField] private Vector3 relativePositionScale = Vector3.one;
        [SerializeField] private Vector3 relativeRotationScale = Vector3.one;

        private Transform targetTransform;
        private Vector3 previousTargetPosition;
        private Vector3 accumulatedOffset;
        private bool isFirstFrame = true;
        private Action applyMethod;

        private void Start()
        {
            ResolveTarget();
            applyMethod = movingCopyMode == MovingCopyMode.Relative ? ApplyRelative : ApplyAbsolute;
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

            }
        }

        private void LateUpdate()
        {
            if (targetTransform == null)
                return;

            applyMethod();
        }

        // モード別の挙動:
        // | モード   | Position                    | Rotation                    |
        // | Absolute | offset + target値           | offset + target値           |
        // | Relative | offset + 変動累積値 * scale | offset + target値 * scale   |

        private void ApplyAbsolute()
        {
            Vector3 targetPos = targetTransform.position;
            transform.position = new Vector3(
                positionOffset.x + (copyPositionX ? targetPos.x : 0f),
                positionOffset.y + (copyPositionY ? targetPos.y : 0f),
                positionOffset.z + (copyPositionZ ? targetPos.z : 0f)
            );

            Vector3 targetEuler = targetTransform.eulerAngles;
            transform.eulerAngles = new Vector3(
                rotationOffset.x + (copyRotationX ? targetEuler.x : 0f),
                rotationOffset.y + (copyRotationY ? targetEuler.y : 0f),
                rotationOffset.z + (copyRotationZ ? targetEuler.z : 0f)
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
                positionOffset.x + (copyPositionX ? accumulatedOffset.x : 0f),
                positionOffset.y + (copyPositionY ? accumulatedOffset.y : 0f),
                positionOffset.z + (copyPositionZ ? accumulatedOffset.z : 0f)
            );

            Vector3 targetEuler = targetTransform.eulerAngles;
            transform.eulerAngles = new Vector3(
                rotationOffset.x + (copyRotationX ? targetEuler.x * relativeRotationScale.x : 0f),
                rotationOffset.y + (copyRotationY ? targetEuler.y * relativeRotationScale.y : 0f),
                rotationOffset.z + (copyRotationZ ? targetEuler.z * relativeRotationScale.z : 0f)
            );
        }
    }
}
