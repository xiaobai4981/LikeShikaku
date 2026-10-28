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
        public bool Cancelled;
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
        private UnityEngine.InputSystem.Controls.TouchControl m_activeTouch;
        private bool m_waitForRelease;
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

            // 一次拖动只跟随发起手指，避免第二根手指改变终点。
            var screen = Touchscreen.current;
            bool touchHeld = false;
            if (screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    touchHeld |= touch.press.isPressed;
                }
            }

            if (m_waitForRelease)
            {
                m_waitForRelease = touchHeld || Mouse.current != null && Mouse.current.leftButton.isPressed;
                CurrentFrame = default;
                return;
            }

            if (m_activeTouch == null && screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                    {
                        m_activeTouch = touch;
                        break;
                    }
                }
            }

            if (m_activeTouch != null)
            {
                var phase = m_activeTouch.phase.ReadValue();
                bool cancelled = phase == UnityEngine.InputSystem.TouchPhase.Canceled || phase == UnityEngine.InputSystem.TouchPhase.None;
                CurrentFrame = new BattleInputFrame
                {
                    PressDown = m_activeTouch.press.wasPressedThisFrame,
                    Pressing = m_activeTouch.press.isPressed,
                    PressUp = phase == UnityEngine.InputSystem.TouchPhase.Ended || m_activeTouch.press.wasReleasedThisFrame,
                    Cancelled = cancelled,
                    ScreenPosition = m_activeTouch.position.ReadValue()
                };
                if (CurrentFrame.PressUp || cancelled)
                {
                    m_activeTouch = null;
                }

                return;
            }

            if (touchHeld)
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
            m_activeTouch = null;
            m_waitForRelease = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    m_waitForRelease |= touch.press.isPressed;
                }
            }

            m_playerMap?.Enable();
        }

        public void OpenMenu()
        {
            m_activeTouch = null;
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
