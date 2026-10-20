
using MainScene.BattlePart.Input;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    /// <summary>
    /// 转换后的 Shikaku 输入
    /// </summary>
    public struct ShikakuInputFrame
    {
        public bool PressDown;
        public bool Pressing;
        public bool PressUp;

        public bool HasCell;
        public Vector2Int CellPosition;
    }

    /// <summary>
    /// 屏幕输入转棋盘格子坐标
    /// 不进行选择规则计算
    /// </summary>
    public class ShikakuInputTransform
    {
        private readonly Camera m_camera;
        private readonly Transform m_mapRoot;
        private readonly LayerMask m_cellLayer;

        public ShikakuInputTransform(Camera camera, Transform mapRoot, LayerMask cellLayer)
        {
            m_camera = camera;
            m_mapRoot = mapRoot;
            m_cellLayer = cellLayer;
        }

        public ShikakuInputFrame Resolve(BattleInputFrame input)
        {
            ShikakuInputFrame result = new()
            {
                PressDown = input.PressDown,
                Pressing = input.Pressing,
                PressUp = input.PressUp
            };

            if (!input.PressDown && !input.Pressing && !input.PressUp)
            {
                return result;
            }

            if (TryGetCell(input.ScreenPosition, out Vector2Int cellPosition))
            {
                result.HasCell = true;
                result.CellPosition = cellPosition;
            }

            return result;
        }

        /// <summary>
        /// 获取屏幕位置对应的场景格子
        /// </summary>
        private bool TryGetCell(Vector2 screenPos, out Vector2Int cellPosition)
        {
            cellPosition = default;

            if (m_camera == null || m_mapRoot == null)
                return false;

            Ray ray = m_camera.ScreenPointToRay(screenPos);

            // 棋盘位于世界 XY 平面
            Plane plane = new(Vector3.forward, m_mapRoot.position);

            if (!plane.Raycast(ray, out float distance))
                return false;

            Vector3 worldPos = ray.GetPoint(distance);

            Collider2D hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y), m_cellLayer);

            if (hit == null)
                return false;

            ShikakuCell cell = hit.GetComponentInParent<ShikakuCell>();

            if (cell == null)
                return false;

            // 不处理其他棋盘或场景物体
            if (!cell.transform.IsChildOf(m_mapRoot))
                return false;

            cellPosition = new Vector2Int(cell.Row, cell.Col);

            return true;
        }
    }
}
