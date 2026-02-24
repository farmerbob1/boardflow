using System;
using System.Collections.Generic;
using BoardFlow.Editor.Data;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class BoardView : VisualElement
    {
        readonly ScrollView m_ScrollView;
        readonly VisualElement m_ColumnsContainer;
        readonly List<ColumnElement> m_Columns = new List<ColumnElement>();

        string m_BoardId;

        public string BoardId => m_BoardId;
        public IReadOnlyList<ColumnElement> Columns => m_Columns;

        public event Action<string> OnAddTaskClicked;
        public event Action<ColumnElement> OnColumnContextMenu;

        public BoardView()
        {
            AddToClassList("board-view");

            m_ScrollView = new ScrollView(ScrollViewMode.Horizontal);
            m_ScrollView.AddToClassList("board-scroll-view");
            Add(m_ScrollView);

            m_ColumnsContainer = m_ScrollView.contentContainer;
            m_ColumnsContainer.AddToClassList("board-columns-container");
        }

        public void SetBoard(BoardData board)
        {
            m_Columns.Clear();
            m_ColumnsContainer.Clear();

            if (board == null) return;

            m_BoardId = board.id;

            for (int i = 0; i < board.columns.Count; i++)
            {
                AddColumnElement(board.columns[i], board.id);
            }
        }

        void AddColumnElement(ColumnData data, string boardId)
        {
            var column = new ColumnElement(data, boardId);
            column.OnAddTaskClicked += colId => OnAddTaskClicked?.Invoke(colId);
            column.OnContextMenu += col => OnColumnContextMenu?.Invoke(col);

            m_Columns.Add(column);
            m_ColumnsContainer.Add(column);
        }

        public ColumnElement FindColumn(string columnId)
        {
            for (int i = 0; i < m_Columns.Count; i++)
            {
                if (m_Columns[i].ColumnId == columnId)
                    return m_Columns[i];
            }
            return null;
        }

        public void SetSearchFilter(string searchText)
        {
            bool hasFilter = !string.IsNullOrWhiteSpace(searchText);
            string lower = hasFilter ? searchText.ToLowerInvariant() : null;

            for (int c = 0; c < m_Columns.Count; c++)
            {
                var cards = m_Columns[c].GetCards();
                for (int t = 0; t < cards.Count; t++)
                {
                    if (!hasFilter)
                    {
                        cards[t].style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        // Check card visibility - we store title in the label
                        var titleLabel = cards[t].Q<Label>(className: "task-card-title");
                        bool visible = titleLabel != null && titleLabel.text.ToLowerInvariant().Contains(lower);
                        cards[t].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }
        }
    }
}
