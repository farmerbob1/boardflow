using System;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class GridSettings
    {
        public float columnWidth;
        public float spacing;
        public bool compactMode;

        public GridSettings()
        {
            columnWidth = 280f;
            spacing = 10f;
            compactMode = false;
        }
    }
}
