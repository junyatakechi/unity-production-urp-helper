using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;

namespace JayT.UnityProductionUrpHelper
{
    [DefaultExecutionOrder(1000)]
    public class PlanarReflectionRenderer : MonoBehaviour
    {
        [SerializeField] private Transform floorObject;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private LayerMask reflectionLayers;
        [SerializeField] private float rotationXOffset = 0f;
        [SerializeField] private float fovMultiplier = 1f;
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
            if (_mainCamera == null || _reflectionCamera == null || floorObject == null)
                return;

            UpdateReflectionCamera();
            RenderReflection();
        }

        private void UpdateReflectionCamera()
        {
            float floorY = floorObject.position.y;
            Vector3 mainPos = _mainCamera.transform.position;

            Vector3 reflectPos = mainPos;
            reflectPos.y = 2f * floorY - mainPos.y;
            _reflectionCamera.transform.position = reflectPos;

            Vector3 normal = Vector3.up;
            Vector3 reflectedForward = Vector3.Reflect(_mainCamera.transform.forward, normal);
            Quaternion reflectedRotation = Quaternion.LookRotation(reflectedForward, Vector3.up);

            _reflectionCamera.transform.rotation = reflectedRotation * Quaternion.Euler(rotationXOffset, 0f, 0f);

            _reflectionCamera.fieldOfView = _mainCamera.fieldOfView * fovMultiplier;
            _reflectionCamera.nearClipPlane = _mainCamera.nearClipPlane;
            _reflectionCamera.farClipPlane = _mainCamera.farClipPlane;
            _reflectionCamera.aspect = _mainCamera.aspect;
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