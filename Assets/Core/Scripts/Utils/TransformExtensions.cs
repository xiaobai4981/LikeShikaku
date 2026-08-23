using UnityEngine;



namespace Core
{
    public static class TransformExtensions
    {
        public static Transform FindRecursively(this Transform root, string name)
        {
            return root.FindRecursively<Transform>(name);
        }



        public static TComponent FindRecursively<TComponent>(this Transform root, string name)
            where TComponent : Component
        {
            using var _ = QueuePool<Transform>.Get(out var queue);
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                Transform t = queue.Dequeue();
                TComponent c;
                if (t.name == name && t.TryGetComponent(out c))
                {
                    return c;
                }

                foreach (Transform child in t)
                {
                    queue.Enqueue(child);
                }
            }
            return null;
        }



        public static T Find<T>(this Transform root, string path)
            where T : Component
        {
            Transform t = root.Find(path);
            if (t == null)
            {
                return null;
            }

            if (!t.TryGetComponent(out T com))
            {
                return null;
            }

            return com;
        }



        public static void Find<T>(this Transform root, string path, out T component)
            where T : Component
        {
            component = Find<T>(root, path);
        }
    }
}
