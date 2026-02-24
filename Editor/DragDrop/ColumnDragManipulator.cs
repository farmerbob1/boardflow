using System;
using System.Collections.Generic;
using BoardFlow.Editor.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.DragDrop
{
    public class ColumnDragManipulator : PointerManipulator
    {
        const float k_DragThreshold = 5f;

        readonly Func<IReadOnlyList<ColumnElement>> m_GetColumns;
        readonly Action<string, int> m_OnDrop;

        VisualElement m_Ghost;
        VisualElement m_DropIndicator;
        Vector2 m_PointerStart;
        Vector2 m_Offset;
        bool m_PointerDown;
        bool m_IsDragging;
        int m_PointerId;
        int m_InsertIndex = -1;

        public ColumnDragManipulator(
            Func<IReadOnlyList<ColumnElement>> getColumns,
            Action<string, int> onDrop)
        {
            m_GetColumns = getColumns;
            m_OnDrop = onDrop;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            if (m_IsDragging) return;

            // Only start drag from the column header area
            var targetEl = evt.target as VisualElement;
            if (targetEl == null) return;

            bool inHeader = false;
            var walk = targetEl;
            while (walk != null && walk != target)
            {
                if (walk.ClassListContains("column-header"))
                {
                    inHeader = true;
                    break;
                }
                // Don't drag from interactive elements in the header
                if (walk is TextField || walk is Button)
                    return;
                // Don't drag when double-clicking to edit title
                if (walk.ClassListContains("column-title"))
                    return;
                walk = walk.parent;
            }

            if (!inHeader) return;

            m_PointerDown = true;
            m_PointerId = evt.pointerId;
            m_PointerStart = evt.position;

            target.CapturePointer(m_PointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!m_PointerDown) return;
            if (evt.pointerId != m_PointerId) return;

            var delta = (Vector2)evt.position - m_PointerStart;

            if (!m_IsDragging)
            {
                if (delta.magnitude < k_DragThreshold)
                    return;
                StartDrag(evt.position);
            }

            UpdateDrag(evt.position);
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!m_PointerDown) return;
            if (evt.pointerId != m_PointerId) return;

            if (m_IsDragging)
                CompleteDrag();

            m_PointerDown = false;

            if (target.HasPointerCapture(m_PointerId))
                target.ReleasePointer(m_PointerId);

            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (m_IsDragging)
                Cleanup();
            m_PointerDown = false;
        }

        void StartDrag(Vector2 pointerPos)
        {
            var col = target as ColumnElement;
            if (col == null) return;

            m_IsDragging = true;

            var colWorldPos = col.worldBound.position;
            m_Offset = pointerPos - colWorldPos;

            // Ghost
            m_Ghost = new VisualElement();
            m_Ghost.AddToClassList("drag-ghost-column");
            m_Ghost.style.width = col.worldBound.width;
            m_Ghost.style.height = col.worldBound.height;
            m_Ghost.style.position = Position.Absolute;
            m_Ghost.pickingMode = PickingMode.Ignore;

            var titleLabel = col.Q<Label>(className: "column-title");
            if (titleLabel != null)
            {
                var label = new Label(titleLabel.text);
                label.AddToClassList("drag-ghost-title");
                label.style.paddingLeft = 8;
                label.style.paddingTop = 8;
                m_Ghost.Add(label);
            }

            var root = col.panel.visualTree;
            root.Add(m_Ghost);

            // Drop indicator (vertical line between columns)
            m_DropIndicator = new VisualElement();
            m_DropIndicator.AddToClassList("column-drop-indicator");
            m_DropIndicator.pickingMode = PickingMode.Ignore;

            col.AddToClassList("column-dragging");

            UpdateGhostPosition(pointerPos);
        }

        void UpdateDrag(Vector2 pointerPos)
        {
            UpdateGhostPosition(pointerPos);
            UpdateDropTarget(pointerPos);
        }

        void UpdateGhostPosition(Vector2 pointerPos)
        {
            if (m_Ghost == null) return;
            m_Ghost.style.left = pointerPos.x - m_Offset.x;
            m_Ghost.style.top = pointerPos.y - m_Offset.y;
        }

        void UpdateDropTarget(Vector2 pointerPos)
        {
            var columns = m_GetColumns();
            int insertIndex = columns.Count;

            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i] == target) continue;

                var bounds = columns[i].worldBound;
                float midX = bounds.x + bounds.width * 0.5f;

                if (pointerPos.x < midX)
                {
                    insertIndex = i;
                    break;
                }
            }

            m_InsertIndex = insertIndex;

            // Position drop indicator in the columns container
            var container = (target as ColumnElement)?.parent;
            if (container == null) return;

            if (m_DropIndicator.parent != container)
            {
                m_DropIndicator.RemoveFromHierarchy();
                container.Add(m_DropIndicator);
            }

            m_DropIndicator.style.position = Position.Relative;
            if (insertIndex < container.childCount)
                container.Insert(insertIndex, m_DropIndicator);
        }

        void CompleteDrag()
        {
            var col = target as ColumnElement;
            if (col != null)
                col.RemoveFromClassList("column-dragging");

            if (m_InsertIndex >= 0)
            {
                m_OnDrop?.Invoke(col?.ColumnId, m_InsertIndex);
            }

            Cleanup();
        }

        void Cleanup()
        {
            m_Ghost?.RemoveFromHierarchy();
            m_Ghost = null;
            m_DropIndicator?.RemoveFromHierarchy();
            m_DropIndicator = null;
            m_IsDragging = false;
            m_InsertIndex = -1;

            var col = target as ColumnElement;
            if (col != null)
                col.RemoveFromClassList("column-dragging");
        }
    }
}
