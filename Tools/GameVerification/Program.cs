using System;
using System.IO;
using System.Text.RegularExpressions;
using MainScene.BattlePart.Shikaku;
using MainScene.BattlePart.Shikaku.Tests;
using UnityEngine;

internal static class Program
{
    private static void Assert(bool condition, string text)
    {
        if (!condition)
        {
            throw new Exception(text);
        }
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--export-campaign")
            {
                File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(ShikakuCampaign.CreateDefault(), new System.Text.Json.JsonSerializerOptions { IncludeFields = true }));
                return 0;
            }

            foreach (var text in ShikakuChecks.Run())
            {
                Console.WriteLine("PASS " + text);
            }

            CheckPackagedLevels();

            var level = ShikakuCampaign.CreateDefault()[0];
            var model = new ShikakuModel(level);
            var sim = new ShikakuSimulation(model);
            sim.Tick(new ShikakuInputFrame { PressDown = true, HasCell = true, CellPosition = new Vector2Int(0, 0) }, .016f);
            sim.Tick(new ShikakuInputFrame { Pressing = true, HasCell = true, CellPosition = new Vector2Int(0, 2) }, .016f);
            sim.Tick(new ShikakuInputFrame { PressUp = true, HasCell = false }, .016f);
            Assert(model.Regions.Count == 0 && !model.IsSelecting && model.Errors == 0, "Release outside must cancel");
            sim.Tick(new ShikakuInputFrame { PressDown = true, HasCell = true, CellPosition = new Vector2Int(0, 0) }, .016f);
            sim.Tick(new ShikakuInputFrame { PressUp = true, HasCell = true, CellPosition = new Vector2Int(0, 2) }, .016f);
            Assert(model.Regions.Count == 1 && !model.IsSelecting && model.LastResult == MainScene.BattlePart.E_ShikakuSelectResult.Success, "Submit through original Simulation");
            sim.Tick(new ShikakuInputFrame { PressDown = true, HasCell = true, CellPosition = new Vector2Int(0, 1) }, .016f);
            sim.Tick(new ShikakuInputFrame { PressUp = true, HasCell = true, CellPosition = new Vector2Int(1, 1) }, .016f);
            Assert(model.Regions.Count == 0 && !model.IsSelecting, "Removal gesture must not submit a new rectangle");
            model.BeginSelection(new Vector2Int(0, 0));
            model.Pause(true);
            Assert(!model.IsSelecting, "Pause cancels current drag");
            Console.WriteLine("PASS original Simulation submission, off-board cancellation, removal gesture and pause");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
    }

    // Check the actual Unity YAML resources as well as the fallback campaign.
    private static void CheckPackagedLevels()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Assets", "_LikeShikaku")))
        {
            root = root.Parent;
        }

        Assert(root != null, "Cannot locate project resources");
        foreach (var expected in ShikakuCampaign.CreateDefault())
        {
            string path = Path.Combine(root.FullName, "Assets", "_LikeShikaku", "Resources", "Shikaku", "Levels", expected.id + ".asset");
            string yaml = File.ReadAllText(path).Replace("\r\n", "\n");
            Assert(!Regex.IsMatch(yaml, @"^  (boundaries|special):", RegexOptions.Multiline), expected.id + " retired serialized rules");
            var size = Regex.Match(yaml, @"determinant: \{x: (\d+), y: (\d+)\}");
            var errors = Regex.Match(yaml, @"maximumErrors: (\d+)");
            var level = new ShikakuLevelData
            {
                id = expected.id,
                rows = int.Parse(size.Groups[1].Value),
                cols = int.Parse(size.Groups[2].Value),
                maximumErrors = int.Parse(errors.Groups[1].Value),
                formation = (E_FormationTheme)int.Parse(Regex.Match(yaml, @"formation: (\d+)").Groups[1].Value),
                requireArcherSupport = Regex.Match(yaml, @"requireArcherSupport: (\d+)").Groups[1].Value == "1"
            };
            foreach (Match cell in Regex.Matches(yaml, @"- position: \{x: (\d+), y: (\d+)\}\n    cellType: (\d+)\n    number: (\d+)\n    armyType: (\d+)"))
            {
                Assert(cell.Groups[3].Value == "0", expected.id + " retired blocked cell");
                level.clues.Add(new ArmyOrder
                {
                    cell = new GridCell(int.Parse(cell.Groups[1].Value), int.Parse(cell.Groups[2].Value)),
                    area = int.Parse(cell.Groups[4].Value),
                    unit = (E_ArmyType)int.Parse(cell.Groups[5].Value)
                });
            }

            Assert(level.Inspect().Count == 0, expected.id + " packaged configuration");
            Assert(level.formation == expected.formation && level.requireArcherSupport == expected.requireArcherSupport, expected.id + " packaged formation rules");
            Assert(level.clues.Count == expected.clues.Count && level.rows == expected.rows && level.cols == expected.cols, expected.id + " packaged board dimensions");
            for (int i = 0; i < level.clues.Count; i++)
            {
                Assert(level.clues[i].cell.Equals(expected.clues[i].cell) && level.clues[i].area == expected.clues[i].area && level.clues[i].unit == expected.clues[i].unit, expected.id + " packaged orders match fallback");
            }

            var solved = ShikakuSolver.Solve(level, null, 1);
            Assert(solved.solutions.Count == 1 && !solved.budgetExceeded, expected.id + " packaged solution");
            var session = new ShikakuModel(level);
            foreach (var rect in solved.solutions[0])
            {
                Assert(session.Submit(rect).IsValid, expected.id + " packaged submission");
            }

            Assert(session.Status == E_ShikakuPlayStatus.Won && session.CoveredArea == level.rows * level.cols, expected.id + " packaged full coverage");
        }

        Console.WriteLine("PASS all 20 packaged Unity levels match fallback data and complete the full board");
    }
}
