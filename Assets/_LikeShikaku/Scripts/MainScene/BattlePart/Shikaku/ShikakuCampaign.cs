using System.Collections.Generic;

namespace MainScene.BattlePart.Shikaku
{
    public static class ShikakuCampaign
    {
        public static List<ShikakuLevelData> CreateDefault()
        {
            var levels = new List<ShikakuLevelData>();
            for (int number = 1; number <= 20; number++)
            {
                levels.Add(Create(number));
            }

            return levels;
        }

        private static ShikakuLevelData Create(int number)
        {
            var level = new ShikakuLevelData
            {
                id = "training-" + number.ToString("D2"),
                title = $"第 {number} 关 · " + Title(number),
                briefing = "依将令列阵：一阵一令，格数相符，覆盖全部可用地。",
                maximumErrors = number >= 19 ? 3 : 0
            };
            var solution = new List<ShikakuRect>();
            if (number == 1)
            {
                level.rows = 2;
                level.cols = 3;
                solution.Add(new ShikakuRect(0, 0, 0, 2));
                solution.Add(new ShikakuRect(1, 1, 0, 2));
            }
            else if (number == 2 || number == 4)
            {
                level.rows = 3;
                level.cols = 4;
                solution.Add(new ShikakuRect(0, 1, 0, 2));
                solution.Add(new ShikakuRect(2, 2, 0, 2));
                solution.Add(new ShikakuRect(0, 2, 3, 3));
            }
            else if (number == 3)
            {
                level.rows = level.cols = 3;
                for (int r = 0; r < 3; r++)
                {
                    solution.Add(new ShikakuRect(r, r, 0, 2));
                }
            }
            else if (number <= 6)
            {
                level.rows = 4;
                level.cols = number == 5 ? 4 : 5;
                solution.Add(new ShikakuRect(0, 1, 0, 1));
                solution.Add(new ShikakuRect(0, 1, 2, level.cols - 1));
                solution.Add(new ShikakuRect(2, 3, 0, 1));
                solution.Add(new ShikakuRect(2, 3, 2, level.cols - 1));
            }
            else
            {
                level.formation = (E_FormationTheme)(1 + (number - 7) % 8);
                level.requireArcherSupport = true;
                solution = CreateFormation(level, number >= 15);
                level.title = $"第 {number} 关 · {level.formation}阵" + (number >= 15 ? " · 变阵" : "");
                level.briefing = ShikakuNames.FormationIdea(level.formation) + " 弓阵须与步阵或枪阵共边；任选护卫，不预设军阵边界。";
            }

            for (int i = 0; i < solution.Count; i++)
            {
                var r = solution[i];
                var unit = i % 3 == 0 ? E_ArmyType.Infantry : i % 3 == 1 ? E_ArmyType.Spearman : E_ArmyType.Cavalry;
                if (i == 1 || (number >= 15 && i == solution.Count - 2))
                {
                    unit = E_ArmyType.Archer;
                }

                if (number <= 6)
                {
                    unit = new[] { E_ArmyType.Infantry, E_ArmyType.Archer, E_ArmyType.Spearman, E_ArmyType.Cavalry }[i % 4];
                }

                int row = number <= 6 ? (number % 2 == 0 ? r.bottom : r.top) : r.top + (number + i * 3) % r.Height;
                int col = number <= 6 ? (i % 2 == 0 ? r.left : r.right) : r.left + (number * 3 + i) % r.Width;
                level.clues.Add(new ArmyOrder { cell = new GridCell(row, col), area = r.Area, unit = unit });
            }

            // 每个示范布局至少存在一支相邻护卫，但玩家可以选择其他合法组合。
            if (level.requireArcherSupport)
            {
                for (int i = 0; i < solution.Count; i++)
                {
                    if (level.clues[i].unit != E_ArmyType.Archer)
                    {
                        continue;
                    }

                    int guard = solution.FindIndex(x => solution[i].SharesEdge(x) && level.clues[solution.IndexOf(x)].unit != E_ArmyType.Archer);
                    if (guard >= 0)
                    {
                        level.clues[guard].unit = guard % 2 == 0 ? E_ArmyType.Infantry : E_ArmyType.Spearman;
                    }
                }
            }

            if (number == 1)
            {
                Teach(level, "按住将令所在格，拖到对角格后松手。每个矩形须包含一道将令，面积与数字相同。", E_TutorialAction.Deploy, level.clues[0].cell);
            }

            if (number == 2)
            {
                Teach(level, "数字表示格数。六格既可以排成一行，也可以排成两行三列；长方形同样有效。", E_TutorialAction.Deploy, level.clues[0].cell);
            }

            if (number == 3)
            {
                level.lessons.Add(new TutorialStep { text = "试着框住两道将令，观察提示。本教学步骤不扣次数，也可跳过。", action = E_TutorialAction.InvalidSubmission, expectedFailure = E_ShikakuRuleFailure.MultipleClues });
                Teach(level, "现在只框住一道将令，并让矩形面积等于数字。", E_TutorialAction.Deploy, level.clues[0].cell);
            }

            if (number == 4)
            {
                Teach(level, "先部署这道六格将令。合法军阵也可以撤回调整。", E_TutorialAction.Deploy, level.clues[0].cell);
                Teach(level, "点击刚部署的阵地撤回整支部队，撤回不扣次数。", E_TutorialAction.Remove, level.clues[0].cell);
                Teach(level, "再部署这道将令，继续完成棋盘。", E_TutorialAction.Deploy, level.clues[0].cell);
            }

            if (number == 7)
            {
                Teach(level, "从本关开始学习弓阵护卫：每支弓阵须与任意步阵或枪阵共边，角接不算。可以先布弓阵再补护卫；青色连线表示协同成立。", E_TutorialAction.Deploy, level.clues[1].cell);
            }

            if (number == 15)
            {
                Teach(level, "变阵中有多支弓阵。一支护卫可以保护多个共边弓阵，也可以分兵保护；先考虑整盘的接应关系。", E_TutorialAction.Deploy, level.clues[1].cell);
            }

            if (number == 19)
            {
                Teach(level, "本关有三次容错。教学完成后，松手提交违规矩形才扣一次；预览、撤回与取消不扣次数。", E_TutorialAction.Deploy, level.clues[0].cell);
            }

            return level;
        }

