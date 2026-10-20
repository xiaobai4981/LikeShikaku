using UnityEngine;
using UnityEngine.InputSystem;

namespace MainScene.BattlePart.Input
{
    /// <summary>
    /// 本帧原始输入快照。
    /// </summary>
    public struct BattleInputFrame
    {
        public bool PressDown;
        public bool Pressing;
        public bool PressUp;

        public Vector2 ScreenPosition;
    }

    /// <summary>
    /// 只负责采集输入，不处理棋盘规则。
    /// </summary>
    public class BattleInputManager
    {
        private InputActionAsset m_projectWideActions;
        private InputActionMap m_playerMap;

        private InputAction m_touchPressAction;
        private InputAction m_touchPositionAction;

        public BattleInputFrame CurrentFrame { get; private set; }

        public bool IsEnabled => m_playerMap != null && m_playerMap.enabled;

        public void Init()
        {
            m_projectWideActions = InputSystem.actions;

            if (m_projectWideActions == null)
            {
                Debug.LogError("没有配置 Project-Wide Input Actions。");
                return;
            }

            m_playerMap = m_projectWideActions.FindActionMap("Player", true);
            m_touchPressAction = m_playerMap.FindAction("TouchPress", true);
            m_touchPositionAction = m_playerMap.FindAction("TouchPosition", true);
            m_playerMap.Enable();

            CurrentFrame = default;
        }

        public void Tick()
        {
            if (!IsEnabled)
            {
                CurrentFrame = default;
                return;
            }

            CurrentFrame = new BattleInputFrame
            {
                PressDown = m_touchPressAction.WasPressedThisFrame(),
                Pressing = m_touchPressAction.IsPressed(),
                PressUp = m_touchPressAction.WasReleasedThisFrame(),
                ScreenPosition = m_touchPositionAction.ReadValue<Vector2>()
            };
        }

        public void EnterGameplay()
        {
            CurrentFrame = default;
            m_playerMap?.Enable();
        }

        public void OpenMenu()
        {
            m_playerMap?.Disable();
            CurrentFrame = default;
        }

        public void Clear()
        {
            m_playerMap?.Disable();

            m_touchPressAction = null;
            m_touchPositionAction = null;
            m_playerMap = null;
            m_projectWideActions = null;

            CurrentFrame = default;
        }
    }
}