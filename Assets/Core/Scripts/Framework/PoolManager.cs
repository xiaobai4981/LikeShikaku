using UnityEngine;
using System.Collections.Generic;

namespace Core
{
    public class PoolManager
    {
        private static PoolManager s_instance = null;
        public static PoolManager Instance => s_instance;


        private Transform m_root;


        // 不同类型GameObject的对象池
        private readonly Dictionary<GameObject, GameObjectPool> m_gameObjectPools = new();
        // 已经分配出去的对象到它所属的对象池的映射
        private readonly Dictionary<GameObject, GameObjectPool> m_spawnedGameObjects = new();



        public void Init()
        {
            s_instance = this;

            GameObject rootGO = new("$PoolManager");
            m_root = rootGO.transform;
            rootGO.SetActive(false);
        }



        public void Cleanup()
        {
            foreach (var kv in m_spawnedGameObjects)
            {
                GameObject go = kv.Key;
                GameObjectPool pool = kv.Value;
                if (go == null)
                {
                    Debug.LogError($"对象池管理的对象被外部销毁了，类型为{pool.PrefabClass}");
                }
                else
                {
                    Debug.LogError($"对象池分配的对象未回收：{go}", go);
                }
            }
            foreach (GameObjectPool pool in m_gameObjectPools.Values)
            {
                pool.Cleanup();
            }

            s_instance = null;
        }



        /// <summary>
        /// 设置实例数量上限
        /// </summary>
        public void SetGameObjectInstanceLimit(GameObject prefabClass, int maxInstances)
        {
            GameObjectPool pool = GetOrCreateGameObjectPool(prefabClass);
            pool.SetMaxInstances(maxInstances);
        }



        public GameObject GetGameObject(GameObject prefabClass, Vector3 position, Quaternion rotation)
        {
            GameObjectPool pool = GetOrCreateGameObjectPool(prefabClass);
            GameObject go = pool.Get(position, rotation);
            if (go == null)
            {
                return null;
            }

            m_spawnedGameObjects.Add(go, pool);
            return go;
        }



        public bool ReleaseGameObject(GameObject instance)
        {
            GameObjectPool pool;
            if (!m_spawnedGameObjects.TryGetValue(instance, out pool))
            {
                Debug.LogError($"要回收的对象不受对象池管理：{instance}", instance);
                return false;
            }

            m_spawnedGameObjects.Remove(instance);
            pool.Recycle(instance);

            return true;
        }



        private GameObjectPool GetOrCreateGameObjectPool(GameObject prefabClass)
        {
            GameObjectPool pool;
            if (m_gameObjectPools.TryGetValue(prefabClass, out pool))
            {
                return pool;
            }

            GameObject newPoolGO = new(prefabClass.name);
            newPoolGO.transform.SetParent(m_root, false);

            pool = new();
            pool.Init(prefabClass, newPoolGO);
            m_gameObjectPools.Add(prefabClass, pool);

            return pool;
        }
    }
}
