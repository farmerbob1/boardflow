using System;
using System.Collections.Generic;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class BoardData
    {
        public string id;
        public string name;
        public List<ColumnData> columns;
        public long createdAt;
        public long modifiedAt;

        public BoardData()
        {
            id = Guid.NewGuid().ToString("N");
            name = "New Board";
            columns = new List<ColumnData>();
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            modifiedAt = createdAt;
        }

        public BoardData(string name) : this()
        {
            this.name = name;
        }

        public void Touch()
        {
            modifiedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
