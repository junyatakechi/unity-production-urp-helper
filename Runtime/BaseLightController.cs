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

    [Header("ムービングライト")]
    public Light[] movingLights;

    private float baseLightIntensity;
    private float targetIntensity;

    void Start()
    {
        baseLightIntensity = baseLight.intensity;
    }

    void Update()
    {
        targetIntensity = baseLightIntensity;

        foreach (Light movingLight in movingLights)
        {
            if (movingLight == null || !movingLight.gameObject.activeInHierarchy) continue;

            if (IsLightHittingCharacter(movingLight))
            {
                targetIntensity = dimmedIntensity;
                break;
            }
        }

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