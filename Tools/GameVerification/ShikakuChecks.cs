using System;
using System.Collections.Generic;

namespace MainScene.BattlePart.Shikaku.Tests
{
    // Shared by Unity EditMode tests and the offline .NET verifier.
    public static class ShikakuChecks
    {
        public static List<string> Run()
        {
            var report = new List<string>();
            var levels = ShikakuCampaign.CreateDefault();
            Check(levels.Count == 20, "Default campaign contains 20 levels");
            foreach (var level in levels)
            {
                Check(level.Inspect().Count == 0, level.id + " configuration");
                var result = ShikakuSolver.Solve(level, null, 2);
                Check(result.solutions.Count > 0 && !result.budgetExceeded, level.id + " solvability");
                foreach (var solution in result.solutions)
                {
                    var session = new ShikakuModel(level);
                    foreach (var rect in solution)
                    {
                        Check(session.Submit(rect).IsValid, level.id + " solution submission");
                    }

                    Check(session.Status == E_ShikakuPlayStatus.Won && session.CoveredArea == level.AvailableArea, level.id + " complete coverage");
                    Check(session.Stars == 3, "Untouched completion earns three stars");
                }

                report.Add(level.id + ": playable; " + (result.solutions.Count >= 2 ? "multiple solutions accepted" : "unique solution"));
            }

            var one = new ShikakuLevelData
            {
                id = "one",
                rows = 1,
                cols = 1
            };
            one.clues.Add(new ArmyOrder { cell = new GridCell(0, 0), area = 1 });
            Check(new ShikakuModel(one).Submit(new ShikakuRect(0, 0, 0, 0)).IsValid, "Area-one order is supported");
            report.Add("Single-cell region supported");
            var exam = levels[19];
            Check(ShikakuRules.Validate(exam, new ShikakuRect(-1, 0, 0, 0)).failure == E_ShikakuRuleFailure.OutsideBoard, "Outside board");
            var fullSolution = ShikakuSolver.Solve(exam, null, 1).solutions[0];
            Check(ShikakuRules.Validate(exam, fullSolution[0], new[] { fullSolution[0] }).failure == E_ShikakuRuleFailure.Overlap, "Overlap");
            Check(ShikakuRules.Validate(levels[2], new ShikakuRect(0, 1, 0, 2)).failure == E_ShikakuRuleFailure.MultipleClues, "One clue only");
            var unoccupied = FindEmptyClueCell(levels[0]);
            Check(ShikakuRules.Validate(levels[0], ShikakuRect.Between(unoccupied, unoccupied)).failure == E_ShikakuRuleFailure.NoClue, "Missing clue");
            Check(ShikakuRules.Validate(levels[0], ShikakuRect.Between(levels[0].clues[0].cell, levels[0].clues[0].cell)).failure == E_ShikakuRuleFailure.AreaMismatch, "Area mismatch");
            // No terrain or unit rule may exclude an otherwise valid rectangle.
            foreach (var unit in new[] { E_ArmyType.Infantry, E_ArmyType.Spearman, E_ArmyType.Archer, E_ArmyType.Cavalry })
            {
                foreach (var shape in new[] { new ShikakuRect(0, 0, 0, 5), new ShikakuRect(0, 5, 0, 0), new ShikakuRect(0, 1, 0, 2), new ShikakuRect(0, 1, 0, 1) })
                {
                    var plain = new ShikakuLevelData { id = "plain", rows = shape.Height, cols = shape.Width };
                    plain.clues.Add(new ArmyOrder { cell = new GridCell(0, 0), area = shape.Area, unit = unit });
                    Check(ShikakuRules.Validate(plain, shape).IsValid, "Unit does not constrain rectangle geometry");
                    Check(ShikakuSolver.Solve(plain).solutions.Count == 1, "Solver covers the full board without terrain restrictions");
                    Check(new ShikakuModel(plain).Submit(shape).IsValid, "Submission uses the same core rules");
                }
            }
            report.Add("Core rules and unrestricted unit/rectangle combinations passed");
            var limited = new ShikakuModel(exam);
            var invalid = new ShikakuRect(-1, 0, 0, 0);
            limited.Submit(invalid);
            Check(limited.Errors == 1, "Exactly one error per submission");
            limited.Submit(fullSolution[0]);
            limited.Undo();
            Check(limited.Errors == 1, "Undo does not refund errors");
            limited.Submit(fullSolution[0]);
            limited.RemoveAt(new GridCell(fullSolution[0].top, fullSolution[0].left));
            Check(limited.Errors == 1 && limited.Regions.Count == 0, "Removal is free");
            limited.Undo();
            Check(limited.Regions.Count == 1, "Undo restores removed region");
            limited.Submit(invalid);
            limited.Submit(invalid);
            Check(limited.Status == E_ShikakuPlayStatus.Lost, "Error limit ends session");
            Check(!limited.CanUndo && limited.Submit(fullSolution[1]).failure == E_ShikakuRuleFailure.SessionClosed, "Loss locks editing");
            var teaching = new ShikakuModel(levels[18]);
            teaching.Submit(invalid);
            Check(teaching.Errors == 0, "Teaching does not spend attempts");
            teaching.SkipLesson();
            teaching.Submit(invalid);
            Check(teaching.Errors == 1, "After teaching errors count");
            var freedom = new ShikakuModel(levels[6]);
            for (int i = 0; i < 5; i++)
            {
                freedom.Submit(invalid);
            }

            Check(freedom.Errors == 0 && freedom.Status == E_ShikakuPlayStatus.Playing, "Practice is unlimited");
            var lessonSession = new ShikakuModel(levels[3]);
            var first = new ShikakuRect(0, 1, 0, 2);
            lessonSession.Submit(first);
            Check(lessonSession.LessonIndex == 1, "Deploy advances lesson");
            lessonSession.RemoveAt(new GridCell(0, 0));
            Check(lessonSession.LessonIndex == 2, "Removal advances lesson");
            lessonSession.Submit(first);
            Check(!lessonSession.InLesson, "Redeploy finishes lesson");
            var lessonErrors = new ShikakuModel(levels[2]);
            lessonErrors.Submit(new ShikakuRect(0, 1, 0, 2));
            Check(lessonErrors.LessonIndex == 1, "Expected invalid action advances lesson");
            report.Add("Error accounting, undo/remove history, loss locking and teaching progression passed");
            var current = new ShikakuModel(exam);
            current.Submit(fullSolution[0]);
            current.UseHint();
            var resume = current.Capture();
            var resumed = new ShikakuModel(exam);
            Check(resumed.Restore(resume) && resumed.Regions.Count == 1 && resumed.HintsUsed == 1 && resumed.CanUndo, "Resume board, hints and history");
            resumed.Undo();
            Check(resumed.Regions.Count == 0 && resumed.HintsUsed == 1, "Undo keeps hint consumption");
            current.Pause(true);
            Check(current.Submit(fullSolution[1]).failure == E_ShikakuRuleFailure.SessionClosed, "Pause locks edits");
            current.Pause(false);
            resume.regions.Add(resume.regions[0]);
            Check(!resumed.Restore(resume) && resumed.Regions.Count == 0, "Corrupt save rejected atomically");
            resume = current.Capture();
            resume.fingerprint = "old";
            Check(!resumed.Restore(resume), "Stale configuration rejected");
            var activeSolve = ShikakuSolver.Solve(exam, current.Regions, 1);
            Check(activeSolve.solutions.Count == 1 && activeSolve.solutions[0].Contains(fullSolution[0]), "Hints preserve placed regions");
            Check(ShikakuSolver.Solve(levels[0], null, 2, 1).budgetExceeded, "Budget exhaustion is distinguished from no solution");
            report.Add("Save validation, pause, hint constraints and search budget passed");
            CheckDeadEnd();
            report.Add("Locally valid dead-end remains legal, solver detects no completion");
            CheckFormationSupport();
            report.Add("Archer support: edge/corner geometry, solver filtering, pending completion, restore, removal and redeployment passed");
            var themes = new HashSet<E_FormationTheme>();
            foreach (var level in levels)
            {
                if (level.formation != E_FormationTheme.操练)
                {
                    themes.Add(level.formation);
                    Check(level.requireArcherSupport, "Formation levels enable meaningful cooperation");
                }
            }
            Check(themes.Count == 8, "Campaign presents all eight formation themes");
            CheckLargeBoards();
            report.Add("Large boards: 10x10, 12x16, 20x20, 32x32, 11x13, narrow boards; solving, full coverage, restore and indexed lookup passed");
            return report;
        }

