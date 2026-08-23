using System.Collections.Generic;

namespace Core
{
    public class QueuePool<T>
    {
        private static readonly UnityEngine.Pool.ObjectPool<Queue<T>> s_pool = new(
            () => new(),
            null,
            delegate (Queue<T> l)
            {
                l.Clear();
            });

        public static Queue<T> Get()
        {
            return s_pool.Get();
        }

        public static UnityEngine.Pool.PooledObject<Queue<T>> Get(out Queue<T> value)
        {
            return s_pool.Get(out value);
        }

        public static void Release(Queue<T> toRelease)
        {
            s_pool.Release(toRelease);
        }
    }
}
