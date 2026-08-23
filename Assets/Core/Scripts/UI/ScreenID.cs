using Cysharp.Threading.Tasks;



namespace Core.UI
{
    public enum ScreenType
    {
        FullScreen, // 全屏界面
        Popup, // 弹窗
    }



    public enum ScreenLayer : int
    {
        Window = 0, // 通常的全屏界面和弹窗
        Overlay, // 资源栏
        Loading, // Loading界面（不会因切换场景而卸载）
        Toast, // 提示（不会因切换场景而卸载）

        NumLayers,
    }



    public interface IScreenID
    {
        ScreenType GetScreenType();
        bool ShouldKeepCached();
        string GetScreenName();
        string GetPrefabPath();
        ScreenLayer GetScreenLayer();
        ScreenController CreateScreen();
    }



    public abstract class ScreenID<T1, T2> : IScreenID
        where T1 : ScreenID<T1, T2>, new()
        where T2 : ScreenController, new()
    {
        private static readonly T1 s_instance = new();

        public abstract ScreenType GetScreenType();

        public virtual bool ShouldKeepCached()
        {
            return false;
        }

        public string GetScreenName()
        {
            return typeof(T2).FullName;
        }

        public virtual ScreenLayer GetScreenLayer()
        {
            return ScreenLayer.Window;
        }

        public abstract string GetPrefabPath();

        public ScreenController CreateScreen()
        {
            return new T2();
        }

        public static void QueueOpen(object data = null)
        {
            UIManager.Instance.QueueOpen(s_instance, data);
        }

        public static async UniTask OpenAsync(object data = null)
        {
            await UIManager.Instance.OpenAsync(s_instance, data);
        }

        public static void QueuePreload()
        {
            UIManager.Instance.QueuePreload(s_instance);
        }

        public static async UniTask PreloadAsync()
        {
            await UIManager.Instance.PreloadAsync(s_instance);
        }

        public static void CloseAllInstances()
        {
            UIManager.Instance.CloseAllInstancesByType(s_instance);
        }

        public static bool IsOpen()
        {
            return UIManager.Instance.IsScreenOpen(s_instance);
        }
    }    
}
