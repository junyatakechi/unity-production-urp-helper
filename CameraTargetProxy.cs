using UnityEngine;


[ExecuteAlways]
public class CameraTargetProxy : MonoBehaviour
{
    public Transform targetPoint;

    void LateUpdate()
    {
        transform.position = targetPoint.position;
    }
}
