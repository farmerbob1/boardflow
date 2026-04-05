using System;
using System.Collections.Generic;
using BoardFlow.Editor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class TaskCardElement : VisualElement
    {
        readonly Label m_TitleLabel;
        readonly PriorityBarElement m_PriorityBar;
        readonly VisualElement m_ColorStripe;
        readonly VisualElement m_Body;
        readonly VisualElement m_ChecklistContainer;
        readonly Label m_ChecklistSummary;

        public string TaskId { get; private set; }
        public string ColumnId { get; set; }

        public event Action<TaskCardElement> OnContextMenu;
        public event Action<TaskCardElement> OnTitleDoubleClicked;
        public event Action<TaskCardElement, EventModifiers> OnCardClicked;
        public event Action<string, string, bool> OnChecklistToggled;
        public event Action<string, string, string> OnChecklistTextChanged;

        IVisualElementScheduledItem m_ClickTimer;
        bool m_DoubleClicked;

        public TaskCardElement(TaskCardData data, string columnId, List<LabelData> boardLabels)
        {
            AddToClassList("task-card");
            TaskId = data.id;
            ColumnId = columnId;

            // Priority bar at top
            m_PriorityBar = new PriorityBarElement(data.priority);
            Add(m_PriorityBar);

            // Color stripe (below priority bar)
            m_ColorStripe = new VisualElement();
            m_ColorStripe.AddToClassList("card-color-stripe");
            Add(m_ColorStripe);

            // Card body
            m_Body = new VisualElement();
            m_Body.AddToClassList("task-card-body");
            Add(m_Body);

            ApplyCardColor(data.color, data.colorMode);

            // Title row with drag handle
            var titleRow = new VisualElement();
            titleRow.AddToClassList("task-card-title-row");
            m_Body.Add(titleRow);

            var dragHandle = new Label("\u2847");
            dragHandle.AddToClassList("task-card-drag-handle");
            dragHandle.tooltip = "Drag to reorder";
            titleRow.Add(dragHandle);

            m_TitleLabel = new Label(data.title);
            m_TitleLabel.AddToClassList("task-card-title");
            m_TitleLabel.RegisterCallback<ClickEvent>(OnTitleClicked);
            titleRow.Add(m_TitleLabel);

            // Description preview (truncated, max 2 lines)
            if (!string.IsNullOrEmpty(data.description))
            {
                var descPreview = new Label(TruncateDescription(data.description, 80));
                descPreview.AddToClassList("task-card-description");
                m_Body.Add(descPreview);
            }

            // Label chips row
            if (boardLabels != null && data.labelIds != null && data.labelIds.Count > 0)
            {
                var labelsRow = new VisualElement();
                labelsRow.AddToClassList("task-card-labels");
                m_Body.Add(labelsRow);

                for (int i = 0; i < data.labelIds.Count; i++)
                {
                    var labelData = FindLabelById(boardLabels, data.labelIds[i]);
                    if (labelData != null)
                    {
                        var chip = new LabelChipElement(labelData);
                        labelsRow.Add(chip);
                    }
                }
            }

            // Checklist section
            if (data.checklist != null && data.checklist.Count > 0)
            {
                var checklistSection = new VisualElement();
                checklistSection.AddToClassList("task-card-checklist-section");
                m_Body.Add(checklistSection);

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

            // Click on card body for selection/open
            m_Body.RegisterCallback<ClickEvent>(OnBodyClicked);

            // Context menu
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                OnContextMenu?.Invoke(this);
            }));
        }

        void OnTitleClicked(ClickEvent evt)
        {
            if (evt.clickCount == 2)
            {
                m_DoubleClicked = true;
                m_ClickTimer?.Pause();
                OnTitleDoubleClicked?.Invoke(this);
                evt.StopPropagation();
                return;
            }

            // Single click on title is handled by OnBodyClicked
            // (the event bubbles up from title to body)
        }

        void OnBodyClicked(ClickEvent evt)
        {
            // Ignore double-clicks (handled by title for inline edit)
            if (evt.clickCount == 2) return;

            // Ignore clicks on interactive elements
            if (evt.target is Toggle || evt.target is Button) return;

            m_DoubleClicked = false;
            var modifiers = EventModifiers.None;
            if (evt.ctrlKey) modifiers |= EventModifiers.Control;
            if (evt.shiftKey) modifiers |= EventModifiers.Shift;
            m_ClickTimer = schedule.Execute(() =>
            {
                if (!m_DoubleClicked)
                    OnCardClicked?.Invoke(this, modifiers);
            });
            m_ClickTimer.ExecuteLater(300);
        }

        public void SimulateClick(bool ctrl, bool shift)
        {
            var modifiers = EventModifiers.None;
            if (ctrl) modifiers |= EventModifiers.Control;
            if (shift) modifiers |= EventModifiers.Shift;
            OnCardClicked?.Invoke(this, modifiers);
        }

        public void SetSelected(bool selected)
        {
            if (selected)
                AddToClassList("task-card--selected");
            else
                RemoveFromClassList("task-card--selected");
        }

        public void SetColor(string color, CardColorMode mode)
        {
            ApplyCardColor(color, mode);
        }

        void ApplyCardColor(string hexColor, CardColorMode mode)
        {
            // Reset
            m_ColorStripe.style.backgroundColor = StyleKeyword.Null;
            m_Body.style.backgroundColor = StyleKeyword.Null;
            m_ColorStripe.RemoveFromClassList("card-color-stripe--hidden");

            if (string.IsNullOrEmpty(hexColor) || mode == CardColorMode.None)
            {
                m_ColorStripe.AddToClassList("card-color-stripe--hidden");
                return;
            }

            Color parsed;
            if (!ColorUtility.TryParseHtmlString(hexColor, out parsed))
            {
                m_ColorStripe.AddToClassList("card-color-stripe--hidden");
                return;
            }

            if (mode == CardColorMode.Stripe)
            {
                m_ColorStripe.style.backgroundColor = parsed;
            }
            else if (mode == CardColorMode.Background)
            {
                m_ColorStripe.AddToClassList("card-color-stripe--hidden");
                parsed.a = 0.2f;
                m_Body.style.backgroundColor = parsed;
            }
        }

        public void SetTitle(string title)
        {
            m_TitleLabel.text = title;
        }

        public void SetPriority(Priority priority)
        {
            m_PriorityBar.SetPriority(priority);
        }

        static string TruncateDescription(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // Replace newlines for preview
            var flat = text.Replace("\n", " ").Replace("\r", "");
            return flat.Length <= maxLength ? flat : flat.Substring(0, maxLength) + "...";
        }

        static LabelData FindLabelById(List<LabelData> labels, string id)
        {
            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i].id == id)
                    return labels[i];
            }
            return null;
        }
    }
}
