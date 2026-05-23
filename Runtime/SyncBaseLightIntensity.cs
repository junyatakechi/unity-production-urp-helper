using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
public class SyncBaseLightIntensity: MonoBehaviour
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
    // キャラクターごとではなくステージ中央を基準にする
    public Transform stageCenter;

    [Header("白ライト（地明かり）")]
    // ムービングライトが当たっていない時にキャラを照らす白いライト
    public Light baseLight;

    [Header("ムービングライト時の白ライト強度")]
    // ムービングライトが当たっている時に下げる輝度（0=完全に消す）
    public float dimmedIntensity = 0f;

    [Header("フェード速度")]
    // 下げる速度：速めにすることでカラーライトがすぐに際立つ
    public float fadeOutSpeed = 5.0f;
    // 戻す速度：遅めにすることでちらつきを抑える
    public float fadeInSpeed = 1.0f;

    [Header("戻り始めるまでの待機時間（秒）")]
    // ムービングライトが外れてもすぐに白ライトを戻さず待機する
    // 素早く通過するライトのちらつきを吸収するためのバッファ
    public float holdTime = 0.5f;

    [Header("ムービングライトのTag")]
    // このTagが付いたGameObjectのLightを自動取得する
    // UnityのTagsに「MovingLight」を追加してムービングライトに設定すること
    public string movingLightTag = "MovingLight";

    private float baseLightIntensity;  // 起動時の白ライト輝度を保存
    private float targetIntensity;     // 現在の目標輝度
    private Light[] movingLights;      // 取得したムービングライトの配列
    private float holdTimer = 0f;      // 戻り待機タイマー
    private bool isHit = false;        // ムービングライトが当たっているか

    void Start()
    {
        // 起動時のbaseLightのIntensityを通常輝度として記録
        baseLightIntensity = baseLight.intensity;
        RefreshMovingLights();
    }

    void RefreshMovingLights()
    {
        // 指定TagのGameObjectからLightコンポーネントを取得して配列に格納
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

        // いずれかのムービングライトがステージ中央に向いているか判定
        foreach (Light movingLight in movingLights)
        {
            if (movingLight == null || !movingLight.gameObject.activeInHierarchy) continue;

            if (IsLightHittingStage(movingLight))
            {
                currentHit = true;
                break;
            }
        }

        if (currentHit)
        {
            // 当たっている間はタイマーをリセットしてDimmed状態を維持
            isHit = true;
            holdTimer = holdTime;
        }
        else if (isHit)
        {
            // 外れたらholdTimeカウントダウン後にisHitを解除
            // これにより素早く通過するライトのちらつきを吸収する
            holdTimer -= Time.deltaTime;
            if (holdTimer <= 0f)
            {
                isHit = false;
            }
        }

        targetIntensity = isHit ? dimmedIntensity : baseLightIntensity;

        // 下げる時と戻す時で速度を変えてなめらかに制御
        float speed = baseLight.intensity > targetIntensity ? fadeOutSpeed : fadeInSpeed;
        baseLight.intensity = Mathf.Lerp(baseLight.intensity, targetIntensity, Time.deltaTime * speed);
    }

    bool IsLightHittingStage(Light spotlight)
    {
        // SpotLightの向きとステージ中央への方向が一致しているか判定
        Vector3 toStage = stageCenter.position - spotlight.transform.position;
        float distance = toStage.magnitude;

        // ライトの照射範囲外なら当たっていない
        if (distance > spotlight.range) return false;

        // ライト前方向とステージ中央への角度がSpotAngle以内なら当たっている
        float angle = Vector3.Angle(spotlight.transform.forward, toStage);
        return angle < spotlight.spotAngle / 2f;
    }
}
}