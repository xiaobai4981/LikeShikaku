using System;
using UnityEngine;
using Cysharp.Threading.Tasks;



namespace Core.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        private RectTransform m_windowLayer;
        [SerializeField]
        private RectTransform m_overlayLayer;
        [SerializeField]
        private RectTransform m_loadingLayer;
        [SerializeField]
        private RectTransform m_toastLayer;
        [SerializeField]
        private GameObject m_inputBlocker;


        private static UIManager s_instance;
        public static UIManager Instance => s_instance;


        private UIGroup[] m_uiGroups;



        public struct InputBlockScope : IDisposable
        {
            private UIManager m_uiManager;

            public InputBlockScope(UIManager uiManager)
            {
                m_uiManager = uiManager;
                if ((++m_uiManager.m_blockInputCounter) > 0)
                {
                    m_uiManager.m_inputBlocker.SetActive(true);
                }
            }

            public void Dispose()
            {
                if (m_uiManager == null)
                {
                    // 默认构造或已经dispose过
                    return;
                }

                if (m_uiManager != s_instance)
                {
                    Debug.LogError($"InputBlockScope的生命周期超越了UIManager");
                    return;
                }
                if ((--m_uiManager.m_blockInputCounter) <= 0)
                {
                    m_uiManager.m_inputBlocker.SetActive(false);
                }

                // 置空，防止double dispose
                m_uiManager = null;
            }
        }
        private int m_blockInputCounter;



        public void Init(IGameInstance gameInstance)
        {
            s_instance = this;

            RectTransform[] layerRoots = new RectTransform[(int)ScreenLayer.NumLayers];
            layerRoots[(int)ScreenLayer.Window] = m_windowLayer;
            layerRoots[(int)ScreenLayer.Overlay] = m_overlayLayer;
            layerRoots[(int)ScreenLayer.Loading] = m_loadingLayer;
            layerRoots[(int)ScreenLayer.Toast] = m_toastLayer;

            m_uiGroups = new UIGroup[(int)ScreenLayer.NumLayers];
            for (int i = 0; i < m_uiGroups.Length; ++i)
            {
                m_uiGroups[i] = new(gameInstance, layerRoots[i]);
            }

            m_blockInputCounter = 0;
            m_inputBlocker.SetActive(false);
        }



        public void Cleanup()
        {
            foreach (var group in m_uiGroups)
            {
                group.Cleanup();
            }

            s_instance = null;
        }



        public InputBlockScope BlockInput()
        {
            return new InputBlockScope(this);
        }



        public int QueueOpen(IScreenID screenID, object data)
        {
            var inputBlockScope = BlockInput();

            var group = m_uiGroups[(int)screenID.GetScreenLayer()];
            return group.QueueOpen(screenID, data, inputBlockScope);
        }



        public async UniTask<int> OpenAsync(IScreenID screenID, object data)
        {
            int instanceID = QueueOpen(screenID, data);
            var group = m_uiGroups[(int)screenID.GetScreenLayer()];
            var ct = group.GetPurgeToken();
            while (!group.IsScreenOpenCompleted(instanceID))
            {
                await UniTask.Yield(ct);
            }

            if (group.FindScreenByInstanceID(instanceID) == null)
            {
                // 界面由于外部原因关闭，取消本Task
                throw new OperationCanceledException();
            }

            return instanceID;
        }



        public void QueuePreload(IScreenID screenID)
        {
            var group = m_uiGroups[(int)screenID.GetScreenLayer()];
            group.QueuePreload(screenID);
        }



        public async UniTask PreloadAsync(IScreenID screenID)
        {
            var group = m_uiGroups[(int)screenID.GetScreenLayer()];
            var ct = group.GetPurgeToken();
            group.QueuePreload(screenID);
            while (!group.IsScreenLoaded(screenID))
            {
                await UniTask.Yield(ct);
            }
        }



        public void CloseAllInstancesByType(IScreenID screenID)
        {
            var group = m_uiGroups[(int)screenID.GetScreenLayer()];
            group.CloseAllInstancesByType(screenID);
        }



        public void Close(ScreenController screenController)
        {
            var group = m_uiGroups[(int)screenController.GetScreenID().GetScreenLayer()];
            group.CloseByInstanceID(screenController.GetInstanceID());
        }



        public void PurgeLayer(ScreenLayer screenLayer)
        {
            var group = m_uiGroups[(int)screenLayer];
            group.Purge();
        }



        public bool IsScreenOpen(IScreenID screenID)
        {
            var group = m_uiGroups[(int)screenID.GetScreenLayer()];
            return group.IsScreenOpen(screenID);
        }
    } 
}
