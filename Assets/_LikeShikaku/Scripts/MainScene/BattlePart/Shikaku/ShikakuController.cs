using Common.Level;
using Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    public class ShikakuController : MonoBehaviour
    {
        [Header("场景引用")]
        [SerializeField]
        private Transform m_mapRoot;

        [SerializeField]
        private Camera m_camera;

        [SerializeField]
        private LayerMask m_cellLayer;

        [SerializeField]
        private TMP_Text m_tipText;

        [Header("颜色")]
        public Color normalColor = Color.white;
        public Color previewColor = Color.cyan;
        public Color successColor = Color.green;
        public Color errorColor = Color.red;

        private MainSceneGameMode m_gameMode;
        private float m_blockSpacing;

        private readonly List<ShikakuCell> m_shikakuCells = new List<ShikakuCell>();

        private int m_lastResultVersion = -1;

        public Camera GameCamera => m_camera;
        public Transform MapRoot => m_mapRoot;
        public LayerMask CellLayer => m_cellLayer;

        public void Init(MainSceneGameMode gameMode, LevelConfig levelConfig)
        {
            ClearMap();

            m_gameMode = gameMode;
            m_camera = Camera.main;

            m_blockSpacing = m_gameMode.BattleSettings.BlockSpacing;
            m_lastResultVersion = -1;

            if (m_tipText != null) m_tipText.text = "";

            InitMap(levelConfig);
        }

        private void InitMap(LevelConfig levelConfig)
        {
            if (m_mapRoot == null)
            {
                Debug.LogError("ShikakuController：MapRoot 未赋值。");
                return;
            }

            int rowCount = levelConfig.determinant.x;
            int colCount = levelConfig.determinant.y;

            var normalBlockPrefab = m_gameMode.BattleSettings.GetBlockByType(E_ShikakuCellType.Normal);

            float xPadding = -(colCount - 1) * m_blockSpacing * 0.5f;

            float yPadding = (rowCount - 1) * m_blockSpacing * 0.5f;

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    Vector3 localPosition = new Vector3(
                        xPadding + col * m_blockSpacing,
                        yPadding - row * m_blockSpacing,
                        0f);

                    GameObject block =
                        PoolManager.Instance.GetGameObject(
                            normalBlockPrefab,
                            Vector3.zero,
                            Quaternion.identity);

                    Transform blockTransform = block.transform;

                    blockTransform.SetParent(m_mapRoot, false);
                    blockTransform.localPosition = localPosition;
                    blockTransform.localRotation = Quaternion.identity;
                    blockTransform.localScale = normalBlockPrefab.transform.localScale;

                    ShikakuCell cell =
                        block.GetComponent<ShikakuCell>();

                    cell.Init(
                        row,
                        col,
                        normalColor,
                        previewColor,
                        successColor,
                        errorColor);

                    m_shikakuCells.Add(cell);
                }
            }

            Physics2D.SyncTransforms();
        }

        public void Sync(ShikakuModel model)
        {
            if (model == null)
                return;

            for (int i = 0; i < m_shikakuCells.Count; i++)
            {
                ShikakuCell cell = m_shikakuCells[i];

                E_ShikakuCellStatus status =
                    model.GetCellStatus(cell.Row, cell.Col);

                cell.ChangeStatus(status);
            }

            if (m_lastResultVersion != model.ResultVersion)
            {
                m_lastResultVersion = model.ResultVersion;
                SyncResult(model.LastResult);
            }
        }

        private void SyncResult(E_ShikakuSelectResult result)
        {
            string message;

            switch (result)
            {
                case E_ShikakuSelectResult.Success:
                    message = "矩形选择成功！";
                    break;

                case E_ShikakuSelectResult.NotSquare:
                    message = "构成正方形失败";
                    break;

                case E_ShikakuSelectResult.Overlap:
                    message = "不能与已完成区域重叠";
                    break;

                case E_ShikakuSelectResult.RegionCleared:
                    message = "已取消该区域";
                    break;

                default:
                    message = "";
                    break;
            }

            if (m_tipText != null)
                m_tipText.text = message;

            switch (result)
            {
                case E_ShikakuSelectResult.Success:
                case E_ShikakuSelectResult.RegionCleared:
                    Debug.Log(message);
                    break;

                case E_ShikakuSelectResult.NotSquare:
                case E_ShikakuSelectResult.Overlap:
                    Debug.LogWarning(message);
                    break;
            }
        }

        public void ClearMap()
        {
            for (int i = 0; i < m_shikakuCells.Count; i++)
            {
                ShikakuCell cell = m_shikakuCells[i];

                if (cell == null) continue;

                PoolManager.Instance.ReleaseGameObject(cell.gameObject);
            }

            m_shikakuCells.Clear();
            m_lastResultVersion = -1;

            if (m_tipText != null) m_tipText.text = "";
        }
    }
}