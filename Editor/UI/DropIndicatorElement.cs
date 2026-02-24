using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class DropIndicatorElement : VisualElement
    {
        public DropIndicatorElement()
        {
            AddToClassList("drop-indicator");
            pickingMode = PickingMode.Ignore;
        }
    }
}
