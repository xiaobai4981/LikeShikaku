using Common.Level;
using Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    public class ShikakuController : MonoBehaviour
    {
        // 场景引用沿用原字段。
        [SerializeField]
        private Transform m_mapRoot;
        [SerializeField]
        private Camera m_camera;
        [SerializeField]
        private LayerMask m_cellLayer;
        [SerializeField]
        private TMP_Text m_tipText = null;
        public Color normalColor = Color.white;
        public Color previewColor = Color.cyan;
        public Color successColor = Color.green;
        public Color errorColor = Color.red;
        private MainSceneGameMode m_gameMode;
        private readonly List<ShikakuCell> m_shikakuCells = new List<ShikakuCell>();
        private readonly List<LineRenderer> m_lines = new List<LineRenderer>();
        private float m_blockSpacing;
        private ShikakuLevelData m_level;
        private int m_drawnRevision = -1;
        private int m_lastResultVersion = -1;
        private Rect m_oldRect;
        private float m_oldSize;
        private Vector3 m_oldCameraPosition;
        private bool m_hasCameraBackup;
        private Material m_lineMaterial;
        private Vector2 m_screenSize;
        public Camera GameCamera => m_camera;
        public Transform MapRoot => m_mapRoot;
        public LayerMask CellLayer => m_cellLayer;

        public void Init(MainSceneGameMode gameMode, LevelConfig levelConfig)
        {
            Init(gameMode, levelConfig.ToData());
        }

        public void Init(MainSceneGameMode gameMode, ShikakuLevelData data)
        {
            ClearMap();
            m_gameMode = gameMode;
            m_level = data;
            if (!m_camera)
            {
                m_camera = Camera.main;
            }

            if (!m_camera)
            {
                throw new InvalidOperationException("ShikakuController：请配置棋盘相机。");
            }

            if (!m_mapRoot)
            {
                var root = new GameObject("ShikakuMapRoot");
                root.transform.SetParent(transform, false);
                m_mapRoot = root.transform;
            }

            if (!gameMode.BattleSettings)
            {
                throw new InvalidOperationException("MainSceneGameMode：BattleSettings 未赋值。");
            }

            m_blockSpacing = Mathf.Max(.1f, gameMode.BattleSettings.BlockSpacing);
            if (m_cellLayer.value == 0)
            {
                m_cellLayer = 1;
            }

            var prefab = gameMode.BattleSettings.GetBlockByType(E_ShikakuCellType.Normal);
            if (!prefab || !prefab.GetComponent<ShikakuCell>())
            {
                throw new InvalidOperationException("BattleSettings 需要配置含 ShikakuCell 的 Normal Prefab。");
            }

            if (PoolManager.Instance == null)
            {
                throw new InvalidOperationException("请从原 BootScene 启动，先初始化 PoolManager。");
            }

            int layer = 0;
            while ((m_cellLayer.value & (1 << layer)) == 0 && layer < 31)
            {
                layer++;
            }

            for (int row = 0; row < data.rows; row++)
            {
                for (int col = 0; col < data.cols; col++)
                {
                    var chosen = prefab;
                    var obj = PoolManager.Instance.GetGameObject(chosen, Vector3.zero, Quaternion.identity);
                    if (!obj)
                    {
                        throw new InvalidOperationException("棋盘对象池分配失败，请检查实例数量上限。");
                    }

                    obj.transform.SetParent(m_mapRoot, false);
                    obj.transform.localPosition = CellLocal(row, col);
                    obj.transform.localRotation = Quaternion.identity;
                    obj.transform.localScale = chosen.transform.localScale;
                    obj.layer = layer;
                    var cell = obj.GetComponent<ShikakuCell>();
                    if (!cell)
                    {
                        PoolManager.Instance.ReleaseGameObject(obj);
                        throw new InvalidOperationException("格子 Prefab 缺少 ShikakuCell。");
                    }

                    m_shikakuCells.Add(cell);
                    cell.Init(row, col, normalColor, previewColor, successColor, errorColor);
                    float fraction = 1 - Mathf.Clamp(gameMode.BattleSettings.CellGapPercent, 0, 40) / 100f;
                    cell.Configure(data, gameMode.BattleSettings, new Vector2(m_blockSpacing * fraction / Mathf.Max(.001f, Mathf.Abs(obj.transform.localScale.x)), m_blockSpacing * fraction / Mathf.Max(.001f, Mathf.Abs(obj.transform.localScale.y))));
                }
            }

            Physics2D.SyncTransforms();
            FitCamera();
            if (m_tipText)
            {
                m_tipText.text = data.briefing;
            }
        }

        private Vector3 CellLocal(int row, int col)
        {
            return new Vector3((col - (m_level.cols - 1) * .5f) * m_blockSpacing, ((m_level.rows - 1) * .5f - row) * m_blockSpacing, 0);
        }

        public void Sync(ShikakuModel model)
        {
            if (model == null)
            {
                return;
            }

            if (m_screenSize != new Vector2(Screen.width, Screen.height))
            {
                FitCamera();
            }

            foreach (var cell in m_shikakuCells)
            {
                cell.Sync(model);
            }

            if (m_drawnRevision != model.Revision)
            {
                m_drawnRevision = model.Revision;
                DrawBorders(model);
            }

            if (m_tipText)
            {
                if (model.IsSelecting)
                {
                    m_tipText.text = $"当前试划 {Mathf.Abs(model.EndCell.x - model.StartCell.x) + 1} 行 × {Mathf.Abs(model.EndCell.y - model.StartCell.y) + 1} 列";
                }
                else
                {
                    m_tipText.text = model.LastMessage;
                }
            }

            m_lastResultVersion = model.ResultVersion;
        }

        private void FitCamera()
        {
            m_screenSize = new Vector2(Screen.width, Screen.height);
            var settings = m_gameMode.BattleSettings;
            if (!settings.AutoFitCamera || !m_camera.orthographic)
            {
                return;
            }

            if (!m_hasCameraBackup)
            {
                m_oldRect = m_camera.rect;
                m_oldSize = m_camera.orthographicSize;
                m_oldCameraPosition = m_camera.transform.position;
                m_hasCameraBackup = true;
            }

            float height = Mathf.Clamp(settings.BoardViewportHeight, .35f, .9f);
            // 保持全屏清屏，通过相机中心偏移为下方界面留位置，避免弹窗关闭后残影。
            m_camera.rect = new Rect(0, 0, 1, 1);
            var bounds = new Bounds(m_mapRoot.TransformPoint(Vector3.zero), Vector3.zero);
            foreach (float x in new[]
            {
                -m_level.cols * m_blockSpacing * .5f,
                m_level.cols * m_blockSpacing * .5f
            }

            )
            {
                foreach (float y in new[]
                {
                    -m_level.rows * m_blockSpacing * .5f,
                    m_level.rows * m_blockSpacing * .5f
                }

                )
                {
                    bounds.Encapsulate(m_mapRoot.TransformPoint(new Vector3(x, y, 0)));
                }
            }

            float aspect = Mathf.Max(.1f, Screen.width / Mathf.Max(1f, Screen.height));
            m_camera.orthographicSize = Mathf.Max(bounds.extents.y / height, bounds.extents.x / aspect) + Mathf.Max(0, settings.CameraPadding);
            var p = m_camera.transform.position;
            m_camera.transform.position = new Vector3(bounds.center.x, bounds.center.y - (1 - height) * m_camera.orthographicSize, p.z);
        }

        private void DrawBorders(ShikakuModel model)
        {
            int used = 0;
            foreach (var rect in model.Regions)
            {
                var topLeft = CellLocal(rect.top, rect.left) + new Vector3(-m_blockSpacing * .49f, m_blockSpacing * .49f, -.1f);
                var bottomRight = CellLocal(rect.bottom, rect.right) + new Vector3(m_blockSpacing * .49f, -m_blockSpacing * .49f, -.1f);
                DrawLine(ref used, new[] { topLeft, new Vector3(bottomRight.x, topLeft.y, -.1f), bottomRight, new Vector3(topLeft.x, bottomRight.y, -.1f), topLeft }, .045f, new Color(.2f, .17f, .12f));
            }


            // 只显示已经成立的接应，不提前透露未部署军阵的边界。
            if (m_level.requireArcherSupport)
            {
                foreach (var archer in model.Regions)
                {
                    var order = m_level.clues.Find(x => archer.Contains(x.cell));
                    if (order.unit != E_ArmyType.Archer)
                    {
                        continue;
                    }

                    foreach (var guard in model.Regions)
                    {
                        var guardOrder = m_level.clues.Find(x => guard.Contains(x.cell));
                        if (ShikakuRules.IsGuard(guardOrder.unit) && archer.SharesEdge(guard))
                        {
                            var a = (CellLocal(archer.top, archer.left) + CellLocal(archer.bottom, archer.right)) * .5f;
                            var b = (CellLocal(guard.top, guard.left) + CellLocal(guard.bottom, guard.right)) * .5f;
                            a.z = b.z = -.12f;
                            DrawLine(ref used, new[] { a, b }, .025f, new Color(.12f, .65f, .68f));
                        }
                    }
                }
            }

            for (int i = used; i < m_lines.Count; i++)
            {
                m_lines[i].gameObject.SetActive(false);
            }
        }

        private void DrawLine(ref int used, Vector3[] points, float width, Color color)
        {
            LineRenderer line;
            if (used < m_lines.Count)
            {
                line = m_lines[used];
            }
            else
            {
                var obj = new GameObject("FormationBorder");
                obj.transform.SetParent(m_mapRoot, false);
                line = obj.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                if (!m_lineMaterial)
                {
                    m_lineMaterial = new Material(Shader.Find("Sprites/Default"));
                }

                line.sharedMaterial = m_lineMaterial;
                var renderer = m_shikakuCells[0].GetComponent<SpriteRenderer>();
                line.sortingLayerID = renderer.sortingLayerID;
                line.sortingOrder = renderer.sortingOrder + 30;
                m_lines.Add(line);
            }

            used++;
            line.gameObject.SetActive(true);
            line.startWidth = line.endWidth = m_blockSpacing * width;
            line.startColor = line.endColor = color;
            line.positionCount = points.Length;
            line.SetPositions(points);
        }

        public void ClearMap()
        {
            foreach (var cell in m_shikakuCells)
            {
                if (cell)
                {
                    if (PoolManager.Instance != null)
                    {
                        PoolManager.Instance.ReleaseGameObject(cell.gameObject);
                    }
                    else
                    {
                        Destroy(cell.gameObject);
                    }
                }
            }

            m_shikakuCells.Clear();
            foreach (var line in m_lines)
            {
                if (line)
                {
                    line.gameObject.SetActive(false);
                    Destroy(line.gameObject);
                }
            }

            m_lines.Clear();
            m_drawnRevision = m_lastResultVersion = -1;
            if (m_lineMaterial)
            {
                Destroy(m_lineMaterial);
                m_lineMaterial = null;
            }

            if (m_hasCameraBackup && m_camera)
            {
                m_camera.rect = m_oldRect;
                m_camera.orthographicSize = m_oldSize;
                m_camera.transform.position = m_oldCameraPosition;
            }

            m_hasCameraBackup = false;
            if (m_tipText)
            {
                m_tipText.text = "";
            }
        }
    }
}
