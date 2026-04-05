using System.Collections.Generic;
using BoardFlow.Editor.UI;

namespace BoardFlow.Editor.UI
{
    public class SelectionManager
    {
        readonly HashSet<string> m_SelectedIds = new HashSet<string>();
        string m_LastSelectedId;
        string m_LastSelectedColumnId;

        public int Count => m_SelectedIds.Count;

        public bool IsSelected(string taskId)
        {
            return m_SelectedIds.Contains(taskId);
        }

        public void SingleSelect(string taskId, string columnId)
        {
            m_SelectedIds.Clear();
            m_SelectedIds.Add(taskId);
            m_LastSelectedId = taskId;
            m_LastSelectedColumnId = columnId;
        }

        public void ToggleSelect(string taskId, string columnId)
        {
            if (m_SelectedIds.Contains(taskId))
                m_SelectedIds.Remove(taskId);
            else
                m_SelectedIds.Add(taskId);

            m_LastSelectedId = taskId;
            m_LastSelectedColumnId = columnId;
        }

        public void RangeSelect(string taskId, string columnId, IReadOnlyList<ColumnElement> columns)
        {
            // Only range-select within the same column as the last selection
            if (m_LastSelectedColumnId != columnId || string.IsNullOrEmpty(m_LastSelectedId))
            {
                SingleSelect(taskId, columnId);
                return;
            }

            // Find the column
            ColumnElement targetCol = null;
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].ColumnId == columnId)
                {
                    targetCol = columns[i];
                    break;
                }
            }

            if (targetCol == null)
            {
                SingleSelect(taskId, columnId);
                return;
            }

            var cards = targetCol.GetCards();
            int fromIdx = -1, toIdx = -1;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].TaskId == m_LastSelectedId) fromIdx = i;
                if (cards[i].TaskId == taskId) toIdx = i;
            }

            if (fromIdx < 0 || toIdx < 0)
            {
                SingleSelect(taskId, columnId);
                return;
            }

            int start = fromIdx < toIdx ? fromIdx : toIdx;
            int end = fromIdx < toIdx ? toIdx : fromIdx;

            for (int i = start; i <= end; i++)
                m_SelectedIds.Add(cards[i].TaskId);
        }

        public void ClearSelection()
        {
            m_SelectedIds.Clear();
            m_LastSelectedId = null;
            m_LastSelectedColumnId = null;
        }

        public IReadOnlyCollection<string> GetSelectedIds()
        {
            return m_SelectedIds;
        }

        public void PruneInvalid(HashSet<string> validIds)
        {
            m_SelectedIds.IntersectWith(validIds);
            if (m_LastSelectedId != null && !m_SelectedIds.Contains(m_LastSelectedId))
            {
                m_LastSelectedId = null;
                m_LastSelectedColumnId = null;
            }
        }
    }
}
