using System.Collections.Generic;

namespace MainScene.BattlePart.Shikaku
{
    public static class ShikakuRules
    {
        public static bool IsGuard(E_ArmyType unit)
        {
            return unit == E_ArmyType.Infantry || unit == E_ArmyType.Spearman;
        }

        public static bool IsSupported(ShikakuLevelData level, ShikakuRect archer, IReadOnlyList<ShikakuRect> regions)
        {
            foreach (var other in regions)
            {
                if (!archer.SharesEdge(other))
                {
                    continue;
                }

                var order = level.clues.Find(x => other.Contains(x.cell));
                if (order != null && IsGuard(order.unit))
                {
                    return true;
                }
            }

            return false;
        }

        public static int SupportedArchers(ShikakuLevelData level, IReadOnlyList<ShikakuRect> regions)
        {
            int count = 0;
            foreach (var rect in regions)
            {
                var order = level.clues.Find(x => rect.Contains(x.cell));
                if (order != null && order.unit == E_ArmyType.Archer && IsSupported(level, rect, regions))
                {
                    count++;
                }
            }

            return count;
        }

        // 协同只参与最终成阵；中间布局可以暂缺护卫，允许自由部署与撤回。
        public static bool FormationSatisfied(ShikakuLevelData level, IReadOnlyList<ShikakuRect> regions)
        {
            return !level.requireArcherSupport || SupportedArchers(level, regions) == level.clues.FindAll(x => x.unit == E_ArmyType.Archer).Count;
        }

        public static string FormationProgress(ShikakuLevelData level, IReadOnlyList<ShikakuRect> regions)
        {
            return level.requireArcherSupport
                ? $"弓阵护卫 {SupportedArchers(level, regions)}/{level.clues.FindAll(x => x.unit == E_ArmyType.Archer).Count} · 弓阵需与步阵或枪阵共边，角接不算"
                : "操练：一阵一令，面积相符，覆盖全盘";
        }

        public static ShikakuRuleResult Validate(ShikakuLevelData level, ShikakuRect rect, IReadOnlyList<ShikakuRect> placed = null)
        {
            if (rect.top > rect.bottom || rect.left > rect.right || !level.InBounds(new GridCell(rect.top, rect.left)) || !level.InBounds(new GridCell(rect.bottom, rect.right)))
            {
                return ShikakuRuleResult.Error(E_ShikakuRuleFailure.OutsideBoard, "阵地超出棋盘范围。");
            }

            if (placed != null)
            {
                foreach (var other in placed)
                {
                    if (rect.Overlaps(other))
                    {
                        return ShikakuRuleResult.Error(E_ShikakuRuleFailure.Overlap, "不能与已布阵地重叠。");
                    }
                }
            }

            ArmyOrder clue = null;
            int count = 0;
            foreach (var c in level.clues)
            {
                if (rect.Contains(c.cell))
                {
                    clue = c;
                    count++;
                }
            }

            if (count == 0)
            {
                return ShikakuRuleResult.Error(E_ShikakuRuleFailure.NoClue, "每个军阵必须包含一道将令。");
            }

            if (count > 1)
            {
                return ShikakuRuleResult.Error(E_ShikakuRuleFailure.MultipleClues, "每个军阵只能包含一道将令。");
            }

            if (rect.Area != clue.area)
            {
                return ShikakuRuleResult.Error(E_ShikakuRuleFailure.AreaMismatch, $"将令要求 {clue.area} 格，当前框选 {rect.Area} 格。", clue.cell);
            }

            return ShikakuRuleResult.Ok();
        }
    }
}
