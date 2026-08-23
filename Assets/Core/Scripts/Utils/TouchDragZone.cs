using UnityEngine;
using UnityEngine.EventSystems;

namespace Core
{
    [RequireComponent(typeof(RectTransform))]
    public class TouchDragZone : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerClickHandler
    {
        [SerializeField]
        private float m_sensitivity = 1f;

        private RectTransform m_rectTransform;

        private Vector2 m_prevPointerPosition;
        private bool m_dragged;

        public event System.Action<Vector2> OnPointerDown;
        public event System.Action<Vector2, Vector2> OnPointerDrag;
        public event System.Action<Vector2> OnPointerClick;

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_prevPointerPosition = eventData.position;
            m_dragged = false;

            if (OnPointerDown != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint);
                OnPointerDown.Invoke(localPoint);
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Vector2 delta = eventData.position - m_prevPointerPosition;
            m_prevPointerPosition = eventData.position;
            m_dragged = true;

            if (OnPointerDrag != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint);
                OnPointerDrag.Invoke(localPoint, delta * m_sensitivity);
            }
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (m_dragged)
            {
                return;
            }

            if (OnPointerClick != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint);
                OnPointerClick.Invoke(localPoint);
            }
        }
    }
}
