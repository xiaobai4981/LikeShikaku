using UnityEngine;
using System.Collections.Generic;



namespace Core
{
    public class GameObjectPool
    {
        private struct Entry
        {
            public GameObject GameObject;

            // 记下回收的时间点，同一帧内不能再次使用，以避免GetComponent返回已经Destroy的Component
            // https://forum.unity.com/threads/getcomponent-can-return-a-deleted-component.1224210/
            // 另外还可以避免粒子、拖尾等残留的问题
            public float TimeOfRecycle;

            public Entry(GameObject go, float timeOfRecycle)
            {
                GameObject = go;
                TimeOfRecycle = timeOfRecycle;
            }
        }



        private GameObject m_prefabClass;
        public GameObject PrefabClass => m_prefabClass;

        private Transform m_root;

        private readonly List<Entry> m_freeObjects = new();

        // 实例数量限制，0表示无限
        private int m_maxInstances = 0;

        // 当前实例数量
        private int m_numInstances = 0;



        public void Init(GameObject prefabClass, GameObject root)
        {
            m_prefabClass = prefabClass;
            m_root = root.transform;
        }



        public void Cleanup()
        {
            foreach (Entry e in m_freeObjects)
            {
                Object.Destroy(e.GameObject);
            }
        }



        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            if (m_maxInstances > 0 && m_numInstances >= m_maxInstances)
            {
                return null;
            }
            ++m_numInstances;

            GameObject go = null;

            for (int i = m_freeObjects.Count - 1; i >= 0; --i)
            {
                Entry e = m_freeObjects[i];
                if (e.TimeOfRecycle != Time.frameCount)
                {
                    // 找到一个非同一帧销毁的
                    go = e.GameObject;
                    m_freeObjects.RemoveAt(i);
                    break;
                }
            }

            if (go == null)
            {
                go = CreateNewItem();
            }

            go.transform.SetParent(null);
            go.transform.SetLocalPositionAndRotation(position, rotation);
            go.SetActive(true);

            return go;
        }



        public void Recycle(GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.SetParent(m_root, false);

            Entry entry = new Entry(instance, Time.frameCount);
            m_freeObjects.Add(entry);

            --m_numInstances;
        }



        private GameObject CreateNewItem()
        {
            GameObject newGO = Object.Instantiate(m_prefabClass, Vector3.zero, Quaternion.identity, m_root);
            newGO.SetActive(false);
            return newGO;
        }



        public void SetMaxInstances(int newMaxInstances)
        {
            m_maxInstances = newMaxInstances;
        }
    }
}
