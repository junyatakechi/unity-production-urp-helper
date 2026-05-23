using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
public class BaseLightController : MonoBehaviour
{
    [Header("白ライト（地明かり）")]
    public Light baseLight;

    [Header("ムービングライト時の白ライト強度")]
    public float dimmedIntensity = 0f;

    [Header("フェード速度")]
    public float fadeInSpeed = 1.0f;
    public float fadeOutSpeed = 5.0f;

    [Header("戻り始めるまでの待機時間")]
    public float holdTime = 0.5f;

    [Header("ムービングライトのTag")]
    public string movingLightTag = "MovingLight";

    private float baseLightIntensity;
    private float targetIntensity;
    private Light[] movingLights;
    private float holdTimer = 0f;
    private bool isHit = false;

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

            if (IsLightHittingCharacter(movingLight))
            {
                currentHit = true;
                break;
            }
        }

        if (currentHit)
        {
            // 当たっている間はタイマーをリセットしてDimmed維持
            isHit = true;
            holdTimer = holdTime;
        }
        else if (isHit)
        {
            // 外れたらタイマーをカウントダウン
            holdTimer -= Time.deltaTime;
            if (holdTimer <= 0f)
            {
                isHit = false;
            }
        }

        targetIntensity = isHit ? dimmedIntensity : baseLightIntensity;

        float speed = baseLight.intensity > targetIntensity ? fadeOutSpeed : fadeInSpeed;
        baseLight.intensity = Mathf.Lerp(baseLight.intensity, targetIntensity, Time.deltaTime * speed);
    }

    bool IsLightHittingCharacter(Light spotlight)
    {
        Vector3 toCharacter = transform.position - spotlight.transform.position;
        float distance = toCharacter.magnitude;

        if (distance > spotlight.range) return false;

        float angle = Vector3.Angle(spotlight.transform.forward, toCharacter);
        return angle < spotlight.spotAngle / 2f;
    }
}
}