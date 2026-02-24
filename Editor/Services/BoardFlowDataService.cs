using System.Collections.Generic;
using System.IO;
using BoardFlow.Editor.Data;
using UnityEngine;

namespace BoardFlow.Editor.Services
{
    public static class BoardFlowDataService
    {
        const string k_Directory = "ProjectSettings/BoardFlow";
        const string k_FilePath = "ProjectSettings/BoardFlow/boardflow-data.json";

        static BoardFlowData s_Data;

        public static BoardFlowData Data
        {
            get
            {
                if (s_Data == null)
                    Load();
                return s_Data;
            }
        }

        public static void Load()
        {
            if (File.Exists(k_FilePath))
            {
                var json = File.ReadAllText(k_FilePath);
                s_Data = JsonUtility.FromJson<BoardFlowData>(json);
                if (s_Data == null)
                    s_Data = new BoardFlowData();
            }
            else
            {
                s_Data = new BoardFlowData();
                CreateDefaultBoard();
                Save();
            }
        }

        public static void Save()
        {
            if (s_Data == null)
                return;

            if (!Directory.Exists(k_Directory))
                Directory.CreateDirectory(k_Directory);

            var json = JsonUtility.ToJson(s_Data, true);
            File.WriteAllText(k_FilePath, json);
        }

        public static void SetDataFromJson(string json)
        {
            s_Data = JsonUtility.FromJson<BoardFlowData>(json);
            if (s_Data == null)
                s_Data = new BoardFlowData();
        }

        public static string GetDataAsJson()
        {
            return JsonUtility.ToJson(s_Data, true);
        }

        // --- Board CRUD ---

        public static BoardData CreateBoard(string name)
        {
            var board = new BoardData(name);
            s_Data.boards.Add(board);
            s_Data.activeBoardId = board.id;
            Save();
            return board;
        }

        public static void DeleteBoard(string boardId)
        {
            s_Data.boards.RemoveAll(b => b.id == boardId);
            if (s_Data.activeBoardId == boardId)
                s_Data.activeBoardId = s_Data.boards.Count > 0 ? s_Data.boards[0].id : string.Empty;
            Save();
        }

        public static void RenameBoard(string boardId, string newName)
        {
            var board = FindBoard(boardId);
            if (board != null)
            {
                board.name = newName;
                board.Touch();
                Save();
            }
        }

        public static void SetActiveBoard(string boardId)
        {
            s_Data.activeBoardId = boardId;
            Save();
        }

        // --- Column CRUD ---

        public static ColumnData CreateColumn(string boardId, string title)
        {
            var board = FindBoard(boardId);
            if (board == null) return null;

            var column = new ColumnData(title);
            board.columns.Add(column);
            board.Touch();
            Save();
            return column;
        }

        public static void DeleteColumn(string boardId, string columnId)
        {
            var board = FindBoard(boardId);
            if (board == null) return;

            board.columns.RemoveAll(c => c.id == columnId);
            board.Touch();
            Save();
        }

        public static void RenameColumn(string boardId, string columnId, string newTitle)
        {
            var col = FindColumn(boardId, columnId);
            if (col != null)
            {
                col.title = newTitle;
                FindBoard(boardId)?.Touch();
                Save();
            }
        }

        // --- Task CRUD ---

        public static TaskCardData CreateTask(string boardId, string columnId, string title)
        {
            var col = FindColumn(boardId, columnId);
            if (col == null) return null;

            var task = new TaskCardData(title);
            col.tasks.Add(task);
            FindBoard(boardId)?.Touch();
            Save();
            return task;
        }

        public static void DeleteTask(string boardId, string columnId, string taskId)
        {
            var col = FindColumn(boardId, columnId);
            if (col == null) return;

            col.tasks.RemoveAll(t => t.id == taskId);
            FindBoard(boardId)?.Touch();
            Save();
        }

        public static void UpdateTaskTitle(string boardId, string columnId, string taskId, string newTitle)
        {
            var task = FindTask(boardId, columnId, taskId);
            if (task != null)
            {
                task.title = newTitle;
                task.Touch();
                FindBoard(boardId)?.Touch();
                Save();
            }
        }

        public static void SetTaskPriority(string boardId, string columnId, string taskId, Priority priority)
        {
            var task = FindTask(boardId, columnId, taskId);
            if (task != null)
            {
                task.priority = priority;
                task.Touch();
                FindBoard(boardId)?.Touch();
                Save();
            }
        }

        // --- Checklist CRUD ---

        public static ChecklistItemData AddChecklistItem(string boardId, string columnId, string taskId, string text)
        {
            var task = FindTask(boardId, columnId, taskId);
            if (task == null) return null;

            var item = new ChecklistItemData(text);
            task.checklist.Add(item);
            task.Touch();
            FindBoard(boardId)?.Touch();
            Save();
            return item;
        }

