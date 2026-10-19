using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    // TODO 单个方块表现管理
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ShikakuCell : MonoBehaviour
    {
        // 标记当前方块状态
        public E_ShikakuCellStatus NowStatus {  get; private set; }
        public int row;
        public int col;

        public void ChangeStatus(E_ShikakuCellStatus cellStatus)
        {
            NowStatus = cellStatus;
            SetStatus(cellStatus);
        }

        // TODO 改变状态，附带部分表现
        private void SetStatus(E_ShikakuCellStatus cellStatus)
        {

        }
    }
}