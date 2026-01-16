using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
    public class TransformProxy : MonoBehaviour
    {
        public Transform point;

        void LateUpdate()
        {
            transform.position = point.position;
        }
}

}
