using System;
using System.Collections.Generic;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class TaskCardData
    {
        public string id;
        public string title;
        public Priority priority;
        public List<ChecklistItemData> checklist;
        public long createdAt;
        public long modifiedAt;

        public TaskCardData()
        {
            id = Guid.NewGuid().ToString("N");
            title = "New Task";
            priority = Priority.None;
            checklist = new List<ChecklistItemData>();
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
