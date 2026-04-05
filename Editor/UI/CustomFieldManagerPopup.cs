using System;
using BoardFlow.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace BoardFlow.Editor.UI
{
    public class CustomFieldManagerPopup : EditorWindow
    {
        string m_BoardId;
        Action m_OnChanged;
        Vector2 m_ScrollPos;
        string m_NewFieldName = "New Field";

        public static void Show(string boardId, Action onChanged)
        {
            var win = CreateInstance<CustomFieldManagerPopup>();
            win.m_BoardId = boardId;
            win.m_OnChanged = onChanged;
            win.titleContent = new GUIContent("Custom Fields");
            win.minSize = new Vector2(300, 200);
            win.maxSize = new Vector2(300, 400);
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

            EditorGUILayout.LabelField("Custom Fields", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            string deleteId = null;

            for (int i = 0; i < board.customFields.Count; i++)
            {
                var field = board.customFields[i];

                EditorGUILayout.BeginHorizontal();

                var newName = EditorGUILayout.TextField(field.name);
                if (newName != field.name)
                {
                    UndoService.RecordState("Rename Custom Field");
                    BoardFlowDataService.RenameCustomField(m_BoardId, field.id, newName);
                    m_OnChanged?.Invoke();
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                    deleteId = field.id;

                EditorGUILayout.EndHorizontal();
            }

            if (deleteId != null)
            {
                UndoService.RecordState("Delete Custom Field");
                BoardFlowDataService.DeleteCustomField(m_BoardId, deleteId);
                m_OnChanged?.Invoke();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Add New Field", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            m_NewFieldName = EditorGUILayout.TextField(m_NewFieldName);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add Field"))
            {
                if (!string.IsNullOrWhiteSpace(m_NewFieldName))
                {
                    UndoService.RecordState("Add Custom Field");
                    BoardFlowDataService.CreateCustomField(m_BoardId, m_NewFieldName);
                    m_NewFieldName = "New Field";
                    m_OnChanged?.Invoke();
                }
            }
        }
    }
}
