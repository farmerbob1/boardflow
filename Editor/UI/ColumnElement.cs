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

        public string ColumnId { get; private set; }
        public VisualElement CardContainer => m_CardContainer;

        public event Action<string> OnAddTaskClicked;
        public event Action<ColumnElement> OnContextMenu;
        public event Action<ColumnElement> OnTitleDoubleClicked;

        public ColumnElement(ColumnData data, string boardId, List<LabelData> boardLabels)
        {
            AddToClassList("column");
            ColumnId = data.id;

            // Column header
            var header = new VisualElement();
            header.AddToClassList("column-header");
            Add(header);

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

            m_CountBadge = new Label(data.tasks.Count.ToString());
            m_CountBadge.AddToClassList("column-count-badge");
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

            // Add task cards
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