        private static List<ShikakuRect> CreateFormation(ShikakuLevelData level, bool variant)
        {
            string[] map;
            switch (level.formation)
            {
                case E_FormationTheme.中军:
                    map = new[] { "AAAAAA", "BCCCCD", "BCCCCD", "BCCCCD", "BCCCCD", "EEEEEE" };
                    break;
                case E_FormationTheme.两翼:
                    map = new[] { "AABBBBCC", "AABBBBCC", "AADDDDCC", "AADDDDCC", "AAEEEECC", "AAEEEECC" };
                    break;
                case E_FormationTheme.纵深:
                    map = new[] { "AAAAA", "AAAAA", "BBCCC", "BBCCC", "DDEEE", "DDEEE", "FFFFF", "FFFFF" };
                    break;
                case E_FormationTheme.环护:
                    map = new[] { "AAAAAAB", "FCCCCCB", "FHIIIDB", "FHIIIDB", "FHIIIDB", "FGGGGDB", "FEEEEEE" };
                    break;
                case E_FormationTheme.交错:
                    map = new[] { "AAABBB", "AAABBB", "CCDBBB", "CCDEEE", "FFDEEE", "FFDEEE" };
                    break;
                case E_FormationTheme.雁行:
                    map = new[] { "AABBBB", "AABBBB", "AACCDD", "EECCDD", "EEFFFF", "EEFFFF" };
                    break;
                case E_FormationTheme.合围:
                    map = new[] { "AAABBCC", "AAABBCC", "DDDBBCC", "DDDEEEE", "FFGEEEE", "FFGEEEE" };
                    break;
                default:
                    map = new[] { "AAABBB", "AAABBB", "CDDBBB", "CDDBBB", "CEEFFF", "CEEFFF", "GGGHHH", "GGGHHH" };
                    break;
            }

            level.rows = variant ? map[0].Length : map.Length;
            level.cols = variant ? map.Length : map[0].Length;
            var regions = new Dictionary<char, ShikakuRect>();
            var order = new List<char>();
            for (int row = 0; row < map.Length; row++)
            {
                for (int col = 0; col < map[row].Length; col++)
                {
                    char key = map[row][col];
                    int r = variant ? col : row;
                    int c = variant ? map.Length - 1 - row : col;
                    if (!regions.TryGetValue(key, out var rect))
                    {
                        rect = new ShikakuRect(r, r, c, c);
                        order.Add(key);
                    }

                    rect.top = System.Math.Min(rect.top, r);
                    rect.bottom = System.Math.Max(rect.bottom, r);
                    rect.left = System.Math.Min(rect.left, c);
                    rect.right = System.Math.Max(rect.right, c);
                    regions[key] = rect;
                }
            }

            return order.ConvertAll(key => regions[key]);
        }

        private static void Teach(ShikakuLevelData level, string text, E_TutorialAction action, GridCell target)
        {
            level.lessons.Add(new TutorialStep { text = text, action = action, hasTarget = true, target = target });
        }

        private static string Title(int n)
        {
            switch (n)
            {
                case 1:
                    return "初识将令";
                case 2:
                    return "因数成阵";
                case 3:
                    return "一阵一令";
                case 4:
                    return "进退有度";
                case 7:
                    return "中盘推演";
                case 11:
                    return "纵横交错";
                case 15:
                    return "全局谋划";
                case 17:
                    return "虚实相生";
                case 18:
                    return "连环推演";
                case 19:
                    return "军令如山";
                case 20:
                    return "校场会试";
                default:
                    return n < 7 ? "校场操练" : n < 11 ? "矩形推演" : n < 15 ? "布局推演" : "全盘筹谋";
            }
        }
    }
}
