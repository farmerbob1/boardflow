using System;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class CustomFieldDefinition
    {
        public string id;
        public string name;

        public CustomFieldDefinition()
        {
            id = Guid.NewGuid().ToString("N");
            name = "New Field";
        }

        public CustomFieldDefinition(string name) : this()
        {
            this.name = name;
        }
    }
}
