// Only for offline tests. Never copy Verification into Unity's Assets folder.
namespace UnityEngine
{
    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}

namespace MainScene.BattlePart.Shikaku
{
    public struct ShikakuInputFrame
    {
        public bool PressDown, Pressing, PressUp, HasCell;
        public UnityEngine.Vector2Int CellPosition;
    }
}
