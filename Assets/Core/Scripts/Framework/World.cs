using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Core
{
    public class World : MonoBehaviour
    {
        private CancellationTokenSource m_cleanupCTS;

        /// <summary>
        /// 当前World销毁的CTS
        /// </summary>
        /// <remarks>
        /// destroyCancellationToken可能比World.Cleanup晚，导致依赖的Task结束时机不对，
        /// 因此这里自己维护一个CancellationTokenSource
        /// </remarks>
        public CancellationToken CleanupCancellationToken => m_cleanupCTS.Token;


        private PoolManager m_poolMgr;


        private GameModeBase m_gameMode;


        private TickSystem m_tickSystem;
        public TickSystem TickSystem => m_tickSystem;



        public static World Create()
        {
            GameObject worldGO = new("$World");
            return worldGO.AddComponent<World>();
        }



        public async UniTask Init()
        {
            m_cleanupCTS = new();

            m_tickSystem = new();

            m_poolMgr = new();
            m_poolMgr.Init();

            m_gameMode = FindAnyObjectByType<GameModeBase>();
            if (m_gameMode == null)
            {
                GameObject gameModeGO = new();
                m_gameMode = gameModeGO.AddComponent<GameModeBase>();
            }
            m_gameMode.gameObject.name = "$GameMode";
            m_gameMode.PreInit(this);

            enabled = false; // 等待Task期间不调用Update
            await m_gameMode.Init();
            enabled = true;
        }



        public void Cleanup()
        {
            m_gameMode.Cleanup();

            m_poolMgr.Cleanup();

            m_cleanupCTS.Cancel();
            m_cleanupCTS.Dispose();
            m_cleanupCTS = null;
        }



        public void Update()
        {
            m_gameMode.Tick();
            m_tickSystem.Tick();
        }



        public void FixedUpdate()
        {
            m_gameMode.FixedTick();
            m_tickSystem.FixedTick();
        }



        public void LateUpdate()
        {
            m_gameMode.LateTick();
            m_tickSystem.LateTick();
        }



        public GameModeBase GetGameMode()
        {
            return m_gameMode;
        }



        public T GetGameMode<T>()
            where T : GameModeBase
        {
            return (T)m_gameMode;
        }



        public void SetPaused(bool newPaused)
        {
            Time.timeScale = newPaused ? 0 : 1;
            enabled = !newPaused;
        }
    }
}
