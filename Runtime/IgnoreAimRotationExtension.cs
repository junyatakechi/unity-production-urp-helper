using Cinemachine;
using UnityEngine;

/// <summary>
/// CinemachineのAim（LookAt）による回転調整を軸ごとに無視するExtension。
/// 有効にした軸は、Aimステージの調整を無視してvcam GameObjectの元のtransform回転をスループットします。
/// </summary>
[ExecuteAlways]
[AddComponentMenu("Cinemachine/Extensions/Ignore Aim Rotation")]
[SaveDuringPlay]
public class IgnoreAimRotationExtension : CinemachineExtension
{
    [Tooltip("X軸（ピッチ）のAimを無視")]
    public bool X;

    [Tooltip("Y軸（ヨー）のAimを無視")]
    public bool Y;

    [Tooltip("Z軸（ロール）のAimを無視")]
    public bool Z;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Aim)
            return;

        if (!X && !Y && !Z)
            return;

        // パイプライン実行中はCinemachineがまだvcam.transformを更新していないため、
        // これがAim処理前の生のGameObjectトランスフォーム回転になる
        var originalEuler = vcam.transform.eulerAngles;
        var aimEuler = state.RawOrientation.eulerAngles;

        float x = X ? originalEuler.x : aimEuler.x;
        float y = Y ? originalEuler.y : aimEuler.y;
        float z = Z ? originalEuler.z : aimEuler.z;

        state.RawOrientation = Quaternion.Euler(x, y, z);
    }
}
