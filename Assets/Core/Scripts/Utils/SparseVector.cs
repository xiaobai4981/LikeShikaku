using System.Collections.Generic;


namespace Core
{
    public readonly struct Handle<T> :
        System.IEquatable<Handle<T>>,
        System.IComparable<Handle<T>>
    {
        private readonly ulong m_value;

        public static Handle<T> Empty
        {
            get => new();
        }

        public Handle(ulong value)
        {
            m_value = value;
        }

        public ulong Value
        {
            get => m_value;
        }

        public bool IsEmpty()
        {
            return m_value == 0;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(m_value);
        }

        // 实现此接口，防止EqualityComparer<T>.Default调用object版本的Equals导致boxing
        public bool Equals(Handle<T> other)
        {
            return m_value == other.m_value;
        }

        public int CompareTo(Handle<T> other)
        {
            return m_value.CompareTo(other.m_value);
        }
    }



    /**
     * 插入、删除、查询均为O(1)的容器
     */
    public class SparseVector<T>
    {
        internal struct Slot
        {
            public uint version;
            public T data;
        }



        public struct Enumerator
        {
            private readonly List<Slot> m_slots;
            private int m_index;



            internal Enumerator(List<Slot> slots)
            {
                m_slots = slots;
                m_index = -1;
            }



            public readonly T Current
            {
                get => m_slots[m_index].data;
            }



            public bool MoveNext()
            {
                ++m_index;
                while (m_index < m_slots.Count)
                {
                    Slot slot = m_slots[m_index];
                    if (slot.version != 0)
                    {
                        return true;
                    }
                    ++m_index;
                }

                return false;
            }



            public void Reset()
            {
                m_index = -1;
            }
        }



        const int TOTAL_BITS = 31; // 句柄共使用的位数（留1位符号位，否则js中会变负数）
        const int INDEX_BITS = 20; // 句柄中下标的位数
        const int VERSION_BITS = TOTAL_BITS - INDEX_BITS; // 句柄中版本号的位数

        const ulong INDEX_MASK = (1 << INDEX_BITS) - 1; // 从句柄获取下标的mask

        const uint MAX_VERSION = (1 << VERSION_BITS) - 1; // 最大版本号，受限于version的位数
        const int MAX_CAPACITY = (1 << INDEX_BITS); // 最大容量，受限于index的位数



        private uint m_nextVersion = 1; // 版本号生成器，0为无效版本号
        private readonly List<Slot> m_slotStorage = new List<Slot>(); // 所有槽位
        private readonly List<int> m_freeIndices = new List<int>(); // 所有空位下标
        private int m_size = 0;



        /**
         * 分配一个句柄
         * @return 元素的句柄，0-失败，>0-有效句柄
         */
        public Handle<T> AllocHandle()
        {
            int index;

            if (m_freeIndices.Count > 0)
            { // 如果有空位则用空位
                index = m_freeIndices[m_freeIndices.Count - 1];
                m_freeIndices.RemoveAt(m_freeIndices.Count - 1);
            }
            else
            { // 没有空位则扩容
                if (m_slotStorage.Count >= MAX_CAPACITY)
                { // 超过最大容量，返回无效句柄
                    return new();
                }

                index = m_slotStorage.Count;
                m_slotStorage.Add(new Slot());
            }

            uint version = m_nextVersion;
            // 限制版本号范围
            ++m_nextVersion;
            if (m_nextVersion > MAX_VERSION)
            {
                m_nextVersion = 1; // 超出范围则重新从1开始
            }

            Slot slot = m_slotStorage[index];
            slot.version = version;
            m_slotStorage[index] = slot;

            ulong val = (ulong)((version << INDEX_BITS) + index);
            Handle<T> ret = new(val);
            ++m_size;
            return ret;
        }



        /**
         * 回收句柄
         * @param handle 已分配的句柄
         */
        public void RecycleHandle(Handle<T> handle)
        {
            int index = (int)(handle.Value & INDEX_MASK);

            Slot slot = m_slotStorage[index];
            slot.version = 0;
            slot.data = default;
            m_slotStorage[index] = slot;

            m_freeIndices.Add(index);
            --m_size;
        }



        /**
         * 添加元素
         */
        public Handle<T> Add(T element)
        {
            var handle = AllocHandle();
            if (handle.IsEmpty())
            {
                throw new System.Exception($"Error generating handle for element {element}");
            }

            Set(handle, element);
            return handle;
        }



        /**
         * 设置句柄槽位对应的元素
         * @param handle 已分配的句柄
         * @param element 要放入的元素
         */
        public void Set(Handle<T> handle, T element)
        {
            if (handle.IsEmpty())
            {
                throw new System.Exception("Handle is empty.");
            }

            int index = (int)(handle.Value & INDEX_MASK);
            uint version = (uint)(handle.Value >> INDEX_BITS);

            Slot slot = m_slotStorage[index];

            if (slot.version != version)
            {
                throw new System.Exception("Handle versions mismatch.");
            }

            slot.data = element;
            m_slotStorage[index] = slot;
        }



        /**
         * 删除
         */
        public void Remove(Handle<T> handle)
        {
            if (!TryGet(handle, out var _))
            {
                return;
            }

            RecycleHandle(handle);
        }



        public void RemoveAndClearHandle(ref Handle<T> handle)
        {
            Remove(handle);
            handle = Handle<T>.Empty;
        }



        public bool TryGet(Handle<T> handle, out T value)
        {
            // 快速排除无效句柄
            if (handle.IsEmpty())
            {
                value = default;
                return false;
            }

            int index = (int)(handle.Value & INDEX_MASK);
            uint version = (uint)(handle.Value >> INDEX_BITS);

            if (index < 0 || index >= m_slotStorage.Count)
            {
                value = default;
                return false;
            }

            Slot slot = m_slotStorage[index];

            if (slot.version != version)
            {
                value = default;
                return false;
            }

            value = slot.data;
            return true;
        }



        /**
         * 删除所有元素
         */
        public void Clear()
        {
            m_nextVersion = 1;
            m_slotStorage.Clear();
            m_freeIndices.Clear();
            m_size = 0;
        }



        public Enumerator GetEnumerator()
        {
            return new Enumerator(m_slotStorage);
        }



        public int GetSize()
        {
            return m_size;
        }
    }
}
