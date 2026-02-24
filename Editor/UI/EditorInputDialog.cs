using UnityEditor;
using UnityEngine;

namespace BoardFlow.Editor.UI
{
    public class EditorInputDialog : EditorWindow
    {
        string m_Value;
        string m_Label;
        bool m_FirstFrame = true;

        static string s_Result;

        public static string Show(string title, string label, string defaultValue)
        {
            s_Result = null;
            var wnd = CreateInstance<EditorInputDialog>();
            wnd.titleContent = new GUIContent(title);
            wnd.m_Value = defaultValue ?? string.Empty;
            wnd.m_Label = label;
            wnd.minSize = new Vector2(300, 80);
            wnd.maxSize = new Vector2(300, 80);
            wnd.ShowModalUtility();
            return s_Result;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);

            GUI.SetNextControlName("InputField");
            m_Value = EditorGUILayout.TextField(m_Label, m_Value);

            if (m_FirstFrame)
            {
                EditorGUI.FocusTextInControl("InputField");
                m_FirstFrame = false;
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("OK", GUILayout.Width(80)) || (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)))
            {
                s_Result = m_Value;
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)) || (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Escape))
            {
                s_Result = null;
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
