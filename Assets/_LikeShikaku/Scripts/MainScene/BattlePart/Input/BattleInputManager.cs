using UnityEngine;
using UnityEngine.InputSystem;

namespace MainScene.BattlePart.Input
{
    public struct BattleInputFrame
    {
        public bool PressDown;
        public bool Pressing;
        public bool PressUp;
        public Vector2 ScreenPosition;
    }

    public class BattleInputManager
    {
        private InputActionAsset m_projectWideActions;
        private InputActionMap m_playerMap;

        private InputAction m_touchPressMoveAction;
        private InputAction m_touchPositionMoveAction;

        public BattleInputFrame CurrentFrame { get; private set; }
        public bool IsEnabled => m_playerMap != null && m_playerMap.enabled;

        public void Init()
        {
            m_projectWideActions = InputSystem.actions;

            m_playerMap = m_projectWideActions.FindActionMap("Player", true);
            m_touchPressMoveAction = m_playerMap.FindAction("TouchPress", true);
            m_touchPositionMoveAction = m_playerMap.FindAction("TouchPosition", true);

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
                PressDown = m_touchPressMoveAction.WasPressedThisFrame(),
                Pressing = m_touchPressMoveAction.IsPressed(),
                PressUp = m_touchPressMoveAction.WasReleasedThisFrame(),
                ScreenPosition = m_touchPositionMoveAction.ReadValue<Vector2>()
            };
        }

        public void EnterGameplay()
        {
            m_playerMap?.Enable();
            CurrentFrame = default;
        }

        public void OpenMenu()
        {
            m_playerMap?.Disable();
            CurrentFrame = default;
        }

        public void Clear()
        {
            m_playerMap.Disable();

            m_playerMap = null;
            m_touchPressMoveAction = null;
            m_touchPositionMoveAction = null;
            m_projectWideActions = null;

            CurrentFrame = default;
        }
    }
}