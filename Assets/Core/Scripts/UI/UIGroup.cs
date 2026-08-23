using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;



namespace Core.UI
{
    public class UIGroup
    {
        private readonly IGameInstance m_gameInstance;



        private readonly RectTransform m_layerRoot;
        private readonly List<ScreenController> m_stack;
        private readonly Dictionary<IScreenID, ScreenController> m_cache;
        private readonly Dictionary<IScreenID, AsyncOperationHandle<GameObject>> m_assetLoadHandles;



        private struct ScreenOpenRequest
        {
            public IScreenID screenID;
            public object data;
            public UIManager.InputBlockScope inputBlockScope;
            public int instanceID;
        }
        // 打开界面请求排队执行，以确保异步打开界面的顺序
        private readonly List<ScreenOpenRequest> m_pendingOpenRequests;



        private CancellationTokenSource m_cts;



        private static int s_screenInstanceID = 0;



        public UIGroup(IGameInstance gameInstance, RectTransform layerRoot)
        {
            m_gameInstance = gameInstance;
            m_layerRoot = layerRoot;
            m_stack = new();
            m_cache = new();
            m_assetLoadHandles = new();
            m_pendingOpenRequests = new();
            m_cts = new();
        }



        public void Cleanup()
        {
            PurgeInternal();
        }



        public int QueueOpen(IScreenID screenID, object data, UIManager.InputBlockScope inputBlockScope)
        {
            int instanceID = ++s_screenInstanceID;

            ScreenOpenRequest openRequest = new()
            {
                screenID = screenID,
                data = data,
                inputBlockScope = inputBlockScope,
                instanceID = instanceID,
            };
            m_pendingOpenRequests.Add(openRequest);

            if (!m_assetLoadHandles.ContainsKey(screenID))
            {
                // UI资源还未加载过，那么开始加载
                PreloadInternal(screenID);
            }
            else
            {
                TryConsumeOpenRequests();
            }

            return instanceID;
        }



        private void TryConsumeOpenRequests()
        {
            while (m_pendingOpenRequests.Count > 0)
            {
                var openRequest = m_pendingOpenRequests[0];
                if (!IsScreenLoaded(openRequest.screenID))
                {
                    // 还未加载完成，停止遍历，不能打开后面的界面
                    break;
                }

                m_pendingOpenRequests.RemoveAt(0);
                OpenInternal(openRequest);
            }
        }



        private void OpenInternal(ScreenOpenRequest openRequest)
        {
            IScreenID screenID = openRequest.screenID;

            bool topScreenClosed;

            // 按照《崩铁》的逻辑，如果全屏界面被重复打开，那么先关闭旧的
            bool allowMultipleInstances = screenID.GetScreenType() == ScreenType.Popup;
            if (!allowMultipleInstances)
            {
                CloseAllInstancesByTypeInternal(screenID, out topScreenClosed);
            }
            else
            {
                topScreenClosed = false;
            }

            ScreenController screen;
            if (m_cache.TryGetValue(screenID, out screen))
            {
                m_cache.Remove(screenID);
                screen.SetInstanceID(openRequest.instanceID);
                screen.GetRoot().SetAsLastSibling();
                screen.GetRoot().gameObject.SetActive(true);
            }
            else
            {
                AsyncOperationHandle<GameObject> loadHandle = m_assetLoadHandles[screenID];
                GameObject screenAsset = loadHandle.Result;
                GameObject screenGO = Object.Instantiate(screenAsset, m_layerRoot);
                screenGO.name = screenAsset.name;
                screen = screenID.CreateScreen();
                screen.SetInstanceID(openRequest.instanceID);
                screen.Setup(m_gameInstance, screenID, screenGO);
            }

            if (!topScreenClosed) // 如果关闭了TopScreen，那么无需再调用OnFocusLost
            {
                var topScreen = GetTopScreen();
                if (topScreen != null)
                {
                    bool occluded = screenID.GetScreenType() == ScreenType.FullScreen;
                    topScreen.OnFocusLost(occluded);
                }
            }

            // 打开全屏界面时，隐藏所有被盖住的界面
            if (screenID.GetScreenType() == ScreenType.FullScreen)
            {
                for (int i = m_stack.Count - 1; i >= 0; --i)
                {
                    m_stack[i].GetRoot().gameObject.SetActive(false);
                }
            }

            m_stack.Add(screen);
            screen.OnOpen(openRequest.data);
            screen.OnFocus();

            openRequest.inputBlockScope.Dispose();
        }



