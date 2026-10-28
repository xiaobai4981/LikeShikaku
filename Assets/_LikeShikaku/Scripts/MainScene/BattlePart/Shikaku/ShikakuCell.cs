using MainScene.BattlePart.Configs;
using TMPro;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    [RequireComponent(typeof(SpriteRenderer)), RequireComponent(typeof(BoxCollider2D))]
    public class ShikakuCell : MonoBehaviour
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public E_ShikakuCellStatus NowStatus { get; private set; }

        private SpriteRenderer m_original;
        private SpriteRenderer m_tile;
        private SpriteRenderer m_soldier;
        private SpriteRenderer m_flag;
        private TMP_Text m_orderText;
        private TMP_Text m_targetText;
        private Color m_normal;
        private Color m_preview;
        private Color m_defaultPreview;
        private Color m_success;
        private Color m_error;
        private Vector2 m_size;
        private BattleSettings m_settings;
        private Color m_deployedColor;
        private int m_lastRevision = -1;
        public void Init(int row, int col, Color normalColor, Color previewColor, Color successColor, Color errorColor)
        {
            Row = row;
            Col = col;
            m_normal = normalColor;
            m_preview = previewColor;
            m_defaultPreview = previewColor;
            m_success = successColor;
            m_deployedColor = successColor;
            m_lastRevision = -1;
            m_error = errorColor;
            if (!m_original)
            {
                m_original = GetComponent<SpriteRenderer>();
            }

            if (!m_tile)
            {
                m_tile = MakeSprite("Ground", 0);
                m_tile.sprite = m_original.sprite;
                m_soldier = MakeSprite("Army", 3);
                m_flag = MakeSprite("OrderFlag", 4);
                m_orderText = MakeText("Order", 10);
                m_targetText = MakeText("Teaching", 13);
            }

            m_original.enabled = false;
            m_soldier.enabled = m_flag.enabled = false;
            m_soldier.color = m_flag.color = Color.white;
            m_orderText.color = new Color(.1f, .08f, .04f);
            m_orderText.text = m_targetText.text = "";
            NowStatus = E_ShikakuCellStatus.Normal;
            SetStatus(NowStatus);
        }

        public void Configure(ShikakuLevelData data, BattleSettings battleSettings, Vector2 localSize)
        {
            m_settings = battleSettings;
            m_size = localSize;
            m_tile.sprite = m_settings.groundSprite ? m_settings.groundSprite : m_original.sprite;
            Fit(m_tile, m_size.x, m_size.y);
            var collider = GetComponent<BoxCollider2D>();
            collider.offset = Vector2.zero;
            collider.size = m_size;
            foreach (var text in new[]
            {
                m_orderText,
                m_targetText
            }

            )
            {
                if (m_settings.boardFont)
                {
                    text.font = m_settings.boardFont;
                }

                text.rectTransform.sizeDelta = new Vector2(m_size.x * .92f, m_size.y * .42f);
            }

            m_orderText.alignment = TextAlignmentOptions.Center;
            // 将令占据上方主体空间，统一深色粗体；兵种颜色由旗帜和阵框表达。
            m_orderText.rectTransform.sizeDelta = new Vector2(m_size.x * .94f, m_size.y * .5f);
            m_orderText.transform.localPosition = new Vector3(0, m_size.y * .23f, -.03f);
            m_orderText.fontStyle = FontStyles.Bold;
            m_orderText.fontSize = 24;
            m_orderText.fontSizeMin = 6;
            m_orderText.fontSizeMax = 24;
            m_orderText.margin = Vector4.zero;
            m_orderText.color = new Color32(22, 24, 28, 255);
            m_targetText.alignment = TextAlignmentOptions.BottomLeft;
            m_targetText.transform.localPosition = new Vector3(0, -m_size.y * .25f, -.05f);
            m_targetText.color = new Color(1, .8f, .1f);
            var clue = data.ClueAt(new GridCell(Row, Col));
            if (clue != null)
            {
                string unit = ShikakuNames.Unit(clue.unit);
                if (!m_orderText.font || !m_orderText.font.HasCharacter(unit[0]))
                {
                    unit = Code(clue.unit);
                }

                m_orderText.text = unit + " " + clue.area;
                m_flag.color = m_settings.UnitColor(clue.unit);
                m_flag.sprite = m_settings.Flag(clue.unit);
                m_flag.enabled = m_flag.sprite;
                Fit(m_flag, m_size.x * .28f, m_size.y * .45f);
                m_flag.transform.localPosition = new Vector3(-m_size.x * .32f, -m_size.y * .23f, -.02f);
            }

            ChangeStatus(E_ShikakuCellStatus.Normal);
        }

        public void Sync(ShikakuModel model, Color? selectionColor = null)
        {
            m_preview = selectionColor.HasValue ? Color.Lerp(Color.white, selectionColor.Value, .45f) : m_defaultPreview;
            if (m_lastRevision != model.Revision)
            {
                m_lastRevision = model.Revision;
                int region = model.RegionAt(new GridCell(Row, Col));
                Sprite sprite = null;
                m_deployedColor = m_success;
                m_soldier.color = Color.white;
                if (region >= 0)
                {
                    var rect = model.Regions[region];
                    var clue = model.Level.clues.Find(x => rect.Contains(x.cell));
                    var armyColor = m_settings.UnitColor(clue.unit);
                    sprite = m_settings.Unit(clue.unit);
                    m_deployedColor = Color.Lerp(Color.white, armyColor, .38f);
                    m_soldier.color = Color.Lerp(Color.white, armyColor, .25f);
                }

                if (m_soldier.sprite != sprite)
                {
                    m_soldier.sprite = sprite;
                    Fit(m_soldier, m_size.x * .62f, m_size.y * .5f);
                }

                m_soldier.enabled = sprite;
                m_soldier.transform.localPosition = new Vector3(m_size.x * .1f, -m_size.y * .22f, -.02f);
            }

            ChangeStatus(model.GetCellStatus(Row, Col));
            bool teaching = model.InLesson && model.CurrentLesson.hasTarget && model.CurrentLesson.target.Equals(new GridCell(Row, Col));
            m_targetText.text = teaching ? ">" : "";
        }

        public void ChangeStatus(E_ShikakuCellStatus status)
        {
            NowStatus = status;
            SetStatus(status);
        }

        private void SetStatus(E_ShikakuCellStatus status)
        {
            if (!m_tile)
            {
                return;
            }

            switch (status)
            {
                case E_ShikakuCellStatus.Preview:
                    m_tile.color = m_preview;
                    break;
                case E_ShikakuCellStatus.Success:
                    m_tile.color = m_deployedColor;
                    break;
                case E_ShikakuCellStatus.Error:
                    m_tile.color = m_error;
                    break;
                case E_ShikakuCellStatus.Hint:
                    m_tile.color = new Color(.65f, .8f, 1);
                    break;
                default:
                    m_tile.color = m_normal;
                    break;
            }
        }

        private SpriteRenderer MakeSprite(string name, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = m_original.sortingLayerID;
            renderer.sortingOrder = m_original.sortingOrder + order;
            renderer.sharedMaterial = m_original.sharedMaterial;
            return renderer;
        }

        private TMP_Text MakeText(string name, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var text = obj.AddComponent<TextMeshPro>();
            text.fontSize = 5;
            text.enableAutoSizing = true;
            text.fontSizeMin = .5f;
            text.fontSizeMax = 8;
            text.color = new Color(.1f, .08f, .04f);
            text.richText = false;
            text.enableWordWrapping = false;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sortingLayerID = m_original.sortingLayerID;
            renderer.sortingOrder = m_original.sortingOrder + order;
            return text;
        }

        private static void Fit(SpriteRenderer renderer, float width, float height)
        {
            if (!renderer || !renderer.sprite)
            {
                return;
            }

            var b = renderer.sprite.bounds.size;
            float scale = Mathf.Min(width / Mathf.Max(.001f, b.x), height / Mathf.Max(.001f, b.y));
            renderer.transform.localScale = Vector3.one * scale;
        }

        private static string Code(E_ArmyType type)
        {
            switch (type)
            {
                case E_ArmyType.Infantry:
                    return "I";
                case E_ArmyType.Spearman:
                    return "S";
                case E_ArmyType.Archer:
                    return "A";
                default:
                    return "C";
            }
        }
    }
}
