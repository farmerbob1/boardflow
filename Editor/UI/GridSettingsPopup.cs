using System;
using BoardFlow.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace BoardFlow.Editor.UI
{
    public class GridSettingsPopup : EditorWindow
    {
        GridSettings m_Settings;
        Action<GridSettings> m_OnApply;

        public static void Show(GridSettings currentSettings, Action<GridSettings> onApply)
        {
            var wnd = CreateInstance<GridSettingsPopup>();
            wnd.titleContent = new GUIContent("Grid Settings");
            wnd.m_Settings = new GridSettings
            {
                columnWidth = currentSettings.columnWidth,
                spacing = currentSettings.spacing,
                compactMode = currentSettings.compactMode
            };
            wnd.m_OnApply = onApply;
            wnd.ShowUtility();
            wnd.minSize = new Vector2(260, 140);
            wnd.maxSize = new Vector2(260, 140);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);

            m_Settings.columnWidth = EditorGUILayout.Slider("Column Width", m_Settings.columnWidth, 200f, 500f);
            m_Settings.spacing = EditorGUILayout.Slider("Spacing", m_Settings.spacing, 4f, 32f);
            m_Settings.compactMode = EditorGUILayout.Toggle("Compact Mode", m_Settings.compactMode);

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(80)))
            {
                m_OnApply?.Invoke(m_Settings);
                Close();
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
