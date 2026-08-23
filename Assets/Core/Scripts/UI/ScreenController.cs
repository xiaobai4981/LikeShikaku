using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;



namespace Core.UI
{
    public abstract class ScreenController
    {
        private IGameInstance m_gameInstance;

        private int m_instanceID;        
        private IScreenID m_screenID;
        private RectTransform m_root;
        private Canvas m_canvas;

        private List<Handle<ITickable>> m_tickHandles;

        private CancellationTokenSource m_closeCTS = null;



        /// <summary>
        /// 用于替代destroyCancellationToken，以确保时机正确
        /// </summary>
        public CancellationToken CloseCancellationToken
        {
            get
            {
                // Lazy initialization
                if (m_closeCTS == null)
                {
                    m_closeCTS = new CancellationTokenSource();
                }
                return m_closeCTS.Token;
            }
        }



        internal void Setup(IGameInstance gameInstance, IScreenID screenID, GameObject screenGO)
        {
            m_gameInstance = gameInstance;
            m_screenID = screenID;
            m_root = screenGO.GetComponent<RectTransform>();
            m_canvas = screenGO.GetComponent<Canvas>();

            OnSetup();
        }



        internal void Cleanup()
        {
            OnCleanup();

            // 移除所有定时器
            UnregisterAllTicks();

            // 移除所有事件监听
            UnsubscribeAllMessages();
        }



        internal void CloseInternal()
        {
            OnClose();

            if (m_closeCTS != null)
            {
                m_closeCTS.Cancel();
                m_closeCTS.Dispose();
                m_closeCTS = null;
            }

            // 移除所有定时器
            UnregisterAllTicks();

            // 移除所有事件监听
            UnsubscribeAllMessages();
        }



        internal void SetInstanceID(int instanceID)
        {
            m_instanceID = instanceID;
        }



        public int GetInstanceID()
        {
            return m_instanceID;
        }



        public IScreenID GetScreenID()
        {
            return m_screenID;
        }



        public RectTransform GetRoot()
        {
            return m_root;
        }



        public Canvas GetCanvas()
        {
            return m_canvas;
        }



        #region 供子类override的生命周期方法
        protected virtual void OnSetup()
        {

        }



        protected virtual void OnCleanup()
        {

        }



        public virtual void OnOpen(object data)
        {

        }



        protected virtual void OnClose()
        {

        }



        public virtual void OnFocus()
        {

        }



        public virtual void OnFocusLost(bool isOccluded)
        {

        }
        #endregion



        public void Close()
        {
            UIManager.Instance.Close(this);
        }



        public Handle<ITickable> RegisterTick(ITickable tickable)
        {
            if (m_tickHandles == null)
            {
                m_tickHandles = new();
            }

            var h = m_gameInstance.TickSystem.RegisterTick(tickable);
            m_tickHandles.Add(h);
            return h;
        }



        public void UnregisterAllTicks()
        {
            if (m_tickHandles == null)
            {
                return;
            }

            foreach (var h in m_tickHandles)
            {
                var h2 = h;
                m_gameInstance.TickSystem.UnregisterTickAndClearHandle(ref h2);
            }
            m_tickHandles = null;
        }



        public void UnsubscribeAllMessages()
        {
            m_gameInstance.MessagingSystem.UnsubscribeAll(this);
        }



        public Button SetButtonClickHandler(string nodePath, UnityAction onClick)
        {
            return SetButtonClickHandler(GetRoot(), nodePath, onClick);
        }



        public Button SetButtonClickHandler(Transform root, string nodePath, UnityAction onClick)
        {
            Button btn;
            if (nodePath == null)
            {
                btn = root.GetComponent<Button>();
            }
            else
            {
                btn = root.Find<Button>(nodePath);
            }
            SetButtonClickHandler(btn, onClick);
            return btn;
        }



        public void SetButtonClickHandler(Button btn, UnityAction onClick)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onClick);
        }
    }
}
