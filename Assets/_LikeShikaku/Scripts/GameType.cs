using System;

[Serializable]
public class UserData
{
    public long score;                        // 用户每周分数
    public long coin;                         // 用户金币数量
    public string lastGetSideBarGiftTime;     // 用户上一次领取侧边栏奖励的时间
    public string lastGetDailyGiftTime;       // 用户上一次领取侧边栏奖励的时间

    public UserData()
    {
        score = 0;
        coin = 0;
        lastGetSideBarGiftTime = "2022.12.12";
        lastGetDailyGiftTime = "2022.12.12";
    }

    public void Init()
    {
#if UNITY_EDITOR
        coin = 999999;
#else
        coin = 0;
#endif
    }

    public void UpdateData(UserData newUserData, bool isInit = false)
    {
        if (isInit == true)
        {
            DayOfWeek today = DateTime.Now.DayOfWeek;
            if (today == DayOfWeek.Monday)
            {
                score = 0;
            }
            else
            {
                score = newUserData.score;
            }
        }
        else
        {
            score = newUserData.score;
        }

        coin = newUserData.coin;
        lastGetSideBarGiftTime = newUserData.lastGetSideBarGiftTime;
        lastGetDailyGiftTime = newUserData.lastGetDailyGiftTime;
    }
}