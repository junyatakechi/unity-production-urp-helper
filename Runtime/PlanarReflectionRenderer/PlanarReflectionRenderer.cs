using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;

namespace JayT.UnityProductionUrpHelper
{
    /// <summary>
    /// URPには平面反射の標準機能がないため、
    /// 床へのアバターなどの鏡面反射像を手軽に表示できるようにするコンポーネント。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class PlanarReflectionRenderer : MonoBehaviour
    {
        [SerializeField] private Transform floorSurfaceTransform;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private LayerMask reflectionLayers;
        [SerializeField] private bool usePostProcessing = true;

        private Camera _mainCamera;
        private Camera _reflectionCamera;

        private void Start()
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
            {
                Debug.LogError("MainCamera が見つかりません。");
                return;
            }

            var go = new GameObject("ReflectionCamera");
            _reflectionCamera = go.AddComponent<Camera>();
            _reflectionCamera.enabled = false;

            var cameraData = go.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Base;
            cameraData.renderPostProcessing = usePostProcessing;

            _reflectionCamera.targetTexture = renderTexture;
            _reflectionCamera.cullingMask = reflectionLayers;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null || _reflectionCamera == null)
                return;

            UpdateReflectionCamera();
            RenderReflection();
        }

        private void UpdateReflectionCamera()
        {
            var floorY = floorSurfaceTransform != null ? floorSurfaceTransform.position.y : 0f;

            // Reflection matrix for horizontal plane y = floorY: maps (x,y,z) -> (x, 2f-y, z)
            var reflectionMatrix = Matrix4x4.identity;
            reflectionMatrix.m11 = -1f;
            reflectionMatrix.m13 = 2f * floorY;

            // Set view matrix directly: V_reflect = V_main * M_reflect
            // Correctly handles all camera orientations including Dutch angle (Z-roll)
            _reflectionCamera.worldToCameraMatrix = _mainCamera.worldToCameraMatrix * reflectionMatrix;

            // Negate Y in projection matrix to match shader's (1 - screenUV.y) correction
            // The two Y-negations cancel, producing correct winding order without GL.invertCulling
            var proj = _mainCamera.projectionMatrix;
            proj.m11 = -proj.m11;
            _reflectionCamera.projectionMatrix = proj;

            // Update transform position for Unity's culling frustum calculations
            _reflectionCamera.transform.position = reflectionMatrix.MultiplyPoint(_mainCamera.transform.position);
        }

        private void RenderReflection()
        {
            _reflectionCamera.Render();
        }

        private void OnDestroy()
        {
            if (_reflectionCamera != null)
                Destroy(_reflectionCamera.gameObject);
        }
    }
}
