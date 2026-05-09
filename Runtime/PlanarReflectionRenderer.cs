using UnityEngine;
using UnityEngine.Rendering.Universal;

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
                Debug.LogError("MainCamera が見つかりません。MainCamera タグを確認してください。");
                return;
            }

            var go = new GameObject("ReflectionCamera");
            _reflectionCamera = go.AddComponent<Camera>();
            _reflectionCamera.enabled = false;

            var cameraData = go.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Base;

            _reflectionCamera.targetTexture = renderTexture;
        }

        private void LateUpdate()
        {
            UpdateReflectionCamera();
            RenderReflection();
        }

        private void UpdateReflectionCamera()
        {
            if (_mainCamera == null)
            {
                Debug.LogError("_mainCamera is null");
                return;
            }
            if (floorObject == null)
            {
                Debug.LogError("floorObject is null");
                return;
            }

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
            _reflectionCamera.Render();
        }

        private void OnDestroy()
        {
            if (_reflectionCamera != null)
                Destroy(_reflectionCamera.gameObject);
        }
    }
}