        public void CloseAllInstancesByType(IScreenID screenID)
        {
            // 界面可能还未加载完成，此时取消打开，清掉相关回调
            for (int i = m_pendingOpenRequests.Count - 1; i >= 0; --i)
            {
                var openRequest = m_pendingOpenRequests[i];
                if (openRequest.screenID == screenID)
                {
                    openRequest.inputBlockScope.Dispose();
                    m_pendingOpenRequests.RemoveAt(i);
                }
            }
            CloseAllInstancesByTypeInternal(screenID, out bool topScreenClosed);
            PostCloseScreens(screenID, topScreenClosed);
        }



        public void CloseByInstanceID(int instanceID)
        {
            ScreenController screenToClose = FindScreenByInstanceID(instanceID);
            if (screenToClose == null)
            {
                return;
            }

            IScreenID screenID = screenToClose.GetScreenID();
            CloseInstanceInternal(screenToClose, out bool topScreenClosed);
            PostCloseScreens(screenID, topScreenClosed);
        }



        private void CloseAllInstancesByTypeInternal(IScreenID screenID, out bool topScreenClosed)
        {
            topScreenClosed = false;
            while (true)
            {
                ScreenController screenToClose = FindFirstScreenByType(screenID);
                if (screenToClose == null)
                {
                    break;
                }
                bool topScreenClosed2;
                CloseInstanceInternal(screenToClose, out topScreenClosed2);
                topScreenClosed = topScreenClosed || topScreenClosed2;
            }
        }



        private void CloseInstanceInternal(ScreenController screenToClose, out bool topScreenClosed)
        {
            int stackIndexStart = m_stack.IndexOf(screenToClose);
            int stackIndexEnd;
            if (screenToClose.GetScreenID().GetScreenType() == ScreenType.FullScreen)
            {
                stackIndexEnd = stackIndexStart + 1;

                // 按照《崩铁》的逻辑，全屏界面关闭时，连带关闭它打开过的弹窗
                while (stackIndexEnd < m_stack.Count)
                {
                    ScreenController s = m_stack[stackIndexEnd];
                    if (s.GetScreenID().GetScreenType() != ScreenType.Popup)
                    {
                        break;
                    }
                    ++stackIndexEnd;
                }
            }
            else
            {
                // 非全屏界面仅关闭自己
                stackIndexEnd = stackIndexStart + 1;
            }

            topScreenClosed = false;
            for (int i = stackIndexEnd - 1; i >= stackIndexStart; --i)
            {
                ScreenController s = m_stack[i];
                IScreenID sID = s.GetScreenID();

                if (s == GetTopScreen())
                {
                    // 自己就是当前聚焦的窗口
                    s.OnFocusLost(false);
                    topScreenClosed = true;
                }

                s.GetRoot().gameObject.SetActive(false);
                s.CloseInternal();

                if (sID.ShouldKeepCached() &&
                    !m_cache.ContainsKey(sID)) // 只cache一个实例
                {
                    m_cache.Add(sID, s);
                }
                else
                {
                    s.Cleanup();
                    Object.Destroy(s.GetRoot().gameObject);
                }
            }

            int numClosedScreens = stackIndexEnd - stackIndexStart;
            m_stack.RemoveRange(stackIndexStart, numClosedScreens);
        }



        private void PostCloseScreens(IScreenID screenID, bool topScreenClosed)
        {
            // 关闭全屏界面后，取消隐藏最上层的全屏界面及与之关联的弹窗
            if (screenID.GetScreenType() == ScreenType.FullScreen)
            {
                for (int i = m_stack.Count - 1; i >= 0; --i)
                {
                    ScreenController screen = m_stack[i];
                    screen.GetRoot().gameObject.SetActive(true);
                    if (screen.GetScreenID().GetScreenType() == ScreenType.FullScreen)
                    {
                        break;
                    }
                }
            }

            ScreenController newTopScreen = GetTopScreen();
            if (newTopScreen != null && topScreenClosed)
            {
                newTopScreen.OnFocus();
            }
        }



