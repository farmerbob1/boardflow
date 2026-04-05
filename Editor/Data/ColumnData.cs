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
        public int wipLimit;
        public bool isCollapsed;
        public SortMode sortMode;

        public ColumnData()
        {
            id = Guid.NewGuid().ToString("N");
            title = "New Column";
            tasks = new List<TaskCardData>();
            wipLimit = 0;
            isCollapsed = false;
            sortMode = SortMode.Manual;
        }

        public ColumnData(string title) : this()
        {
            this.title = title;
        }
    }
}
