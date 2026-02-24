using System;
using System.Collections.Generic;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class ColumnData
    {
        public string id;
        public string title;
        public List<TaskCardData> tasks;

        public ColumnData()
        {
            id = Guid.NewGuid().ToString("N");
            title = "New Column";
            tasks = new List<TaskCardData>();
        }

        public ColumnData(string title) : this()
        {
            this.title = title;
        }
    }
}
