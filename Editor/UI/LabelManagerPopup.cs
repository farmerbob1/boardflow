using System;
using System.Collections.Generic;
using BoardFlow.Editor.Data;
using BoardFlow.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace BoardFlow.Editor.UI
{
    public class LabelManagerPopup : EditorWindow
    {
        string m_BoardId;
        Action m_OnChanged;
        Vector2 m_ScrollPos;

        string m_NewLabelName = "New Label";
        Color m_NewLabelColor = new Color(0f, 0.47f, 0.83f);

        public static void Show(string boardId, Action onChanged)
        {
            var wnd = CreateInstance<LabelManagerPopup>();
            wnd.titleContent = new GUIContent("Manage Labels");
            wnd.m_BoardId = boardId;
            wnd.m_OnChanged = onChanged;
            wnd.ShowUtility();
            wnd.minSize = new Vector2(300, 250);
            wnd.maxSize = new Vector2(300, 500);
        }

        void OnGUI()
        {
            var board = BoardFlowDataService.FindBoard(m_BoardId);
            if (board == null)
            {
                EditorGUILayout.HelpBox("Board not found.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Board Labels", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            string deleteId = null;
            for (int i = 0; i < board.labels.Count; i++)
            {
                var label = board.labels[i];
                EditorGUILayout.BeginHorizontal();

                Color c;
                if (!ColorUtility.TryParseHtmlString(label.color, out c))
                    c = Color.blue;

                var newColor = EditorGUILayout.ColorField(GUIContent.none, c, false, false, false, GUILayout.Width(30));
                var newName = EditorGUILayout.TextField(label.name);

                if (GUILayout.Button("X", GUILayout.Width(22)))
                    deleteId = label.id;

                EditorGUILayout.EndHorizontal();

                // Apply changes
                string newHex = "#" + ColorUtility.ToHtmlStringRGB(newColor);
                if (newName != label.name || newHex != label.color)
                {
                    UndoService.RecordState("Edit Label");
                    BoardFlowDataService.UpdateLabel(m_BoardId, label.id, newName, newHex);
                    m_OnChanged?.Invoke();
                }
            }

            if (deleteId != null)
            {
                UndoService.RecordState("Delete Label");
                BoardFlowDataService.DeleteLabel(m_BoardId, deleteId);
                m_OnChanged?.Invoke();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Add New Label", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            m_NewLabelColor = EditorGUILayout.ColorField(GUIContent.none, m_NewLabelColor, false, false, false, GUILayout.Width(30));
            m_NewLabelName = EditorGUILayout.TextField(m_NewLabelName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Add Label"))
            {
                if (!string.IsNullOrWhiteSpace(m_NewLabelName))
                {
                    string hex = "#" + ColorUtility.ToHtmlStringRGB(m_NewLabelColor);
                    UndoService.RecordState("Create Label");
                    BoardFlowDataService.CreateLabel(m_BoardId, m_NewLabelName, hex);
                    m_NewLabelName = "New Label";
                    m_NewLabelColor = new Color(0f, 0.47f, 0.83f);
                    m_OnChanged?.Invoke();
                }
            }

            EditorGUILayout.Space(4);
        }
    }
}
