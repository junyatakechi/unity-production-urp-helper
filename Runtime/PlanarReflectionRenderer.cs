using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;

namespace JayT.UnityProductionUrpHelper
{
    public class PlanarReflectionRenderer : MonoBehaviour
    {
        [SerializeField] private Transform floorObject;
        [SerializeField] private RenderTexture renderTexture;

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

            _reflectionCamera.targetTexture = renderTexture;

            CinemachineCore.CameraUpdatedEvent.AddListener(OnCinemachineCameraUpdated);
        }

        private void OnCinemachineCameraUpdated(CinemachineBrain brain)
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

            Vector3 mainEuler = _mainCamera.transform.eulerAngles;
            _reflectionCamera.transform.eulerAngles = new Vector3(
                -mainEuler.x,
                mainEuler.y,
                mainEuler.z
            );

            _reflectionCamera.fieldOfView = _mainCamera.fieldOfView;
            _reflectionCamera.nearClipPlane = _mainCamera.nearClipPlane;
            _reflectionCamera.farClipPlane = _mainCamera.farClipPlane;
        }

        private void RenderReflection()
        {
            GL.invertCulling = true;
            _reflectionCamera.Render();
            GL.invertCulling = false;
        }

        private void OnDestroy()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCinemachineCameraUpdated);

            if (_reflectionCamera != null)
                Destroy(_reflectionCamera.gameObject);
        }
    }
}