using System;
using System.Collections.Generic;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class TaskCardData
    {
        public string id;
        public string title;
        public string description;
        public Priority priority;
        public List<ChecklistItemData> checklist;
        public List<string> labelIds;
        public string color;
        public CardColorMode colorMode;
        public long createdAt;
        public long modifiedAt;

        public TaskCardData()
        {
            id = Guid.NewGuid().ToString("N");
            title = "New Task";
            description = string.Empty;
            priority = Priority.None;
            checklist = new List<ChecklistItemData>();
            labelIds = new List<string>();
            color = string.Empty;
            colorMode = CardColorMode.None;
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            modifiedAt = createdAt;
        }

        public TaskCardData(string title) : this()
        {
            this.title = title;
        }

        public void Touch()
        {
            modifiedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
