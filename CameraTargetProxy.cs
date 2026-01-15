using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
    [ExecuteAlways]
    public class CameraTargetProxy : MonoBehaviour
    {
        public Transform targetPoint;

        void LateUpdate()
        {
            transform.position = targetPoint.position;
        }
}

}
