using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
public class SyncBaseLightIntensity : MonoBehaviour
{
    // ---------------------------------------------------------------
    // 設計方針
    // ---------------------------------------------------------------
    // コンサート照明の基本原則：
    //   「暗い空間をベースに、光で演出する」
    //
    // 白ライト（地明かり）とカラーライト（ムービングライト）は加算合成のため、
    //   白(1,1,1) + 赤(1,0,0) = (2,1,1) → クランプ → 白のまま
    // となり、白が強いとカラーが乗らない。
    //
    // そのため：
    //   ムービングライトがステージに当たっていない → 白ライトを通常輝度に保つ
    //   ムービングライトがステージに当たっている   → 白ライトを下げてカラーを際立たせる
    //
    // キャラクターごとに白ライトを用意すると複雑になるため、
    // ステージ中央を基準点として白ライト1本で全体を制御する。
    // ---------------------------------------------------------------

    [Header("ステージ中央のTransform")]
    public Transform stageCenter;

    [Header("ステージ判定半径")]
    // 光線がこの半径内を通過すれば「当たっている」と判定する
    public float stageRadius = 2.0f;

    [Header("白ライト（地明かり）")]
    public Light baseLight;

    [Header("ムービングライト時の白ライト強度")]
    // ムービングライトが当たっている時に下げる輝度（0=完全に消す）
    public float dimmedIntensity = 0f;

    [Header("なめらかさ（秒）")]
    // 値が大きいほどゆっくり変化する。ちらつきが気になる場合は増やす
    public float smoothTime = 0.5f;

    [Header("ムービングライトのTag")]
    public string movingLightTag = "MovingLight";

    private float baseLightIntensity;
    private float currentVelocity = 0f;  // SmoothDamp用の内部速度
    private Light[] movingLights;

    void Start()
    {
        baseLightIntensity = baseLight.intensity;
        RefreshMovingLights();
    }

    void RefreshMovingLights()
    {
        var gameObjects = GameObject.FindGameObjectsWithTag(movingLightTag);
        var lights = new System.Collections.Generic.List<Light>();
        foreach (var go in gameObjects)
        {
            var light = go.GetComponent<Light>();
            if (light != null) lights.Add(light);
        }
        movingLights = lights.ToArray();
    }

    void Update()
    {
        bool currentHit = false;

        foreach (Light movingLight in movingLights)
        {
            if (movingLight == null || !movingLight.gameObject.activeInHierarchy) continue;

            if (IsLightHittingStage(movingLight))
            {
                currentHit = true;
                break;
            }
        }

        float targetIntensity = currentHit ? dimmedIntensity : baseLightIntensity;

        // SmoothDampで出も戻りもなめらかに制御
        // smoothTimeが大きいほどゆっくり変化し、ちらつきを自然に吸収する
        baseLight.intensity = Mathf.SmoothDamp(
            baseLight.intensity,
            targetIntensity,
            ref currentVelocity,
            smoothTime
        );
    }

    bool IsLightHittingStage(Light spotlight)
    {
        Vector3 lightPos = spotlight.transform.position;
        Vector3 lightDir = spotlight.transform.forward;
        Vector3 toStage = stageCenter.position - lightPos;

        if (toStage.magnitude > spotlight.range) return false;

        float t = Vector3.Dot(toStage, lightDir);
        if (t < 0f) return false;

        Vector3 closest = lightPos + lightDir * t;
        float distToCenter = Vector3.Distance(closest, stageCenter.position);
        return distToCenter <= stageRadius;
    }
}
}