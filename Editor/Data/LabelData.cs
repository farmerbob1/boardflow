using System;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class LabelData
    {
        public string id;
        public string name;
        public string color;

        public LabelData()
        {
            id = Guid.NewGuid().ToString("N");
            name = "New Label";
            color = "#0078d4";
        }

        public LabelData(string name, string color) : this()
        {
            this.name = name;
            this.color = color;
        }
    }
}
