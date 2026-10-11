using Core;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;

public class MainGameInstance : GameInstance<MainGameInstance>
{
    #region TODO 抖音相关




    #endregion

    #region TODO 玩家相关

    // 玩家数据相关
    private UserData m_localUserData;
    public UserData LocalUserData => m_localUserData;

    private Queue<UniTask> m_uploadQueue;

    #endregion

    [SerializeField]
    private AssetReferenceT<AudioMixer> m_audioMixer;

    private CancellationToken ct;

    protected override async UniTask StartAsync()
    {
        var debugLogMgrGO = FindAnyObjectByType<IngameDebugConsole.DebugLogManager>(FindObjectsInactive.Include).gameObject;
        if (Debug.isDebugBuild)
        {
            debugLogMgrGO.SetActive(!Application.isEditor);
        }
        else
        {
            debugLogMgrGO.SetActive(false);
        }

        await base.StartAsync();

        var audioMixerInstance = await m_audioMixer.LoadAssetAsync();
        ct = Application.exitCancellationToken;

        QualitySettings.vSyncCount = 0;
        Application.runInBackground = true;

        m_uploadQueue = new();
        //// 用户设置管理器初始化
        //m_userSettingsManager = new(audioMixerInstance);
        //m_userSettingsManager.LoadFromFile();

        //// 预先打开单例UI
        //await Common.UI.UIScreenFader.Load();
        //await Common.UI.UILoading.Load();

        //await Common.UI.UIPopUp.Load();
        //await MainScene.HomePart.UI.UISideBar.Load();

        //// 加载抖音相关SDK和玩家数据,侧边栏和广告初始化也在这里
        //TT.InitSDK();
#if UNITY_EDITOR
        Application.targetFrameRate = -1;
#else
        TT.SetPreferredFramesPerSecond(30);
#endif
        // TODO 广告
        //m_ttAdManager.CreateRewardedVideoAd(rewardedVideoAdUnitId);
        //m_ttAdManager.CreateInterstitialAd(interstitialAdUnitId);
#if !UNITY_EDITOR
        db = TT.CreateCloud().CloudDb();
#endif
        // TODO 查看是否是从侧边栏进入
        //TT.GetAppLifeCycle().OnShow += HandleSideBar;

        await GameStartInit(ct);
        LoadBattleScene();
    }

    public static void LoadBattleScene()
    {
        Instance.LoadScene("Assets/_LikeShikaku/Content/MainScene/MainScene.unity");
    }

    protected override async UniTask OnLoadingComplete(CancellationToken ct)
    {
        await UniTask.WaitForSeconds(0.5f, cancellationToken: ct);
        //await Common.UI.UIScreenFader.Show(ct);
        //await Common.UI.UIScreenFader.Hide(ct);
        //Common.UI.UILoading.Show();
        //Common.UI.UILoading.UpdateProgress(0f);
    }

    protected override async UniTask OnLoadingStarted(CancellationToken ct)
    {
        await UniTask.WaitForSeconds(0.5f, cancellationToken: ct);
        //Common.UI.UILoading.UpdateProgress(1f);
        //await Common.UI.UIScreenFader.Show(ct);
        //Common.UI.UILoading.Hide();
        //await Common.UI.UIScreenFader.Hide(ct);
    }

    private async UniTask GameStartInit(CancellationToken ct)
    {
        //bool isTrueLogin = false;
        //TT.Login(
        //    (code, anonymousCode, isLogin) =>
        //    {
        //        isTrueLogin = true;
        //    },
        //    (msg) =>
        //    {
        //    }
        //);
        //while (!isTrueLogin)
        //{
        //    await UniTask.Yield(Application.exitCancellationToken);
        //}

        // 获取玩家数据并保存数据至LocalUserData
        await InitUserData();

        // 获取后先上报一份数据
        AddUploadQueue(m_localUserData);
    }

    private async UniTask InitUserData()
    {
        // 问抖音云数据库要数据
        bool isComplete = false;
        bool isFirst = false;
        m_localUserData = new UserData();

#if UNITY_EDITOR
        m_localUserData.Init();
        await UniTask.Yield(Application.exitCancellationToken);
#else

        #region 本地缓存写法

        UserData loadedLocalUserData = TT.LoadSaving<UserData>("Magic2048LocalData");

        if (null == loadedLocalUserData)
        {
            Debug.Log("不存在存档");
            m_localUserData.Init();
            isFirst = true;
        }
        else
        {
            Debug.Log("存在存档" + loadedLocalUserData.lastGetGiftTime);
            m_localUserData.UpdateData(loadedLocalUserData, true);
        }
        isComplete = true;

        while (!isComplete)
        {
            await UniTask.Yield(ct);
        }

        if (isFirst)
        {
            await UploadUserData(m_localUserData, isFirst);
        }

        UploadUserDataInQueue().Forget();
        RankDataSet();

        #endregion
#endif
    }

    public async UniTask UploadUserDataInQueue()
    {
        while (!ct.IsCancellationRequested)
        {
            if (m_uploadQueue.Count <= 0)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                continue;
            }

            var task = m_uploadQueue.Dequeue();

            await task;

            await UniTask.WaitForSeconds(0.1f, cancellationToken: ct);
        }
    }

    // 写入排行榜
    public void RankDataSet()
    {
        //var paramJson = new JsonData
        //{
        //    ["zoneId"] = "test",
        //    ["dataType"] = 0,
        //    ["value"] = m_localUserData.score.ToString(),
        //};
        //if (!Debug.isDebugBuild)
        //{
        //    paramJson["zoneId"] = "default";
        //}

        //TT.SetImRankData(paramJson, (isSet, str) =>
        //{
        //    if (isSet)
        //    {

        //    }
        //});
    }

    public void AddUploadQueue(UserData userData)
    {
        m_localUserData.UpdateData(userData);
        var nowUserData = new UserData();
        nowUserData.UpdateData(m_localUserData);
        m_uploadQueue.Enqueue(UploadUserData(nowUserData));
    }

    private async UniTask UploadUserData(UserData nowUserData, bool isFirst = false)
    {
        var isComplete = false;

        #region 本地缓存玩家数据

        // 本地缓存，不用在意是否是第一次游玩
        //TT.Save(nowUserData, "Magic2048LocalData");
        isComplete = true;

        #endregion

        while (!isComplete)
        {
            await UniTask.Yield(ct);
        }
    }

    private void HandleSideBar(Dictionary<string, object> param)
    {
        //if (param["scene"].ToString() == "021036")
        //{
        //    if (param["launchFrom"].ToString() == "homepage")
        //    {
        //        if (param["location"].ToString() == "sidebar_card")
        //        {
        //            isSideBarEnter = true;
        //        }
        //    }
        //}
        //else
        //{
        //    isSideBarEnter = false;
        //}
    }
}
