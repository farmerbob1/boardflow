using System;
using System.Collections.Generic;
using BoardFlow.Editor.Data;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class ColumnElement : VisualElement
    {
        readonly Label m_TitleLabel;
        readonly Label m_CountBadge;
        readonly VisualElement m_CardContainer;
        readonly Button m_AddTaskButton;
        readonly Button m_CollapseToggle;

        public string ColumnId { get; private set; }
        public VisualElement CardContainer => m_CardContainer;

        public event Action<string> OnAddTaskClicked;
        public event Action<ColumnElement> OnContextMenu;
        public event Action<ColumnElement> OnTitleDoubleClicked;
        public event Action<ColumnElement, bool> OnCollapseToggled;

        public ColumnElement(ColumnData data, string boardId, List<LabelData> boardLabels)
        {
            AddToClassList("column");
            ColumnId = data.id;

            if (data.isCollapsed)
            {
                AddToClassList("column--collapsed");
                tooltip = data.title;
            }

            // Column header
            var header = new VisualElement();
            header.AddToClassList("column-header");
            Add(header);

            // Collapse toggle
            m_CollapseToggle = new Button(() =>
            {
                OnCollapseToggled?.Invoke(this, !data.isCollapsed);
            });
            m_CollapseToggle.text = data.isCollapsed ? "\u25B6" : "\u25BC";
            m_CollapseToggle.AddToClassList("collapse-toggle");
            m_CollapseToggle.tooltip = data.isCollapsed ? "Expand column" : "Collapse column";
            header.Add(m_CollapseToggle);

            var dragHandle = new Label("\u2630");
            dragHandle.AddToClassList("column-drag-handle");
            dragHandle.tooltip = "Drag to reorder";
            header.Add(dragHandle);

            var headerLeft = new VisualElement();
            headerLeft.AddToClassList("column-header-left");
            header.Add(headerLeft);

            m_TitleLabel = new Label(data.title);
            m_TitleLabel.AddToClassList("column-title");
            m_TitleLabel.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    OnTitleDoubleClicked?.Invoke(this);
            });
            headerLeft.Add(m_TitleLabel);

            // Count badge with WIP limit
            int count = data.tasks.Count;
            string badgeText = data.wipLimit > 0 ? $"{count}/{data.wipLimit}" : count.ToString();
            m_CountBadge = new Label(badgeText);
            m_CountBadge.AddToClassList("column-count-badge");

            if (data.wipLimit > 0 && count >= data.wipLimit)
            {
                m_CountBadge.AddToClassList("column-wip-warning");
                if (count > data.wipLimit)
                    AddToClassList("column-wip-exceeded");
            }

            headerLeft.Add(m_CountBadge);

            // Column header context menu
            header.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                OnContextMenu?.Invoke(this);
            }));

            // Scrollable card container
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.AddToClassList("column-scroll");
            Add(scrollView);

            m_CardContainer = scrollView.contentContainer;
            m_CardContainer.AddToClassList("column-card-container");

            // Add task cards (sorting is handled by the service layer)
            for (int i = 0; i < data.tasks.Count; i++)
            {
                var card = new TaskCardElement(data.tasks[i], data.id, boardLabels);
                m_CardContainer.Add(card);
            }

            // Add task button
            m_AddTaskButton = new Button(() => OnAddTaskClicked?.Invoke(ColumnId));
            m_AddTaskButton.text = "+ Add Task";
            m_AddTaskButton.AddToClassList("add-task-button");
            Add(m_AddTaskButton);
        }

        public void UpdateCount(int count)
        {
            m_CountBadge.text = count.ToString();
        }

        public List<TaskCardElement> GetCards()
        {
            var cards = new List<TaskCardElement>();
            foreach (var child in m_CardContainer.Children())
            {
                if (child is TaskCardElement card)
                    cards.Add(card);
            }
            return cards;
        }
    }
}
