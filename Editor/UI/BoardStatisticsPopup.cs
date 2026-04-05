using System;
using BoardFlow.Editor.Data;
using BoardFlow.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace BoardFlow.Editor.UI
{
    public class BoardStatisticsPopup : EditorWindow
    {
        string m_BoardId;
        Vector2 m_ScrollPos;

        public static void Show(string boardId)
        {
            var win = CreateInstance<BoardStatisticsPopup>();
            win.m_BoardId = boardId;
            win.titleContent = new GUIContent("Board Statistics");
            win.minSize = new Vector2(300, 350);
            win.maxSize = new Vector2(300, 500);
            win.ShowUtility();
        }

        void OnGUI()
        {
            var board = BoardFlowDataService.FindBoard(m_BoardId);
            if (board == null)
            {
                EditorGUILayout.HelpBox("Board not found.", MessageType.Warning);
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            EditorGUILayout.LabelField(board.name, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Total tasks
            int totalTasks = 0;
            for (int c = 0; c < board.columns.Count; c++)
                totalTasks += board.columns[c].tasks.Count;

            EditorGUILayout.LabelField("Total Tasks", totalTasks.ToString());
            EditorGUILayout.Space(8);

            // Tasks per column
            EditorGUILayout.LabelField("Tasks per Column", EditorStyles.boldLabel);
            for (int c = 0; c < board.columns.Count; c++)
            {
                int count = board.columns[c].tasks.Count;
                float pct = totalTasks > 0 ? (float)count / totalTasks * 100f : 0f;
                string wipInfo = board.columns[c].wipLimit > 0
                    ? $"  (limit: {board.columns[c].wipLimit})"
                    : "";
                EditorGUILayout.LabelField(
                    $"  {board.columns[c].title}",
                    $"{count} ({pct:F0}%){wipInfo}");
            }

            EditorGUILayout.Space(8);

            // Priority breakdown
            EditorGUILayout.LabelField("Priority Breakdown", EditorStyles.boldLabel);
            int[] priorityCounts = new int[5];
            for (int c = 0; c < board.columns.Count; c++)
            {
                for (int t = 0; t < board.columns[c].tasks.Count; t++)
                {
                    int p = (int)board.columns[c].tasks[t].priority;
                    if (p >= 0 && p < 5)
                        priorityCounts[p]++;
                }
            }

            string[] priorityNames = { "None", "Low", "Medium", "High", "Critical" };
            for (int i = 4; i >= 0; i--)
            {
                if (priorityCounts[i] > 0)
                    EditorGUILayout.LabelField($"  {priorityNames[i]}", priorityCounts[i].ToString());
            }

            EditorGUILayout.Space(8);

            // Checklist stats
            EditorGUILayout.LabelField("Checklist Progress", EditorStyles.boldLabel);
            int totalItems = 0, completedItems = 0;
            for (int c = 0; c < board.columns.Count; c++)
            {
                for (int t = 0; t < board.columns[c].tasks.Count; t++)
                {
                    var cl = board.columns[c].tasks[t].checklist;
                    if (cl == null) continue;
                    totalItems += cl.Count;
                    for (int ci = 0; ci < cl.Count; ci++)
                    {
                        if (cl[ci].isCompleted) completedItems++;
                    }
                }
            }

            if (totalItems > 0)
            {
                float pct = (float)completedItems / totalItems * 100f;
                EditorGUILayout.LabelField($"  {completedItems}/{totalItems} items completed ({pct:F0}%)");
            }
            else
            {
                EditorGUILayout.LabelField("  No checklist items");
            }

            EditorGUILayout.Space(8);

            // Labels usage
            if (board.labels.Count > 0)
            {
                EditorGUILayout.LabelField("Label Usage", EditorStyles.boldLabel);
                for (int l = 0; l < board.labels.Count; l++)
                {
                    int labelCount = 0;
                    for (int c = 0; c < board.columns.Count; c++)
                    {
                        for (int t = 0; t < board.columns[c].tasks.Count; t++)
                        {
                            if (board.columns[c].tasks[t].labelIds.Contains(board.labels[l].id))
                                labelCount++;
                        }
                    }
                    EditorGUILayout.LabelField($"  {board.labels[l].name}", $"{labelCount} tasks");
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Close"))
                Close();
        }
    }
}
