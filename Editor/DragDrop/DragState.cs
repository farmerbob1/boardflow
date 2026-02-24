using BoardFlow.Editor.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.DragDrop
{
    public class DragState
    {
        public TaskCardElement SourceCard;
        public string SourceColumnId;
        public VisualElement Ghost;
        public DropIndicatorElement DropIndicator;
        public ColumnElement TargetColumn;
        public int InsertIndex;
        public bool IsDragging;
        public Vector2 StartPointerPosition;

        public void Reset()
        {
            Ghost?.RemoveFromHierarchy();
            DropIndicator?.RemoveFromHierarchy();
            SourceCard = null;
            SourceColumnId = null;
            Ghost = null;
            DropIndicator = null;
            TargetColumn = null;
            InsertIndex = -1;
            IsDragging = false;
        }
    }
}