        public ScreenController GetTopScreen()
        {
            if (m_stack.Count > 0)
            {
                return m_stack[m_stack.Count - 1];
            }

            return null;
        }



        public ScreenController FindFirstScreenByType(IScreenID screenID)
        {
            for (int i = m_stack.Count - 1; i >= 0; --i)
            {
                if (m_stack[i].GetScreenID() == screenID)
                {
                    return m_stack[i];
                }
            }
            return null;
        }



        public ScreenController FindScreenByInstanceID(int instanceID)
        {
            foreach (var screen in m_stack)
            {
                if (screen.GetInstanceID() == instanceID)
                {
                    return screen;
                }
            }
            return null;
        }



        public bool IsScreenOpenCompleted(int instanceID)
        {
            foreach (var openRequest in m_pendingOpenRequests)
            {
                if (openRequest.instanceID == instanceID)
                {
                    return false;
                }
            }

            // m_pendingOpenRequests 中找不到，也有可能是等待加载期间调用了 Purge,
            // 这并不代表 UI 已经打开完成
            return true;
        }



        public void Purge()
        {
            PurgeInternal();

            m_cts = new();
        }



        public bool IsScreenOpen(IScreenID screenID)
        {
            if (FindFirstScreenByType(screenID) != null)
            {
                return true;
            }

            foreach (var openRequest in m_pendingOpenRequests)
            {
                if (openRequest.screenID == screenID)
                {
                    return true;
                }
            }

            return false;
        }



        private void PurgeInternal()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;

            for (int i = m_stack.Count - 1; i >= 0; --i)
            {
                ScreenController screen = m_stack[i];
                GameObject go = screen.GetRoot().gameObject;
                // 先主动SetActive(false)，确保OnDisable调到，因为Destroy并不会马上调用OnDisable
                // 这样做是为了避免打包后的游戏退出时TMP报错
                // https://forum.unity.com/threads/tmpro-submeshui-error-when-changing-scenes.1129724/
                go.SetActive(false);
                screen.CloseInternal();
                screen.Cleanup();
                Object.Destroy(go);
            }
            m_stack.Clear();

            foreach (ScreenController screen in m_cache.Values)
            {
                GameObject go = screen.GetRoot().gameObject;
                go.SetActive(false);
                screen.Cleanup();
                Object.Destroy(go);
            }
            m_cache.Clear();

            foreach (var openRequest in m_pendingOpenRequests)
            {
                openRequest.inputBlockScope.Dispose();
            }
            m_pendingOpenRequests.Clear();

            foreach (var loadHandle in m_assetLoadHandles.Values)
            {
                Addressables.Release(loadHandle);
            }
            m_assetLoadHandles.Clear();
        }



        private void OnAssetLoaded(AsyncOperationHandle<GameObject> loadHandle)
        {
            TryConsumeOpenRequests();
        }



        public void QueuePreload(IScreenID screenID)
        {
            if (m_assetLoadHandles.ContainsKey(screenID))
            {
                // 已经在加载中，或已经加载完成
                return;
            }

            PreloadInternal(screenID);
        }



        private void PreloadInternal(IScreenID screenID)
        {
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(screenID.GetPrefabPath());
            m_assetLoadHandles.Add(screenID, loadHandle);
            loadHandle.Completed += OnAssetLoaded;
        }



        /// <summary>
        /// 查询 UI 是否已经加载完成
        /// </summary>
        /// <remarks>
        /// 注意：如果加载期间调用了 Purge，此方法会一直返回 false
        /// </remarks>
        public bool IsScreenLoaded(IScreenID screenID)
        {
            return m_assetLoadHandles.TryGetValue(screenID, out var loadHandle) &&
                loadHandle.IsDone;
        }



        public CancellationToken GetPurgeToken()
        {
            return m_cts.Token;
        }
    }
}
