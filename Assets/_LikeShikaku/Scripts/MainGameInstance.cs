using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MainGameInstance : GameInstance<MainGameInstance>
{
    private const string UserDataKey = "Shikaku.UserData.v1";
    // 玩家基础数据。关卡进度由 ShikakuProgressStore 单独管理。
    private UserData m_localUserData;
    public UserData LocalUserData => m_localUserData;

    protected override async UniTask StartAsync()
    {
        var debugConsole = FindAnyObjectByType<IngameDebugConsole.DebugLogManager>(FindObjectsInactive.Include);
        if (debugConsole != null)
        {
            debugConsole.gameObject.SetActive(Debug.isDebugBuild && !Application.isEditor);
        }

        await base.StartAsync();
        if (Instance != this)
        {
            return;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        LoadUserData();
        LoadBattleScene();
    }

    public static void LoadBattleScene()
    {
        Instance.LoadScene("Assets/_LikeShikaku/Content/MainScene/MainScene.unity");
    }

    protected override UniTask OnLoadingStarted(System.Threading.CancellationToken ct)
    {
        return UniTask.CompletedTask;
    }

    protected override UniTask OnLoadingComplete(System.Threading.CancellationToken ct)
    {
        return UniTask.CompletedTask;
    }

    private void LoadUserData()
    {
        try
        {
            string json = PlayerPrefs.GetString(UserDataKey, "");
            m_localUserData = string.IsNullOrEmpty(json) ? new UserData() : JsonUtility.FromJson<UserData>(json);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("玩家基础数据读取失败：" + exception.Message);
        }

        m_localUserData ??= new UserData();
    }

    public void AddUploadQueue(UserData userData)
    {
        if (userData == null)
        {
            return;
        }

        m_localUserData ??= new UserData();
        m_localUserData.UpdateData(userData);
        PlayerPrefs.SetString(UserDataKey, JsonUtility.ToJson(m_localUserData));
        PlayerPrefs.Save();
    }

    protected override void OnApplicationQuit()
    {
        if (m_localUserData != null)
        {
            AddUploadQueue(m_localUserData);
        }

        base.OnApplicationQuit();
    }
}
