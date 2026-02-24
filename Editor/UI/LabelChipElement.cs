using BoardFlow.Editor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class LabelChipElement : VisualElement
    {
        readonly Label m_NameLabel;

        public string LabelId { get; private set; }

        public LabelChipElement(LabelData data, bool active = true)
        {
            AddToClassList("label-chip");
            LabelId = data.id;

            if (ColorUtility.TryParseHtmlString(data.color, out var color))
                style.backgroundColor = color;
            else
                style.backgroundColor = new Color(0f, 0.47f, 0.83f);

            m_NameLabel = new Label(data.name);
            m_NameLabel.AddToClassList("label-chip-text");
            Add(m_NameLabel);

            if (!active)
                AddToClassList("label-chip--inactive");
        }

        public void SetActive(bool active)
        {
            if (active)
                RemoveFromClassList("label-chip--inactive");
            else
                AddToClassList("label-chip--inactive");
        }
    }
}
