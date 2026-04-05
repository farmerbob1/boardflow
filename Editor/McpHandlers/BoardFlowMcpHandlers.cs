using System;
using System.Collections.Generic;
using System.Linq;
using BoardFlow.Editor.Data;
using BoardFlow.Editor.Services;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace BoardFlow.Editor.McpHandlers
{
    [McpHandlerGroup]
    public static class BoardFlowMcpHandlers
    {
        public static void Register(Dictionary<string, Func<JObject, object>> handlers)
        {
            // Board commands
            handlers["boardflow_list_boards"] = ListBoards;
            handlers["boardflow_create_board"] = CreateBoard;
            handlers["boardflow_delete_board"] = DeleteBoard;
            handlers["boardflow_rename_board"] = RenameBoard;

            // Column commands
            handlers["boardflow_list_columns"] = ListColumns;
            handlers["boardflow_create_column"] = CreateColumn;
            handlers["boardflow_delete_column"] = DeleteColumn;
            handlers["boardflow_rename_column"] = RenameColumn;

            // Task commands
            handlers["boardflow_get_board"] = GetBoard;
            handlers["boardflow_create_task"] = CreateTask;
            handlers["boardflow_delete_task"] = DeleteTask;
            handlers["boardflow_update_task"] = UpdateTask;
            handlers["boardflow_move_task"] = MoveTask;

            // Label commands
            handlers["boardflow_create_label"] = CreateLabel;
            handlers["boardflow_delete_label"] = DeleteLabel;
            handlers["boardflow_add_label_to_task"] = AddLabelToTask;
            handlers["boardflow_remove_label_from_task"] = RemoveLabelFromTask;

            // Checklist commands
            handlers["boardflow_add_checklist_item"] = AddChecklistItem;
            handlers["boardflow_toggle_checklist_item"] = ToggleChecklistItem;
            handlers["boardflow_delete_checklist_item"] = DeleteChecklistItem;

            // Column settings
            handlers["boardflow_set_column_wip_limit"] = SetColumnWipLimit;
            handlers["boardflow_set_column_collapsed"] = SetColumnCollapsed;
            handlers["boardflow_set_column_sort_mode"] = SetColumnSortMode;

            // Custom field commands
            handlers["boardflow_create_custom_field"] = CreateCustomField;
            handlers["boardflow_delete_custom_field"] = DeleteCustomField;
            handlers["boardflow_set_custom_field_value"] = SetCustomFieldValue;

            // Statistics
            handlers["boardflow_get_board_statistics"] = GetBoardStatistics;
        }

        static void RefreshWindow()
        {
            if (EditorWindow.HasOpenInstances<BoardFlowWindow>())
                EditorWindow.GetWindow<BoardFlowWindow>().RefreshBoard();
        }

        // --- Board commands ---

        static object ListBoards(JObject parameters)
        {
            var data = BoardFlowDataService.Data;
            var boards = data.boards.Select(b => new
            {
                id = b.id,
                name = b.name,
                columnCount = b.columns.Count,
                taskCount = b.columns.Sum(c => c.tasks.Count),
                createdAt = b.createdAt,
                modifiedAt = b.modifiedAt
            }).ToList();

            return new { success = true, boards };
        }

        static object CreateBoard(JObject parameters)
        {
            var name = (string)parameters["name"];
            var board = BoardFlowDataService.CreateBoard(name);
            RefreshWindow();
            return new { success = true, board = new { id = board.id, name = board.name } };
        }

        static object DeleteBoard(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            BoardFlowDataService.DeleteBoard(boardId);
            RefreshWindow();
            return new { success = true };
        }

        static object RenameBoard(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var name = (string)parameters["name"];
            BoardFlowDataService.RenameBoard(boardId, name);
            RefreshWindow();
            return new { success = true };
        }

        // --- Column commands ---

        static object ListColumns(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var board = BoardFlowDataService.FindBoard(boardId);
            var columns = board.columns.Select(c => new
            {
                id = c.id,
                title = c.title,
                taskCount = c.tasks.Count,
                wipLimit = c.wipLimit,
                isCollapsed = c.isCollapsed,
                sortMode = c.sortMode.ToString()
            }).ToList();

            return new { success = true, columns };
        }

        static object CreateColumn(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var title = (string)parameters["title"];
            var column = BoardFlowDataService.CreateColumn(boardId, title);
            RefreshWindow();
            return new { success = true, column = new { id = column.id, title = column.title } };
        }

        static object DeleteColumn(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            BoardFlowDataService.DeleteColumn(boardId, columnId);
            RefreshWindow();
            return new { success = true };
        }

        static object RenameColumn(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var title = (string)parameters["title"];
            BoardFlowDataService.RenameColumn(boardId, columnId, title);
            RefreshWindow();
            return new { success = true };
        }

        // --- Task commands ---

        static object GetBoard(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var board = BoardFlowDataService.FindBoard(boardId);

            return new
            {
                success = true,
                board = new
                {
                    id = board.id,
                    name = board.name,
                    createdAt = board.createdAt,
                    modifiedAt = board.modifiedAt,
                    labels = board.labels.Select(l => new
                    {
                        id = l.id,
                        name = l.name,
                        color = l.color
                    }).ToList(),
                    customFields = board.customFields.Select(cf => new
                    {
                        id = cf.id,
                        name = cf.name
                    }).ToList(),
                    columns = board.columns.Select(c => new
                    {
                        id = c.id,
                        title = c.title,
                        wipLimit = c.wipLimit,
                        isCollapsed = c.isCollapsed,
                        sortMode = c.sortMode.ToString(),
                        tasks = c.tasks.Select(t => new
                        {
                            id = t.id,
                            title = t.title,
                            description = t.description,
                            priority = t.priority.ToString(),
                            color = t.color,
                            colorMode = t.colorMode.ToString(),
                            labelIds = t.labelIds,
                            checklist = t.checklist.Select(ci => new
                            {
                                id = ci.id,
                                text = ci.text,
                                isCompleted = ci.isCompleted
                            }).ToList(),
                            customFieldValues = t.customFieldValues.Select(fv => new
                            {
                                fieldId = fv.fieldId,
                                value = fv.value
                            }).ToList(),
                            createdAt = t.createdAt,
                            modifiedAt = t.modifiedAt
                        }).ToList()
                    }).ToList()
                }
            };
        }

        static object CreateTask(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var title = (string)parameters["title"];
            var task = BoardFlowDataService.CreateTask(boardId, columnId, title);

            if (parameters["description"] != null)
            {
                var description = (string)parameters["description"];
                BoardFlowDataService.UpdateTaskDescription(boardId, columnId, task.id, description);
            }

            if (parameters["priority"] != null)
            {
                var priority = Enum.Parse<Priority>((string)parameters["priority"]);
                BoardFlowDataService.SetTaskPriority(boardId, columnId, task.id, priority);
            }

            RefreshWindow();
            return new { success = true, task = new { id = task.id, title = task.title } };
        }

        static object DeleteTask(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];
            BoardFlowDataService.DeleteTask(boardId, columnId, taskId);
            RefreshWindow();
            return new { success = true };
        }

        static object UpdateTask(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];

            if (parameters["title"] != null)
                BoardFlowDataService.UpdateTaskTitle(boardId, columnId, taskId, (string)parameters["title"]);

            if (parameters["description"] != null)
                BoardFlowDataService.UpdateTaskDescription(boardId, columnId, taskId, (string)parameters["description"]);

            if (parameters["priority"] != null)
            {
                var priority = Enum.Parse<Priority>((string)parameters["priority"]);
                BoardFlowDataService.SetTaskPriority(boardId, columnId, taskId, priority);
            }

            RefreshWindow();
            return new { success = true };
        }

        static object MoveTask(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var fromColumnId = (string)parameters["fromColumnId"];
            var toColumnId = (string)parameters["toColumnId"];
            var taskId = (string)parameters["taskId"];
            var insertIndex = parameters["insertIndex"] != null ? (int)parameters["insertIndex"] : -1;
            BoardFlowDataService.MoveTask(boardId, fromColumnId, toColumnId, taskId, insertIndex);
            RefreshWindow();
            return new { success = true };
        }

        // --- Label commands ---

        static object CreateLabel(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var name = (string)parameters["name"];
            var color = (string)parameters["color"];
            var label = BoardFlowDataService.CreateLabel(boardId, name, color);
            RefreshWindow();
            return new { success = true, label = new { id = label.id, name = label.name, color = label.color } };
        }

        static object DeleteLabel(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var labelId = (string)parameters["labelId"];
            BoardFlowDataService.DeleteLabel(boardId, labelId);
            RefreshWindow();
            return new { success = true };
        }

        static object AddLabelToTask(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];
            var labelId = (string)parameters["labelId"];
            BoardFlowDataService.AddLabelToTask(boardId, columnId, taskId, labelId);
            RefreshWindow();
            return new { success = true };
        }

        static object RemoveLabelFromTask(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];
            var labelId = (string)parameters["labelId"];
            BoardFlowDataService.RemoveLabelFromTask(boardId, columnId, taskId, labelId);
            RefreshWindow();
            return new { success = true };
        }

        // --- Checklist commands ---

        static object AddChecklistItem(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];
            var text = (string)parameters["text"];
            var item = BoardFlowDataService.AddChecklistItem(boardId, columnId, taskId, text);
            RefreshWindow();
            return new
            {
                success = true,
                item = new { id = item.id, text = item.text, isCompleted = item.isCompleted }
            };
        }

        static object ToggleChecklistItem(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];
            var itemId = (string)parameters["itemId"];
            BoardFlowDataService.ToggleChecklistItem(boardId, columnId, taskId, itemId);
            RefreshWindow();
            return new { success = true };
        }

        static object DeleteChecklistItem(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];
            var itemId = (string)parameters["itemId"];
            BoardFlowDataService.RemoveChecklistItem(boardId, columnId, taskId, itemId);
            RefreshWindow();
            return new { success = true };
        }

        // --- Column settings ---

        static object SetColumnWipLimit(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var wipLimit = (int)parameters["wipLimit"];
            BoardFlowDataService.SetColumnWipLimit(boardId, columnId, wipLimit);
            RefreshWindow();
            return new { success = true };
        }

        static object SetColumnCollapsed(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var collapsed = (bool)parameters["collapsed"];
            BoardFlowDataService.SetColumnCollapsed(boardId, columnId, collapsed);
            RefreshWindow();
            return new { success = true };
        }

        static object SetColumnSortMode(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var sortMode = Enum.Parse<SortMode>((string)parameters["sortMode"]);
            BoardFlowDataService.SetColumnSortMode(boardId, columnId, sortMode);
            RefreshWindow();
            return new { success = true };
        }

        // --- Custom field commands ---

        static object CreateCustomField(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var name = (string)parameters["name"];
            var field = BoardFlowDataService.CreateCustomField(boardId, name);
            RefreshWindow();
            return new { success = true, field = new { id = field.id, name = field.name } };
        }

        static object DeleteCustomField(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var fieldId = (string)parameters["fieldId"];
            BoardFlowDataService.DeleteCustomField(boardId, fieldId);
            RefreshWindow();
            return new { success = true };
        }

        static object SetCustomFieldValue(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var columnId = (string)parameters["columnId"];
            var taskId = (string)parameters["taskId"];
            var fieldId = (string)parameters["fieldId"];
            var value = (string)parameters["value"];
            BoardFlowDataService.SetCustomFieldValue(boardId, columnId, taskId, fieldId, value);
            RefreshWindow();
            return new { success = true };
        }

        // --- Statistics ---

        static object GetBoardStatistics(JObject parameters)
        {
            var boardId = (string)parameters["boardId"];
            var board = BoardFlowDataService.FindBoard(boardId);

            int totalTasks = 0;
            int totalChecklist = 0;
            int completedChecklist = 0;
            var priorityCounts = new Dictionary<string, int>();
            var columnsStats = new List<object>();

            for (int c = 0; c < board.columns.Count; c++)
            {
                var col = board.columns[c];
                totalTasks += col.tasks.Count;
                columnsStats.Add(new { title = col.title, taskCount = col.tasks.Count, wipLimit = col.wipLimit });

                for (int t = 0; t < col.tasks.Count; t++)
                {
                    var task = col.tasks[t];
                    var pName = task.priority.ToString();
                    if (!priorityCounts.ContainsKey(pName))
                        priorityCounts[pName] = 0;
                    priorityCounts[pName]++;

                    if (task.checklist != null)
                    {
                        totalChecklist += task.checklist.Count;
                        for (int ci = 0; ci < task.checklist.Count; ci++)
                        {
                            if (task.checklist[ci].isCompleted)
                                completedChecklist++;
                        }
                    }
                }
            }

            return new
            {
                success = true,
                statistics = new
                {
                    totalTasks,
                    columns = columnsStats,
                    priorityBreakdown = priorityCounts,
                    checklistTotal = totalChecklist,
                    checklistCompleted = completedChecklist,
                    hasCriticalTasks = BoardFlowDataService.HasCriticalTasks(boardId)
                }
            };
        }
    }
}
