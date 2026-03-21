using UnityEngine;
using UnityEngine.Rendering;

namespace JayT.UnityProductionUrpHelper
{
    /// <summary>
    /// Gameビューにセンター線やグリッドを描画するカメラオーバーレイ。
    /// 任意の GameObject にアタッチして使用します。
    /// ターゲットカメラを指定しない場合は Camera.main を使用します。
    /// [ExecuteAlways] により再生中・停止中どちらでも描画されます。
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("JayT/Game View Center Line")]
    public class GameViewCenterLine : MonoBehaviour
    {
        [Header("ターゲットカメラ")]
        [Tooltip("描画対象のカメラ。None の場合は Camera.main を使用します。")]
        [SerializeField] private Camera targetCamera;

        [Header("センター線")]
        [SerializeField] private bool showHorizontal = true;
        [SerializeField] private bool showVertical = true;
        [SerializeField] private Color centerLineColor = new Color(0f, 1f, 0f, 0.8f);

        [Header("三分割グリッド")]
        [SerializeField] private bool showRuleOfThirds = false;
        [SerializeField] private Color ruleOfThirdsColor = new Color(0f, 1f, 0f, 0.3f);

        [Header("セーフエリア")]
        [SerializeField] private bool showSafeArea = false;
        [Range(0.5f, 0.99f)]
        [SerializeField] private float safeAreaRatio = 0.9f;
        [SerializeField] private Color safeAreaColor = new Color(1f, 1f, 0f, 0.5f);

        private Material _lineMaterial;

        private void OnEnable()
        {
            EnsureLineMaterial();
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            if (_lineMaterial != null)
                DestroyImmediate(_lineMaterial);
            _lineMaterial = null;
        }

        private void EnsureLineMaterial()
        {
            if (_lineMaterial != null) return;

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;

            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
        }

        private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            var resolvedCamera = targetCamera ?? Camera.main;
            if (cam != resolvedCamera) return;

            EnsureLineMaterial();
            if (_lineMaterial == null) return;

            GL.PushMatrix();
            _lineMaterial.SetPass(0);
            GL.LoadOrtho();

            if (showRuleOfThirds)
                DrawRuleOfThirds();

            if (showSafeArea)
                DrawSafeArea();

            if (showHorizontal || showVertical)
                DrawCenterLines();

            GL.PopMatrix();
        }

        private void DrawCenterLines()
        {
            GL.Begin(GL.LINES);
            GL.Color(centerLineColor);

            if (showHorizontal)
            {
                GL.Vertex3(0f, 0.5f, 0f);
                GL.Vertex3(1f, 0.5f, 0f);
            }

            if (showVertical)
            {
                GL.Vertex3(0.5f, 0f, 0f);
                GL.Vertex3(0.5f, 1f, 0f);
            }

            GL.End();
        }

        private void DrawRuleOfThirds()
        {
            GL.Begin(GL.LINES);
            GL.Color(ruleOfThirdsColor);

            // 水平方向 1/3, 2/3
            GL.Vertex3(0f, 1f / 3f, 0f); GL.Vertex3(1f, 1f / 3f, 0f);
            GL.Vertex3(0f, 2f / 3f, 0f); GL.Vertex3(1f, 2f / 3f, 0f);

            // 垂直方向 1/3, 2/3
            GL.Vertex3(1f / 3f, 0f, 0f); GL.Vertex3(1f / 3f, 1f, 0f);
            GL.Vertex3(2f / 3f, 0f, 0f); GL.Vertex3(2f / 3f, 1f, 0f);

            GL.End();
        }

        private void DrawSafeArea()
        {
            float margin = (1f - safeAreaRatio) / 2f;
            float left   = margin;
            float right  = 1f - margin;
            float bottom = margin;
            float top    = 1f - margin;

            GL.Begin(GL.LINES);
            GL.Color(safeAreaColor);

            GL.Vertex3(left,  top,    0f); GL.Vertex3(right, top,    0f);
            GL.Vertex3(left,  bottom, 0f); GL.Vertex3(right, bottom, 0f);
            GL.Vertex3(left,  bottom, 0f); GL.Vertex3(left,  top,    0f);
            GL.Vertex3(right, bottom, 0f); GL.Vertex3(right, top,    0f);

            GL.End();
        }
    }
}
