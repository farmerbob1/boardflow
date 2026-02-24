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
        BoardData m_Board;

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
            m_Board = board;

            for (int i = 0; i < board.columns.Count; i++)
            {
                AddColumnElement(board.columns[i], board.id, board.labels);
            }
        }

        void AddColumnElement(ColumnData data, string boardId, List<LabelData> boardLabels)
        {
            var column = new ColumnElement(data, boardId, boardLabels);
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
                        bool visible = false;

                        // Match title
                        var titleLabel = cards[t].Q<Label>(className: "task-card-title");
                        if (titleLabel != null && titleLabel.text.ToLowerInvariant().Contains(lower))
                            visible = true;

                        // Match description preview
                        if (!visible)
                        {
                            var descLabel = cards[t].Q<Label>(className: "task-card-description");
                            if (descLabel != null && descLabel.text.ToLowerInvariant().Contains(lower))
                                visible = true;
                        }

                        // Match label names via data
                        if (!visible && m_Board != null)
                        {
                            var labelsRow = cards[t].Q(className: "task-card-labels");
                            if (labelsRow != null)
                            {
                                foreach (var child in labelsRow.Children())
                                {
                                    if (child is LabelChipElement chip)
                                    {
                                        var chipText = chip.Q<Label>(className: "label-chip-text");
                                        if (chipText != null && chipText.text.ToLowerInvariant().Contains(lower))
                                        {
                                            visible = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        cards[t].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }
        }
    }
}
