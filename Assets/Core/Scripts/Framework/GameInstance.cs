using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using Core.UI;

namespace Core
{
    public interface IGameInstance
    {
        World CurrentWorld { get; }
        TickSystem TickSystem { get; }
        MessagingSystem MessagingSystem { get; }
    }



    public abstract class GameInstance<T> : MonoBehaviour, IGameInstance
        where T : GameInstance<T>
    {
        private static T s_instance = null;
        public static T Instance => s_instance;


        private UIManager m_uiManager;


        private World m_world;
        public static World CurrentWorld => Instance.m_world;
        World IGameInstance.CurrentWorld => m_world;


        /// <inheritdoc cref="World.CleanupCancellationToken"/>
        public static CancellationToken WorldCleanupCancellationToken => CurrentWorld.CleanupCancellationToken;


        private TickSystem m_tickSystem;
        public static TickSystem TickSystem => Instance.m_tickSystem;
        TickSystem IGameInstance.TickSystem => m_tickSystem;


        private MessagingSystem m_messagingSystem;
        public static MessagingSystem MessagingSystem => Instance.m_messagingSystem;
        MessagingSystem IGameInstance.MessagingSystem => m_messagingSystem;


        private bool m_isLoadingScene = false;
        public bool IsLoadingScene => m_isLoadingScene;



        protected virtual void Awake()
        {
            m_tickSystem = new();
            m_messagingSystem = new();
        }



        private void Start()
        {
            StartAsync().Forget();
        }



        protected virtual async UniTask StartAsync()
        {
            if (s_instance != null)
            {
                Debug.LogError("重复创建GameInstance");
                Destroy(gameObject);
                return;
            }

            s_instance = (T)this;
            DontDestroyOnLoad(gameObject);

            m_isLoadingScene = true;

            // 创建UIManager
            var uiManagerLoadHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Core/Prefabs/$UIManager.prefab");
            var uiManagerAsset = await uiManagerLoadHandle;
            var uiManagerGO = Instantiate(uiManagerAsset);
            uiManagerGO.name = "$UIManager";
            m_uiManager = uiManagerGO.GetComponentInChildren<UIManager>();
            DontDestroyOnLoad(uiManagerGO);
            Addressables.Release(uiManagerLoadHandle);

            m_uiManager.Init(this);
            await SetupCurrentScene();

            m_isLoadingScene = false;
        }



        protected virtual void OnApplicationQuit()
        {
            m_uiManager?.Cleanup();
            CleanupCurrentScene();
        }



        protected virtual void Update()
        {
            m_tickSystem.Tick();
        }



        protected virtual void FixedUpdate()
        {
            m_tickSystem.FixedTick();
        }



        protected virtual void LateUpdate()
        {
            m_tickSystem.LateTick();
        }



        private UniTask SetupCurrentScene()
        {
            m_world = World.Create();
            return m_world.Init();
        }



        private void CleanupCurrentScene()
        {
            if (m_world != null)
            {
                m_world.Cleanup();
                Destroy(m_world.gameObject);
                m_world = null;
            }
        }



        protected void LoadScene(string scenePath)
        {
            LoadSceneAsync(scenePath).Forget();
        }



        private async UniTaskVoid LoadSceneAsync(string scenePath)
        {
            if (m_isLoadingScene)
            {
                Debug.LogError($"场景加载中，无法加载场景{scenePath}");
                return;
            }
            m_isLoadingScene = true;

            var ct = Application.exitCancellationToken;

            await OnLoadingStarted(ct);

            // 切换场景时只关闭常规界面
            m_uiManager.PurgeLayer(ScreenLayer.Overlay);
            m_uiManager.PurgeLayer(ScreenLayer.Window);
            CleanupCurrentScene();

            await UniTask.WaitForSeconds(0.3f, cancellationToken: ct);
            //Common.UI.UILoading.UpdateProgress(0.18f);

            var loadHandle = Addressables.LoadSceneAsync(scenePath);
            await loadHandle;

            await UniTask.WaitForSeconds(0.4f, cancellationToken: ct);
            //Common.UI.UILoading.UpdateProgress(0.47f);

            try
            {
                await SetupCurrentScene();
            }
            catch (System.OperationCanceledException)
            {
                // 允许取消初始化场景流程
            }

            await UniTask.WaitForSeconds(0.2f, cancellationToken: ct);
            //Common.UI.UILoading.UpdateProgress(0.85f);

            await OnLoadingComplete(ct);

            m_isLoadingScene = false;
        }



        protected abstract UniTask OnLoadingStarted(CancellationToken ct);
        protected abstract UniTask OnLoadingComplete(CancellationToken ct);
    }
}
