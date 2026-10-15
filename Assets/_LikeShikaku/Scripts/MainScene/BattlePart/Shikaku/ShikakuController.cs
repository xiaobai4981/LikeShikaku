using Common.Level;
using Core;
using System.Collections.Generic;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    public class ShikakuController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform m_mapRoot;

        private MainSceneGameMode m_gameMode;
        private float m_blockSpacing;
        private List<ShikakuCell> m_shikakuCells;

        public void Init(MainSceneGameMode gameMode, LevelConfig levelConfig)
        {
            m_gameMode = gameMode;
            m_shikakuCells = new();

            // TODO 同步BattleSettings的内容
            m_blockSpacing = gameMode.BattleSettings.BlockSpacing;

            // 生成地图
            InitMap(levelConfig);
        }

        // TODO 当前只生成地图 每一个格子都一样,后续要根据配置来修改
        private void InitMap(LevelConfig levelConfig)
        {
            int rowCount = levelConfig.determinant.x;
            int colCount = levelConfig.determinant.y;
            var normalBlockPrefab = m_gameMode.BattleSettings.GetBlockByType(E_ShikakuCellType.Normal);

            // 先算起点
            float xPadding = (colCount % 2 == 0 ? -m_blockSpacing / 2 : 0)
                              - (colCount % 2 == 0? colCount / 2 - 1 : colCount / 2) * m_blockSpacing;
            float yPadding = (rowCount % 2 == 0 ? m_blockSpacing / 2 : 0)
                              + (rowCount % 2 == 0 ? rowCount / 2 - 1 : rowCount / 2) * m_blockSpacing;

            // 初始化地图,TODO 后续要改，这里只是为了生成地图
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    var pos = new Vector3
                        (xPadding + j * m_blockSpacing,
                         yPadding - i * m_blockSpacing, 0);

                    var normalBlock = PoolManager.Instance.GetGameObject
                        (normalBlockPrefab, Vector3.zero, Quaternion.identity);
                    normalBlock.transform.SetParent(m_mapRoot);
                    normalBlock.transform.position = pos;
                    m_shikakuCells.Add(normalBlock.GetComponent<ShikakuCell>());
                }
            }
        }

        public void ClearMap()
        {
            if (m_shikakuCells != null)
            {
                for (int i = 0; i < m_shikakuCells.Count; i++)
                {
                    PoolManager.Instance.ReleaseGameObject(m_shikakuCells[i].gameObject);
                }
                m_shikakuCells.Clear();
            }
        }

    }
}