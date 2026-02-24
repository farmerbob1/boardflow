using BoardFlow.Editor.Data;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    [UnityEngine.UIElements.UxmlElement]
    public partial class PriorityBarElement : VisualElement
    {
        public PriorityBarElement()
        {
            AddToClassList("priority-bar");
        }

        public PriorityBarElement(Priority priority) : this()
        {
            SetPriority(priority);
        }

        public void SetPriority(Priority priority)
        {
            RemoveFromClassList("priority-none");
            RemoveFromClassList("priority-low");
            RemoveFromClassList("priority-medium");
            RemoveFromClassList("priority-high");
            RemoveFromClassList("priority-critical");

            switch (priority)
            {
                case Priority.Low:
                    AddToClassList("priority-low");
                    break;
                case Priority.Medium:
                    AddToClassList("priority-medium");
                    break;
                case Priority.High:
                    AddToClassList("priority-high");
                    break;
                case Priority.Critical:
                    AddToClassList("priority-critical");
                    break;
                default:
                    AddToClassList("priority-none");
                    break;
            }
        }
    }
}
