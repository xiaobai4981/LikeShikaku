using System;
using System.Collections.Generic;

namespace MainScene.BattlePart.Shikaku
{
    public sealed class ShikakuSolveResult
    {
        public readonly List<List<ShikakuRect>> solutions = new List<List<ShikakuRect>>();
        public bool budgetExceeded;
        public int visitedNodes;
    }

    // 精确覆盖搜索：优先处理可选矩形最少的未覆盖格。
    public static class ShikakuSolver
    {
        public static ShikakuSolveResult Solve(ShikakuLevelData level, IReadOnlyList<ShikakuRect> placed = null, int solutionLimit = 2, int nodeBudget = 100000)
        {
            if (level.Inspect().Count > 0)
            {
                throw new ArgumentException("请先修复关卡配置。");
            }

            var result = new ShikakuSolveResult();
            if (solutionLimit < 1 || nodeBudget < 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            var occupied = new bool[level.rows * level.cols];

            var selected = new List<ShikakuRect>();
            Action<ShikakuRect, bool> mark = (rect, value) =>
            {
                for (int r = rect.top; r <= rect.bottom; r++)
                {
                    for (int c = rect.left; c <= rect.right; c++)
                    {
                        occupied[r * level.cols + c] = value;
                    }
                }
            };
            if (placed != null)
            {
                foreach (var rect in placed)
                {
                    if (!ShikakuRules.Validate(level, rect, selected).IsValid)
                    {
                        return result;
                    }

                    selected.Add(rect);
                    mark(rect, true);
                }
            }

            var byCell = new List<ShikakuRect>[occupied.Length];
            for (int i = 0; i < byCell.Length; i++)
            {
                byCell[i] = new List<ShikakuRect>();
            }

            foreach (var clue in level.clues)
            {
                if (selected.Exists(r => r.Contains(clue.cell)))
                {
                    continue;
                }

                for (int height = 1; height <= level.rows; height++)
                {
                    if (clue.area % height != 0)
                    {
                        continue;
                    }

                    int width = clue.area / height;
                    if (width > level.cols)
                    {
                        continue;
                    }

                    for (int top = Math.Max(0, clue.cell.row - height + 1); top <= Math.Min(clue.cell.row, level.rows - height); top++)
                    {
                        for (int left = Math.Max(0, clue.cell.col - width + 1); left <= Math.Min(clue.cell.col, level.cols - width); left++)
                        {
                            var rect = new ShikakuRect(top, top + height - 1, left, left + width - 1);
                            if (!ShikakuRules.Validate(level, rect, selected).IsValid)
                            {
                                continue;
                            }

                            for (int r = rect.top; r <= rect.bottom; r++)
                            {
                                for (int c = rect.left; c <= rect.right; c++)
                                {
                                    byCell[r * level.cols + c].Add(rect);
                                }
                            }
                        }
                    }
                }
            }

            Func<ShikakuRect, bool> free = rect =>
            {
                for (int r = rect.top; r <= rect.bottom; r++)
                {
                    for (int c = rect.left; c <= rect.right; c++)
                    {
                        if (occupied[r * level.cols + c])
                        {
                            return false;
                        }
                    }
                }

                return true;
            };
            Action search = null;
            search = () =>
            {
                if (result.solutions.Count >= solutionLimit || result.budgetExceeded)
                {
                    return;
                }

                if (++result.visitedNodes > nodeBudget)
                {
                    result.budgetExceeded = true;
                    return;
                }

                List<ShikakuRect> choices = null;
                for (int i = 0; i < occupied.Length; i++)
                {
                    if (occupied[i])
                    {
                        continue;
                    }

                    var available = byCell[i].FindAll(rect => free(rect));
                    if (available.Count == 0)
                    {
                        return;
                    }

                    if (choices == null || available.Count < choices.Count)
                    {
                        choices = available;
                    }

                    if (available.Count == 1)
                    {
                        break;
                    }
                }

                if (choices == null)
                {
                    if (ShikakuRules.FormationSatisfied(level, selected))
                    {
                        result.solutions.Add(new List<ShikakuRect>(selected));
                    }
                    return;
                }

                foreach (var rect in choices)
                {
                    selected.Add(rect);
                    mark(rect, true);
                    search();
                    mark(rect, false);
                    selected.RemoveAt(selected.Count - 1);
                    if (result.solutions.Count >= solutionLimit || result.budgetExceeded)
                    {
                        return;
                    }
                }
            };
            search();
            return result;
        }
    }
}