        public static void RemoveChecklistItem(string boardId, string columnId, string taskId, string itemId)
        {
            var task = FindTask(boardId, columnId, taskId);
            if (task == null) return;

            task.checklist.RemoveAll(i => i.id == itemId);
            task.Touch();
            FindBoard(boardId)?.Touch();
            Save();
        }

        public static void ToggleChecklistItem(string boardId, string columnId, string taskId, string itemId)
        {
            var task = FindTask(boardId, columnId, taskId);
            if (task == null) return;

            for (int i = 0; i < task.checklist.Count; i++)
            {
                if (task.checklist[i].id == itemId)
                {
                    task.checklist[i].isCompleted = !task.checklist[i].isCompleted;
                    break;
                }
            }

            task.Touch();
            FindBoard(boardId)?.Touch();
            Save();
        }

        public static void UpdateChecklistItemText(string boardId, string columnId, string taskId, string itemId, string newText)
        {
            var task = FindTask(boardId, columnId, taskId);
            if (task == null) return;

            for (int i = 0; i < task.checklist.Count; i++)
            {
                if (task.checklist[i].id == itemId)
                {
                    task.checklist[i].text = newText;
                    break;
                }
            }

            task.Touch();
            FindBoard(boardId)?.Touch();
            Save();
        }

        // --- Move operations ---

        public static void MoveTask(string boardId, string fromColumnId, string toColumnId, string taskId, int insertIndex)
        {
            var fromCol = FindColumn(boardId, fromColumnId);
            var toCol = FindColumn(boardId, toColumnId);
            if (fromCol == null || toCol == null) return;

            TaskCardData task = null;
            for (int i = 0; i < fromCol.tasks.Count; i++)
            {
                if (fromCol.tasks[i].id == taskId)
                {
                    task = fromCol.tasks[i];
                    fromCol.tasks.RemoveAt(i);
                    break;
                }
            }

            if (task == null) return;

            if (insertIndex < 0 || insertIndex > toCol.tasks.Count)
                insertIndex = toCol.tasks.Count;

            toCol.tasks.Insert(insertIndex, task);
            task.Touch();
            FindBoard(boardId)?.Touch();
            Save();
        }

        public static void MoveColumn(string boardId, string columnId, int insertIndex)
        {
            var board = FindBoard(boardId);
            if (board == null) return;

            ColumnData column = null;
            int fromIndex = -1;
            for (int i = 0; i < board.columns.Count; i++)
            {
                if (board.columns[i].id == columnId)
                {
                    column = board.columns[i];
                    fromIndex = i;
                    break;
                }
            }

            if (column == null) return;

            board.columns.RemoveAt(fromIndex);

            // Adjust insert index if we removed before it
            if (fromIndex < insertIndex)
                insertIndex--;

            if (insertIndex < 0 || insertIndex > board.columns.Count)
                insertIndex = board.columns.Count;

            board.columns.Insert(insertIndex, column);
            board.Touch();
            Save();
        }

        // --- Grid Settings ---

        public static void SaveGridSettings(GridSettings settings)
        {
            s_Data.gridSettings = settings;
            Save();
        }

        // --- Lookup helpers ---

        public static BoardData FindBoard(string boardId)
        {
            if (s_Data == null || string.IsNullOrEmpty(boardId)) return null;
            for (int i = 0; i < s_Data.boards.Count; i++)
            {
                if (s_Data.boards[i].id == boardId)
                    return s_Data.boards[i];
            }
            return null;
        }

        public static ColumnData FindColumn(string boardId, string columnId)
        {
            var board = FindBoard(boardId);
            if (board == null) return null;
            for (int i = 0; i < board.columns.Count; i++)
            {
                if (board.columns[i].id == columnId)
                    return board.columns[i];
            }
            return null;
        }

        public static TaskCardData FindTask(string boardId, string columnId, string taskId)
        {
            var col = FindColumn(boardId, columnId);
            if (col == null) return null;
            for (int i = 0; i < col.tasks.Count; i++)
            {
                if (col.tasks[i].id == taskId)
                    return col.tasks[i];
            }
            return null;
        }

        public static (ColumnData column, TaskCardData task) FindTaskAcrossColumns(string boardId, string taskId)
        {
            var board = FindBoard(boardId);
            if (board == null) return (null, null);
            for (int c = 0; c < board.columns.Count; c++)
            {
                for (int t = 0; t < board.columns[c].tasks.Count; t++)
                {
                    if (board.columns[c].tasks[t].id == taskId)
                        return (board.columns[c], board.columns[c].tasks[t]);
                }
            }
            return (null, null);
        }

        // --- Default data ---

        static void CreateDefaultBoard()
        {
            var board = new BoardData("My Board");
            board.columns.Add(new ColumnData("To Do"));
            board.columns.Add(new ColumnData("In Progress"));
            board.columns.Add(new ColumnData("Done"));
            s_Data.boards.Add(board);
            s_Data.activeBoardId = board.id;
        }
    }
}
