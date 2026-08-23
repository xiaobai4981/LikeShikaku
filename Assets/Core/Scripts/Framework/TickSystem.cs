namespace Core
{
    public class TickSystem
    {
        private SparseVector<ITickable> m_tickables = new();
        private SparseVector<IFixedTickable> m_fixedTickables = new();
        private SparseVector<ILateTickable> m_lateTickable = new();



        public void Tick()
        {
            foreach (ITickable tickable in m_tickables)
            {
                tickable.Tick();
            }
        }



        public void FixedTick()
        {
            foreach (IFixedTickable fixedTickable in m_fixedTickables)
            {
                fixedTickable.FixedTick();
            }
        }



        public void LateTick()
        {
            foreach (ILateTickable lateTickable in m_lateTickable)
            {
                lateTickable.LateTick();
            }
        }



        public Handle<ITickable> RegisterTick(ITickable tickable)
        {
            var handle = m_tickables.AllocHandle();
            m_tickables.Set(handle, tickable);
            return handle;
        }



        public void RegisterTickSafe(ITickable tickable, ref Handle<ITickable> handle)
        {
            if (!handle.IsEmpty())
            {
                return;
            }

            handle = RegisterTick(tickable);
        }



        public void UnregisterTickAndClearHandle(ref Handle<ITickable> handle)
        {
            m_tickables.RemoveAndClearHandle(ref handle);
        }



        public Handle<IFixedTickable> RegisterFixedTick(IFixedTickable fixedTickable)
        {
            var handle = m_fixedTickables.AllocHandle();
            m_fixedTickables.Set(handle, fixedTickable);
            return handle;
        }



        public void UnregisterFixedTickAndClearHandle(ref Handle<IFixedTickable> handle)
        {
            m_fixedTickables.RemoveAndClearHandle(ref handle);
        }



        public Handle<ILateTickable> RegisterLateTick(ILateTickable lateTickable)
        {
            var handle = m_lateTickable.AllocHandle();
            m_lateTickable.Set(handle, lateTickable);
            return handle;
        }



        public void UnregisterLateTickAndClearHandle(ref Handle<ILateTickable> handle)
        {
            m_lateTickable.RemoveAndClearHandle(ref handle);
        }
    }
}
