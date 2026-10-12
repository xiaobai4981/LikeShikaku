using Core;
using MainScene.BattlePart.Configs;
using UnityEngine;

namespace MainScene
{
    public class MainSceneGameMode : GameModeBase
    {
        // 这里管理的是临时用户数据，用于游戏内，上传时还是用的MainGameInstance的LocalUserData
        private UserData m_copyUserData;
        public UserData CopyUserData => m_copyUserData;

        // 战斗配置
        [SerializeField]
        private BattleSettings m_battleSettings;
        public BattleSettings BattleSettings => m_battleSettings;


    }
}