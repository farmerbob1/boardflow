using System;
using BoardFlow.Editor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class ChecklistItemElement : VisualElement
    {
        readonly Toggle m_Toggle;
        Label m_Label;
        TextField m_EditField;

        public string ItemId { get; private set; }

        public event Action<string, bool> OnToggled;
        public event Action<string, string> OnTextChanged;

        public ChecklistItemElement(ChecklistItemData data)
        {
            AddToClassList("checklist-item");
            ItemId = data.id;

            m_Toggle = new Toggle();
            m_Toggle.value = data.isCompleted;
            m_Toggle.AddToClassList("checklist-toggle");
            m_Toggle.RegisterValueChangedCallback(evt => OnToggled?.Invoke(ItemId, evt.newValue));
            Add(m_Toggle);

            m_Label = new Label(data.text);
            m_Label.AddToClassList("checklist-label");
            if (data.isCompleted)
                m_Label.AddToClassList("checklist-completed");
            m_Label.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    BeginEdit();
            });
            Add(m_Label);
        }

        void BeginEdit()
        {
            if (m_EditField != null) return;

            var currentText = m_Label.text;
            m_Label.style.display = DisplayStyle.None;

            m_EditField = new TextField();
            m_EditField.AddToClassList("checklist-edit-field");
            m_EditField.value = currentText;
            Add(m_EditField);

            m_EditField.schedule.Execute(() =>
            {
                m_EditField.Focus();
                m_EditField.SelectAll();
            });

            bool committed = false;
            void CommitEdit()
            {
                if (committed) return;
                committed = true;

                var newText = m_EditField.value;
                m_EditField.RemoveFromHierarchy();
                m_EditField = null;
                m_Label.style.display = DisplayStyle.Flex;

                if (!string.IsNullOrWhiteSpace(newText) && newText != currentText)
                {
                    m_Label.text = newText;
                    OnTextChanged?.Invoke(ItemId, newText);
                }
            }

            m_EditField.RegisterCallback<FocusOutEvent>(evt => CommitEdit());
            m_EditField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    evt.StopPropagation();
                    CommitEdit();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    evt.StopPropagation();
                    committed = true;
                    m_EditField.RemoveFromHierarchy();
                    m_EditField = null;
                    m_Label.style.display = DisplayStyle.Flex;
                }
            });
        }

        public void SetCompleted(bool completed)
        {
            m_Toggle.SetValueWithoutNotify(completed);
            if (completed)
                m_Label.AddToClassList("checklist-completed");
            else
                m_Label.RemoveFromClassList("checklist-completed");
        }
    }
}
