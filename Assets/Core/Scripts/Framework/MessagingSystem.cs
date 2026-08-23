using System.Collections.Generic;

namespace Core
{
    public class MessagingSystem
    {
        private struct SubscriberInfo : System.IEquatable<SubscriberInfo>
        {
            public object owner;
            public System.Delegate callback;

            // List.Contains需要实现IEquatable避免GC
            public readonly bool Equals(SubscriberInfo other)
            {
                return owner == other.owner &&
                    callback == other.callback;
            }
        }

        private Dictionary<System.Type, List<SubscriberInfo>> m_subscriberMap = new();
        private List<SubscriberInfo> m_pendingSubscribers = new();



        public void Subscribe<T>(System.Action<T> callback, object owner)
        {
            System.Type messageType = typeof(T);

            List<SubscriberInfo> subscribers;
            if (!m_subscriberMap.TryGetValue(messageType, out subscribers))
            {
                subscribers = new();
                m_subscriberMap[messageType] = subscribers;
            }

            // 防止重复注册
            foreach (var info in subscribers)
            {
                if ((System.Delegate)callback == info.callback)
                {
                    return;
                }
            }

            subscribers.Add(new()
            {
                owner = owner,
                callback = callback,
            });
        }



        public void Unsubscribe<T>(System.Action<T> callback)
        {
            System.Type messageType = typeof(T);

            List<SubscriberInfo> subscribers;
            if (m_subscriberMap.TryGetValue(messageType, out subscribers))
            {
                for (int i = subscribers.Count - 1; i >= 0; --i)
                {
                    if ((System.Delegate)callback == subscribers[i].callback)
                    {
                        subscribers.RemoveAt(i);
                    }
                }
            }
        }



        public void UnsubscribeAll(object owner)
        {
            foreach (var kv in m_subscriberMap)
            {
                List<SubscriberInfo> subscribers = kv.Value;
                for (int i = subscribers.Count - 1; i >= 0; --i)
                {
                    if (subscribers[i].owner == owner)
                    {
                        subscribers.RemoveAt(i);
                    }
                }
            }
        }



        public void Dispatch<T>(T message)
        {
            System.Type messageType = typeof(T);

            List<SubscriberInfo> subscribers;
            if (m_subscriberMap.TryGetValue(messageType, out subscribers))
            {
                m_pendingSubscribers.AddRange(subscribers);

                foreach (var info in m_pendingSubscribers)
                {
                    if (subscribers.Contains(info)) // 防止一边遍历一边删除导致错误
                    {
                        ((System.Action<T>)info.callback).Invoke(message);
                    }
                }

                m_pendingSubscribers.Clear();
            }
        }
    }
}