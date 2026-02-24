using BoardFlow.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace BoardFlow.Editor
{
    public class UndoService : ScriptableObject
    {
        [SerializeField] string jsonSnapshot;

        static UndoService s_Instance;

        public static UndoService Instance
        {
            get
            {
                if (s_Instance == null)
                    Initialize();
                return s_Instance;
            }
        }

        static void Initialize()
        {
            s_Instance = CreateInstance<UndoService>();
            s_Instance.hideFlags = HideFlags.HideAndDontSave;
            s_Instance.jsonSnapshot = BoardFlowDataService.GetDataAsJson();
        }

        public static void RecordState(string operationName)
        {
            var inst = Instance;
            Undo.RecordObject(inst, "BoardFlow: " + operationName);
            inst.CaptureState();
        }

        void CaptureState()
        {
            jsonSnapshot = BoardFlowDataService.GetDataAsJson();
        }

        public static void RestoreFromProxy()
        {
            if (s_Instance == null) return;
            BoardFlowDataService.SetDataFromJson(s_Instance.jsonSnapshot);
            BoardFlowDataService.Save();
        }
    }
}
