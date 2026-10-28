using System;
using System.Collections.Generic;

namespace MainScene.BattlePart.Shikaku
{
    public enum E_ArmyType
    {
        Any,
        Infantry,
        Spearman,
        Archer,
        Cavalry
    }

    // 游戏中的八种布局主题，不代表历史阵法复原或固定答案。
    public enum E_FormationTheme
    {
        操练,
        中军,
        两翼,
        纵深,
        环护,
        交错,
        雁行,
        合围,
        连营
    }

    public enum E_ShikakuRuleFailure
    {
        None = 0,
        OutsideBoard = 1,
        Overlap = 4,
        NoClue = 5,
        MultipleClues = 6,
        AreaMismatch = 7,
        SessionClosed = 11
    }

    public enum E_ShikakuPlayStatus
    {
        Playing,
        Paused,
        Won,
        Lost
    }

    public enum E_TutorialAction
    {
        Deploy,
        Remove,
        InvalidSubmission
    }

    [Serializable]
    public struct GridCell : IEquatable<GridCell>
    {
        public int row, col;
        public GridCell(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public bool Equals(GridCell other)
        {
            return row == other.row && col == other.col;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return row * 397 ^ col;
        }
    }

    [Serializable]
    public struct ShikakuRect : IEquatable<ShikakuRect>
    {
        public int top, bottom, left, right;
        public int Width => right - left + 1;
        public int Height => bottom - top + 1;
        public int Area => Width * Height;

        public ShikakuRect(int top, int bottom, int left, int right)
        {
            this.top = top;
            this.bottom = bottom;
            this.left = left;
            this.right = right;
        }

        public static ShikakuRect Between(GridCell a, GridCell b)
        {
            return new ShikakuRect(Math.Min(a.row, b.row), Math.Max(a.row, b.row), Math.Min(a.col, b.col), Math.Max(a.col, b.col));
        }

        public bool Contains(GridCell c)
        {
            return c.row >= top && c.row <= bottom && c.col >= left && c.col <= right;
        }

        public bool Overlaps(ShikakuRect b)
        {
            return top <= b.bottom && bottom >= b.top && left <= b.right && right >= b.left;
        }

        public bool SharesEdge(ShikakuRect other)
        {
            return ((right + 1 == other.left || other.right + 1 == left) && Math.Max(top, other.top) <= Math.Min(bottom, other.bottom))
                || ((bottom + 1 == other.top || other.bottom + 1 == top) && Math.Max(left, other.left) <= Math.Min(right, other.right));
        }

        public bool Equals(ShikakuRect b)
        {
            return top == b.top && bottom == b.bottom && left == b.left && right == b.right;
        }

        public override bool Equals(object obj)
        {
            return obj is ShikakuRect b && Equals(b);
        }

        public override int GetHashCode()
        {
            return ((top * 397 ^ bottom) * 397 ^ left) * 397 ^ right;
        }
    }

    [Serializable]
    public class ArmyOrder
    {
        public GridCell cell;
        public int area;
        public E_ArmyType unit = E_ArmyType.Infantry;
    }

    [Serializable]
    public class TutorialStep
    {
        public string text;
        public E_TutorialAction action;
        public bool hasTarget;
        public GridCell target;
        public E_ShikakuRuleFailure expectedFailure;
    }

    [Serializable]
    public class ShikakuLevelData
    {
        public const int MaxBoardSize = 32;
        public string id;
        public string title;
        public string briefing;
        public E_FormationTheme formation;
        public bool requireArcherSupport;
        public int rows = 5;
        public int cols = 5;
        // 0 为不限次数，教学步骤不消耗容错。
        public int maximumErrors;
        public List<ArmyOrder> clues = new List<ArmyOrder>();
        public List<TutorialStep> lessons = new List<TutorialStep>();
        public int AvailableArea => rows * cols;

        public bool InBounds(GridCell c)
        {
            return c.row >= 0 && c.row < rows && c.col >= 0 && c.col < cols;
        }

        public ArmyOrder ClueAt(GridCell c)
        {
            return clues.Find(x => x.cell.Equals(c));
        }

