using System;
using System.Collections.Generic;
using BoardFlow.Editor.Data;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class TaskCardElement : VisualElement
    {
        readonly Label m_TitleLabel;
        readonly PriorityBarElement m_PriorityBar;
        readonly VisualElement m_ChecklistContainer;
        readonly Label m_ChecklistSummary;

        public string TaskId { get; private set; }
        public string ColumnId { get; set; }

        public event Action<TaskCardElement> OnContextMenu;
        public event Action<TaskCardElement> OnTitleDoubleClicked;
        public event Action<string, string, bool> OnChecklistToggled;
        public event Action<string, string, string> OnChecklistTextChanged;

        public TaskCardElement(TaskCardData data, string columnId)
        {
            AddToClassList("task-card");
            TaskId = data.id;
            ColumnId = columnId;

            // Priority bar at top
            m_PriorityBar = new PriorityBarElement(data.priority);
            Add(m_PriorityBar);

            // Card body
            var body = new VisualElement();
            body.AddToClassList("task-card-body");
            Add(body);

            // Title row with drag handle
            var titleRow = new VisualElement();
            titleRow.AddToClassList("task-card-title-row");
            body.Add(titleRow);

            var dragHandle = new Label("\u2847");
            dragHandle.AddToClassList("task-card-drag-handle");
            dragHandle.tooltip = "Drag to reorder";
            titleRow.Add(dragHandle);

            m_TitleLabel = new Label(data.title);
            m_TitleLabel.AddToClassList("task-card-title");
            m_TitleLabel.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    OnTitleDoubleClicked?.Invoke(this);
            });
            titleRow.Add(m_TitleLabel);

            // Checklist section
            if (data.checklist != null && data.checklist.Count > 0)
            {
                var checklistSection = new VisualElement();
                checklistSection.AddToClassList("task-card-checklist-section");
                body.Add(checklistSection);

                // Progress summary
                int completed = 0;
                for (int i = 0; i < data.checklist.Count; i++)
                {
                    if (data.checklist[i].isCompleted) completed++;
                }

                m_ChecklistSummary = new Label($"{completed}/{data.checklist.Count}");
                m_ChecklistSummary.AddToClassList("checklist-summary");
                checklistSection.Add(m_ChecklistSummary);

                // Progress bar
                var progressBar = new VisualElement();
                progressBar.AddToClassList("checklist-progress-bar");
                checklistSection.Add(progressBar);

                var progressFill = new VisualElement();
                progressFill.AddToClassList("checklist-progress-fill");
                float pct = data.checklist.Count > 0 ? (float)completed / data.checklist.Count * 100f : 0f;
                progressFill.style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
                if (completed == data.checklist.Count && data.checklist.Count > 0)
                    progressFill.AddToClassList("checklist-progress-complete");
                progressBar.Add(progressFill);

                // Individual items
                m_ChecklistContainer = new VisualElement();
                m_ChecklistContainer.AddToClassList("checklist-container");
                checklistSection.Add(m_ChecklistContainer);

                for (int i = 0; i < data.checklist.Count; i++)
                {
                    var item = new ChecklistItemElement(data.checklist[i]);
                    item.OnToggled += (itemId, value) => OnChecklistToggled?.Invoke(TaskId, itemId, value);
                    item.OnTextChanged += (itemId, newText) => OnChecklistTextChanged?.Invoke(TaskId, itemId, newText);
                    m_ChecklistContainer.Add(item);
                }
            }

            // Context menu
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                OnContextMenu?.Invoke(this);
            }));
        }

        public void SetTitle(string title)
        {
            m_TitleLabel.text = title;
        }

        public void SetPriority(Priority priority)
        {
            m_PriorityBar.SetPriority(priority);
        }
    }
}
