using UnityEngine;
using UnityEngine.InputSystem;

namespace MainScene.BattlePart.Input
{
    public class BattleInputManager
    {
        private InputActionAsset m_projectWideActions;
        private InputActionMap m_playerMap;

        private InputAction m_touchPressMoveAction;
        private InputAction m_touchPositionMoveAction;
        private Vector2 m_touchStartPosition;

        public void Init()
        {
            m_projectWideActions = InputSystem.actions;

            m_playerMap = m_projectWideActions.FindActionMap("Player", true);
            m_touchPressMoveAction = m_playerMap.FindAction("TouchPress", true);
            m_touchPositionMoveAction = m_playerMap.FindAction("TouchPosition", true);

            m_playerMap.Enable();
        }

        public void EnterGameplay()
        {
            m_playerMap?.Enable();
        }

        public void OpenMenu()
        {
            m_playerMap?.Disable();
        }

        public void Clear()
        {
            m_projectWideActions.Disable();
        }
    }
}