        public List<string> Inspect()
        {
            var errors = new List<string>();
            if (rows < 1 || cols < 1 || rows > MaxBoardSize || cols > MaxBoardSize)
            {
                errors.Add($"棋盘行列必须在 1～{MaxBoardSize} 之间。");
            }

            if (maximumErrors < 0)
            {
                errors.Add("容错次数不能为负数。");
            }

            if (formation < E_FormationTheme.操练 || formation > E_FormationTheme.连营)
            {
                errors.Add("阵法主题不合法。");
            }

            if (clues == null || lessons == null)
            {
                errors.Add("配置列表不能为 null。");
                return errors;
            }

            var cells = new HashSet<GridCell>();
            long sum = 0;
            foreach (var clue in clues)
            {
                if (clue == null)
                {
                    errors.Add("将令不能为 null。");
                    continue;
                }

                if (!InBounds(clue.cell) || !cells.Add(clue.cell))
                {
                    errors.Add("将令越界或重复。");
                }

                if (clue.area < 1 || clue.area > rows * cols)
                {
                    errors.Add("将令面积不合法。");
                }

                if (clue.unit < E_ArmyType.Infantry || clue.unit > E_ArmyType.Cavalry)
                {
                    errors.Add("将令必须指定有效兵种。");
                }

                sum += clue.area;
            }

            if (sum != rows * cols)
            {
                errors.Add("将令数字之和必须等于可用格数。");
            }

            if (requireArcherSupport && (!clues.Exists(x => x != null && x.unit == E_ArmyType.Archer) || !clues.Exists(x => x != null && (x.unit == E_ArmyType.Infantry || x.unit == E_ArmyType.Spearman))))
            {
                errors.Add("弓阵护卫关卡需要至少一道弓兵将令和一道步兵或枪兵将令。");
            }

            foreach (var step in lessons)
            {
                if (step == null || (step.hasTarget && !InBounds(step.target)))
                {
                    errors.Add("教学目标不合法。");
                }
            }

            return errors;
        }

        // 规则指纹：防止关卡内容变化后恢复不兼容的局面。
        public string Fingerprint()
        {
            var text = new System.Text.StringBuilder().Append("formation-v3|").Append(id).Append('|').Append(rows).Append('|').Append(cols).Append('|').Append(maximumErrors).Append('|').Append((int)formation).Append('|').Append(requireArcherSupport);
            foreach (var c in clues)
            {
                text.Append($"|C{c.cell.row},{c.cell.col},{c.area},{(int)c.unit}");
            }

            foreach (var s in lessons)
            {
                text.Append($"|L{(int)s.action},{s.hasTarget},{s.target.row},{s.target.col},{(int)s.expectedFailure}");
            }

            uint hash = 2166136261;
            foreach (char ch in text.ToString())
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return hash.ToString("X8");
        }
    }

    public sealed class ShikakuRuleResult
    {
        public E_ShikakuRuleFailure failure;
        public string message;
        public GridCell? conflict;
        public bool IsValid => failure == E_ShikakuRuleFailure.None;

        public static ShikakuRuleResult Ok()
        {
            return new ShikakuRuleResult
            {
                message = "阵地合法，部队就位。"
            };
        }

        public static ShikakuRuleResult Error(E_ShikakuRuleFailure failure, string message, GridCell? cell = null)
        {
            return new ShikakuRuleResult
            {
                failure = failure,
                message = message,
                conflict = cell
            };
        }
    }

    public static class ShikakuNames
    {
        public static string FormationIdea(E_FormationTheme formation)
        {
            switch (formation)
            {
                case E_FormationTheme.中军: return "中军居中，四面接应；先看中腹与外沿如何衔接。";
                case E_FormationTheme.两翼: return "两翼展开，中路策应；比较左右军阵的接应空间。";
                case E_FormationTheme.纵深: return "前后分层，留有纵深；让前列与后阵相互接应。";
                case E_FormationTheme.环护: return "层层环护，内外相援；从外围追踪到中腹的接应。";
                case E_FormationTheme.交错: return "长短交错，彼此咬合；一处伸缩会改变邻阵的选择。";
                case E_FormationTheme.雁行: return "错列相随，逐层推进；沿阶梯寻找可接应的军阵。";
                case E_FormationTheme.合围: return "多路合围，内外呼应；统筹两侧与中路的护卫。";
                case E_FormationTheme.连营: return "诸营相连，前后相援；把分散将令连成协同网络。";
                default: return "校场操练：熟悉将令、格数与撤回。";
            }
        }

        public static string Unit(E_ArmyType unit)
        {
            switch (unit)
            {
                case E_ArmyType.Infantry:
                    return "步";
                case E_ArmyType.Spearman:
                    return "枪";
                case E_ArmyType.Archer:
                    return "弓";
                case E_ArmyType.Cavalry:
                    return "骑";
                default:
                    return "任意";
            }
        }

    }
}
