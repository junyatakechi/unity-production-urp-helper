using UnityEngine;
using Cinemachine;

namespace JayT.UnityProductionUrpHelper
{
    /// <summary>
    /// CinemachineVirtualCameraのPosition/Rotationの各軸を固定値に維持するExtension
    /// </summary>
    [SaveDuringPlay]
    [AddComponentMenu("")] // Hide in menu, add via Inspector
    public class CinemachineAxisLocker : CinemachineExtension
    {
        [Header("Position Lock")]
        [SerializeField]
        private bool lockPositionX = false;

        [SerializeField]
        private bool lockPositionY = false;

        [SerializeField]
        private bool lockPositionZ = false;

        [SerializeField]
        private float fixedXPosition = 0f;

        [SerializeField]
        private float fixedYPosition = 0f;

        [SerializeField]
        private float fixedZPosition = 0f;

        [Header("Rotation Lock")]
        [SerializeField]
        private bool lockRotationX = false;

        [SerializeField]
        private bool lockRotationY = false;

        [SerializeField]
        private bool lockRotationZ = false;

        [SerializeField]
        private float fixedXRotation = 0f;

        [SerializeField]
        private float fixedYRotation = 0f;

        [SerializeField]
        private float fixedZRotation = 0f;

        private void Start()
        {
            // 再生開始時にGameObjectのtransform値を取得
            SyncFromTransform();
        }

        private void OnValidate()
        {
            // Inspector上で値が変更されたらGameObjectのtransformに反映
            SyncToTransform();
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, 
            ref CameraState state, 
            float deltaTime)
        {
            // Aimステージの後に適用
            if (stage == CinemachineCore.Stage.Finalize)
            {
                // Position
                Vector3 pos = state.RawPosition;

                if (lockPositionX)
                {
                    pos.x = fixedXPosition;
                }

                if (lockPositionY)
                {
                    pos.y = fixedYPosition;
                }

                if (lockPositionZ)
                {
                    pos.z = fixedZPosition;
                }

                state.RawPosition = pos;

                // Rotation
                Vector3 rot = state.RawOrientation.eulerAngles;

                if (lockRotationX)
                {
                    rot.x = fixedXRotation;
                }

                if (lockRotationY)
                {
                    rot.y = fixedYRotation;
                }

                if (lockRotationZ)
                {
                    rot.z = fixedZRotation;
                }

                state.RawOrientation = Quaternion.Euler(rot);
            }
        }

        /// <summary>
        /// GameObjectのtransformから値を取得
        /// </summary>
        private void SyncFromTransform()
        {
            Vector3 pos = transform.position;
            fixedXPosition = pos.x;
            fixedYPosition = pos.y;
            fixedZPosition = pos.z;

            Vector3 rot = transform.eulerAngles;
            fixedXRotation = rot.x;
            fixedYRotation = rot.y;
            fixedZRotation = rot.z;
        }

        /// <summary>
        /// fixedの値をGameObjectのtransformに反映
        /// </summary>
        private void SyncToTransform()
        {
            Vector3 pos = transform.position;
            pos.x = fixedXPosition;
            pos.y = fixedYPosition;
            pos.z = fixedZPosition;
            transform.position = pos;

            Vector3 rot = transform.eulerAngles;
            rot.x = fixedXRotation;
            rot.y = fixedYRotation;
            rot.z = fixedZRotation;
            transform.eulerAngles = rot;
        }

        /// <summary>
        /// 現在の座標を固定値として設定
        /// </summary>
        [ContextMenu("Set Current Position as Fixed")]
        public void SetCurrentPositionAsFixed()
        {
            Vector3 pos = transform.position;
            fixedXPosition = pos.x;
            fixedYPosition = pos.y;
            fixedZPosition = pos.z;
        }

        /// <summary>
        /// 現在の回転を固定値として設定
        /// </summary>
        [ContextMenu("Set Current Rotation as Fixed")]
        public void SetCurrentRotationAsFixed()
        {
            Vector3 rot = transform.eulerAngles;
            fixedXRotation = rot.x;
            fixedYRotation = rot.y;
            fixedZRotation = rot.z;
        }
    }
}