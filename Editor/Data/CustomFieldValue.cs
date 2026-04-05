using System;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class CustomFieldValue
    {
        public string fieldId;
        public string value;

        public CustomFieldValue()
        {
            fieldId = string.Empty;
            value = string.Empty;
        }

        public CustomFieldValue(string fieldId, string value)
        {
            this.fieldId = fieldId;
            this.value = value;
        }
    }
}