        private static void CheckLargeBoards()
        {
            foreach (var size in new[] { new GridCell(10, 10), new GridCell(12, 16), new GridCell(20, 20), new GridCell(32, 32), new GridCell(11, 13), new GridCell(1, 12), new GridCell(12, 1), new GridCell(2, 2) })
            {
                var level = ShikakuCampaign.CreateLargePractice(size.row, size.col);
                Check(level.Inspect().Count == 0, $"Large template {size.row}x{size.col} configuration");
                var solved = ShikakuSolver.Solve(level, null, 1, 50000);
                Check(!solved.budgetExceeded && solved.solutions.Count == 1, "Large-board hint budget");
                var session = new ShikakuModel(level);
                var first = solved.solutions[0][0];
                var cell = new GridCell(first.top, first.left);
                Check(session.RegionAt(cell) == -1, "Empty cached lookup");
                session.Submit(first);
                Check(session.RegionAt(cell) == 0, "Placed cached lookup");
                if (session.Status == E_ShikakuPlayStatus.Won)
                {
                    Check(solved.solutions[0].Count == 1 && session.CoveredArea == size.row * size.col, "Single-region template completed");
                    continue;
                }
                session.RemoveAt(cell);
                Check(session.RegionAt(cell) == -1, "Removal invalidates cached lookup");
                Check(session.Undo() && session.RegionAt(cell) == 0, "Undo rebuilds cached lookup");
                var restored = new ShikakuModel(level);
                Check(restored.Restore(session.Capture()) && restored.RegionAt(cell) == 0, "Large-board partial restore");
                foreach (var rect in solved.solutions[0])
                {
                    if (!rect.Equals(first))
                    {
                        Check(restored.Submit(rect).IsValid, "Large-board submission");
                    }
                }

                Check(restored.Status == E_ShikakuPlayStatus.Won && restored.CoveredArea == size.row * size.col, "Large-board full coverage");
                for (int row = 0; row < size.row; row++)
                {
                    for (int col = 0; col < size.col; col++)
                    {
                        int index = restored.RegionAt(new GridCell(row, col));
                        Check(index >= 0 && restored.Regions[index].Contains(new GridCell(row, col)), "Every cached cell matches its region");
                    }
                }
            }

            var oversized = new ShikakuLevelData { rows = ShikakuLevelData.MaxBoardSize + 1, cols = 1 };
            Check(oversized.Inspect().Count > 0, "Unsupported board size rejected");
        }

