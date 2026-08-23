using System;
using UnityEngine;



namespace Core
{
    public class FollowTransform : MonoBehaviour
    {
        private Transform m_target;
        public event Action OnTargetLost;



        public void SetTarget(Transform target)
        {
            m_target = target;
        }



        private void Update()
        {
            if (m_target != null)
            {
                transform.position = m_target.position;
            }
            else
            {
                OnTargetLost?.Invoke();
            }
        }
    }
}
