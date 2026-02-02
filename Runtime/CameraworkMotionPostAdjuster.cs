using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
    /// <summary>
    /// TransformのPosition/Rotationの各軸を固定値に維持するコンポーネント
    /// </summary>
    public class CameraworkMotionPostAdjuster : MonoBehaviour
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

        [SerializeField]
        private bool lockPositionOnStart = true;

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

        [SerializeField]
        private bool lockRotationOnStart = true;

        private void Start()
        {
            if (lockPositionOnStart)
            {
                Vector3 pos = transform.position;
                fixedXPosition = pos.x;
                fixedYPosition = pos.y;
                fixedZPosition = pos.z;
            }

            if (lockRotationOnStart)
            {
                Vector3 rot = transform.eulerAngles;
                fixedXRotation = rot.x;
                fixedYRotation = rot.y;
                fixedZRotation = rot.z;
            }
        }

        private void LateUpdate()
        {
            // Position
            Vector3 pos = transform.position;

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

            transform.position = pos;

            // Rotation
            Vector3 rot = transform.eulerAngles;

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

            transform.eulerAngles = rot;
        }

        /// <summary>
        /// 固定するX座標を設定
        /// </summary>
        public void SetFixedX(float x)
        {
            fixedXPosition = x;
        }

        /// <summary>
        /// 固定するY座標を設定
        /// </summary>
        public void SetFixedY(float y)
        {
            fixedYPosition = y;
        }

        /// <summary>
        /// 固定するZ座標を設定
        /// </summary>
        public void SetFixedZ(float z)
        {
            fixedZPosition = z;
        }

        /// <summary>
        /// 固定するX回転を設定
        /// </summary>
        public void SetFixedRotationX(float x)
        {
            fixedXRotation = x;
        }

        /// <summary>
        /// 固定するY回転を設定
        /// </summary>
        public void SetFixedRotationY(float y)
        {
            fixedYRotation = y;
        }

        /// <summary>
        /// 固定するZ回転を設定
        /// </summary>
        public void SetFixedRotationZ(float z)
        {
            fixedZRotation = z;
        }

        /// <summary>
        /// 現在の座標を固定値として設定
        /// </summary>
        public void LockCurrentPosition()
        {
            Vector3 pos = transform.position;
            fixedXPosition = pos.x;
            fixedYPosition = pos.y;
            fixedZPosition = pos.z;
        }

        /// <summary>
        /// 現在の回転を固定値として設定
        /// </summary>
        public void LockCurrentRotation()
        {
            Vector3 rot = transform.eulerAngles;
            fixedXRotation = rot.x;
            fixedYRotation = rot.y;
            fixedZRotation = rot.z;
        }
    }
}