        private static void CheckFormationSupport()
        {
            Check(new ShikakuRect(0, 1, 0, 0).SharesEdge(new ShikakuRect(1, 2, 1, 1)), "Partial shared edge counts");
            Check(!new ShikakuRect(0, 0, 0, 0).SharesEdge(new ShikakuRect(1, 1, 1, 1)), "Diagonal corner does not count");
            Check(!new ShikakuRect(0, 0, 0, 0).SharesEdge(new ShikakuRect(0, 0, 2, 2)), "Separated regions do not count");
            var level = new ShikakuLevelData { id = "support", rows = 2, cols = 3, requireArcherSupport = true };
            level.clues.Add(new ArmyOrder { cell = new GridCell(0, 0), area = 2, unit = E_ArmyType.Archer });
            level.clues.Add(new ArmyOrder { cell = new GridCell(0, 2), area = 2, unit = E_ArmyType.Infantry });
            level.clues.Add(new ArmyOrder { cell = new GridCell(1, 1), area = 2, unit = E_ArmyType.Cavalry });
            var badArcher = new ShikakuRect(0, 1, 0, 0);
            var cavalry = new ShikakuRect(0, 1, 1, 1);
            var guard = new ShikakuRect(0, 1, 2, 2);
            var session = new ShikakuModel(level);
            Check(session.Submit(badArcher).IsValid && session.Submit(cavalry).IsValid && session.Submit(guard).IsValid, "Deploy in any order without premature support penalties");
            Check(session.CoveredArea == 6 && session.Status == E_ShikakuPlayStatus.Playing && session.Errors == 0, "Full coverage without support stays editable");
            Check(ShikakuSolver.Solve(level, session.Regions).solutions.Count == 0, "Hints reject fully covered unsupported board");
            var restored = new ShikakuModel(level);
            Check(restored.Restore(session.Capture()) && restored.Status == E_ShikakuPlayStatus.Playing, "Pending formation survives restore");
            var solved = ShikakuSolver.Solve(level, null, 10);
            Check(solved.solutions.Count > 0, "Support fixture solvable");
            foreach (var solution in solved.solutions)
            {
                Check(ShikakuRules.FormationSatisfied(level, solution), "All solver solutions satisfy support");
            }

            Check(restored.RemoveAt(new GridCell(0, 0)) && restored.RemoveAt(new GridCell(1, 1)), "Unsupported formation can be adjusted");
            restored.Submit(new ShikakuRect(0, 0, 0, 1));
            restored.Submit(new ShikakuRect(1, 1, 0, 1));
            Check(restored.Status == E_ShikakuPlayStatus.Won, "Rearranging to share a guard edge completes formation");
            string fingerprint = level.Fingerprint();
            level.requireArcherSupport = false;
            Check(level.Fingerprint() != fingerprint, "Support changes invalidate stale resumes");
            var unrestricted = new ShikakuModel(level);
            unrestricted.Submit(badArcher);
            unrestricted.Submit(cavalry);
            unrestricted.Submit(guard);
            Check(unrestricted.Status == E_ShikakuPlayStatus.Won, "Existing practice levels retain base rules");

            level.requireArcherSupport = true;
            level.clues[1].unit = E_ArmyType.Cavalry;
            Check(level.Inspect().Count > 0, "Missing guard configuration rejected");
            level.clues[1].unit = E_ArmyType.Spearman;
            Check(ShikakuSolver.Solve(level).solutions.Count > 0, "Spearmen can guard archers");

            var shared = new ShikakuLevelData { id = "shared-guard", rows = 1, cols = 4, requireArcherSupport = true };
            var units = new[] { E_ArmyType.Archer, E_ArmyType.Infantry, E_ArmyType.Archer, E_ArmyType.Cavalry };
            for (int col = 0; col < units.Length; col++)
            {
                shared.clues.Add(new ArmyOrder { cell = new GridCell(0, col), area = 1, unit = units[col] });
            }

            var network = new ShikakuModel(shared);
            for (int col = 0; col < 3; col++)
            {
                network.Submit(new ShikakuRect(0, 0, col, col));
            }

            Check(ShikakuRules.SupportedArchers(shared, network.Regions) == 2, "One guard can support multiple archers");
            network.RemoveAt(new GridCell(0, 1));
            Check(ShikakuRules.SupportedArchers(shared, network.Regions) == 0, "Removing guard updates all cooperation");
            Check(network.Undo() && ShikakuRules.SupportedArchers(shared, network.Regions) == 2, "Undo restores cooperation");
            network.Submit(new ShikakuRect(0, 0, 3, 3));
            var finished = new ShikakuModel(shared);
            Check(finished.Restore(network.Capture()) && finished.Status == E_ShikakuPlayStatus.Won, "Supported complete formation restores as won");
        }

        private static GridCell FindEmptyClueCell(ShikakuLevelData level)
        {
            for (int r = 0; r < level.rows; r++)
            {
                for (int c = 0; c < level.cols; c++)
                {
                    if (level.ClueAt(new GridCell(r, c)) == null)
                    {
                        return new GridCell(r, c);
                    }
                }
            }

            throw new Exception("Need empty cell");
        }

        private static void CheckDeadEnd()
        {
            var level = new ShikakuLevelData
            {
                id = "dead-end",
                rows = 2,
                cols = 3
            };
            level.clues.Add(new ArmyOrder { cell = new GridCell(0, 0), area = 2 });
            level.clues.Add(new ArmyOrder { cell = new GridCell(0, 2), area = 4 });
            Check(ShikakuSolver.Solve(level, null, 1).solutions.Count == 1, "Dead-end fixture has a solution");
            var session = new ShikakuModel(level);
            Check(session.Submit(new ShikakuRect(0, 0, 0, 1)).IsValid, "Locally valid rectangle accepted");
            Check(session.Errors == 0 && ShikakuSolver.Solve(level, session.Regions, 1).solutions.Count == 0, "Dead-end not penalized");
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException("FAILED: " + name);
            }
        }
    }
}
