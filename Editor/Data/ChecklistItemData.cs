using System;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class ChecklistItemData
    {
        public string id;
        public string text;
        public bool isCompleted;

        public ChecklistItemData()
        {
            id = Guid.NewGuid().ToString("N");
            text = string.Empty;
            isCompleted = false;
        }

        public ChecklistItemData(string text)
        {
            id = Guid.NewGuid().ToString("N");
            this.text = text;
            isCompleted = false;
        }
    }
}
