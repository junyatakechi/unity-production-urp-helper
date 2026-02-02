using UnityEngine;
using Cinemachine;

namespace JayT.UnityProductionUrpHelper
{
    /// <summary>
    /// CinemachineVirtualCameraのPositionの各軸に倍率を適用するExtension
    /// </summary>
    [SaveDuringPlay]
    [AddComponentMenu("")] // Hide in menu, add via Inspector
    public class CinemachineAxisScaler : CinemachineExtension
    {
        [Header("Position Scale")]
        [SerializeField]
        private float positionXScale = 1f;

        [SerializeField]
        private float positionYScale = 1f;

        [SerializeField]
        private float positionZScale = 1f;

        private Vector3 basePosition;
        private bool isInitialized = false;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, 
            ref CameraState state, 
            float deltaTime)
        {
            // Body処理の直後に基準位置を記録
            if (stage == CinemachineCore.Stage.Body && !isInitialized)
            {
                basePosition = state.RawPosition;
                isInitialized = true;
            }

            // 最終段階で倍率を適用
            if (stage == CinemachineCore.Stage.Finalize)
            {
                // Position - 基準位置からのオフセットに倍率を適用
                Vector3 currentPos = state.RawPosition;
                Vector3 offset = currentPos - basePosition;

                Vector3 scaledPos = basePosition + new Vector3(
                    offset.x * positionXScale,
                    offset.y * positionYScale,
                    offset.z * positionZScale
                );

                state.RawPosition = scaledPos;
            }
        }

        /// <summary>
        /// 基準位置をリセット
        /// </summary>
        [ContextMenu("Reset Base Position")]
        public void ResetBasePosition()
        {
            isInitialized = false;
        }
    }
}