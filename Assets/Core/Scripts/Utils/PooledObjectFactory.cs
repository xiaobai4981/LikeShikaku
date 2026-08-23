using System.Collections.Generic;

namespace Core
{
    public interface IPooledObjectFactory<TBase>
        where TBase : class
    {
        TBase Create();
        void Destroy(TBase controller);
    }

    public class PooledObjectFactory<TDerived, TBase> : IPooledObjectFactory<TBase>
        where TBase : class
        where TDerived : class, TBase, new()
    {
        private readonly UnityEngine.Pool.ObjectPool<TDerived> m_pool;

        public PooledObjectFactory()
        {
            m_pool = new(() => {
                return new TDerived();
            });
        }

        TBase IPooledObjectFactory<TBase>.Create()
        {
            return m_pool.Get();
        }

        void IPooledObjectFactory<TBase>.Destroy(TBase instance)
        {
            m_pool.Release((TDerived)instance);
        }
    }

    public class PooledObjectFactoryCollection<TBase>
        where TBase : class
    {
        private Dictionary<System.Type, IPooledObjectFactory<TBase>> m_factories = new();

        public void AddFactory<TDerived>() where TDerived : class, TBase, new()
        {
            m_factories[typeof(TDerived)] = new PooledObjectFactory<TDerived, TBase>();
        }

        public TBase Create(System.Type type)
        {
            return m_factories[type].Create();
        }

        public TDerived Create<TDerived>() where TDerived : TBase
        {
            return (TDerived)Create(typeof(TDerived));
        }

        public void Destroy(TBase instance)
        {
            m_factories[instance.GetType()].Destroy(instance);
        }
    }
}
