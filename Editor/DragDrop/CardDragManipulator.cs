using System;
using System.Collections.Generic;
using BoardFlow.Editor.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.DragDrop
{
    public class CardDragManipulator : PointerManipulator
    {
        const float k_DragThreshold = 5f;

        readonly DragState m_DragState;
        readonly Func<IReadOnlyList<ColumnElement>> m_GetColumns;
        readonly Action<string, string, string, int> m_OnDrop;

        Vector2 m_PointerStart;
        Vector2 m_CardOffset;
        bool m_PointerDown;
        int m_PointerId;

        public CardDragManipulator(
            DragState dragState,
            Func<IReadOnlyList<ColumnElement>> getColumns,
            Action<string, string, string, int> onDrop)
        {
            m_DragState = dragState;
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
            if (m_DragState.IsDragging) return;

            // Don't drag if clicking on interactive elements
            if (evt.target is Toggle || evt.target is TextField || evt.target is Button)
                return;

            var targetEl = evt.target as VisualElement;
            if (targetEl != null)
            {
                // Check parents for interactive elements or editable labels
                var parent = targetEl;
                while (parent != null && parent != target)
                {
                    if (parent is Toggle || parent is TextField || parent is Button
                        || parent is ChecklistItemElement
                        || parent.ClassListContains("task-card-title"))
                        return;
                    parent = parent.parent;
                }
            }

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

            if (!m_DragState.IsDragging)
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

            if (m_DragState.IsDragging)
            {
                CompleteDrag();
            }

            m_PointerDown = false;

            if (target.HasPointerCapture(m_PointerId))
                target.ReleasePointer(m_PointerId);

            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (m_DragState.IsDragging)
            {
                m_DragState.Reset();
            }
            m_PointerDown = false;
        }

        void StartDrag(Vector2 pointerPos)
        {
            var card = target as TaskCardElement;
            if (card == null) return;

            m_DragState.IsDragging = true;
            m_DragState.SourceCard = card;
            m_DragState.SourceColumnId = card.ColumnId;
            m_DragState.StartPointerPosition = pointerPos;

            // Calculate offset from card top-left to pointer
            var cardWorldPos = card.worldBound.position;
            m_CardOffset = pointerPos - cardWorldPos;

            // Create ghost element
            var ghost = new VisualElement();
            ghost.AddToClassList("drag-ghost");
            ghost.style.width = card.worldBound.width;
            ghost.style.minHeight = card.worldBound.height;
            ghost.style.position = Position.Absolute;
            ghost.pickingMode = PickingMode.Ignore;

            // Clone card content appearance
            var titleLabel = card.Q<Label>(className: "task-card-title");
            if (titleLabel != null)
            {
                var label = new Label(titleLabel.text);
                label.AddToClassList("drag-ghost-title");
                ghost.Add(label);
            }

            var root = card.panel.visualTree;
            root.Add(ghost);
            m_DragState.Ghost = ghost;

            // Create drop indicator
            m_DragState.DropIndicator = new DropIndicatorElement();
            m_DragState.DropIndicator.style.position = Position.Absolute;

            // Dim original card
            card.AddToClassList("card-dragging");

            UpdateGhostPosition(pointerPos);
        }

        void UpdateDrag(Vector2 pointerPos)
        {
            UpdateGhostPosition(pointerPos);
            UpdateDropTarget(pointerPos);
        }

        void UpdateGhostPosition(Vector2 pointerPos)
        {
            if (m_DragState.Ghost == null) return;
            m_DragState.Ghost.style.left = pointerPos.x - m_CardOffset.x;
            m_DragState.Ghost.style.top = pointerPos.y - m_CardOffset.y;
        }

        void UpdateDropTarget(Vector2 pointerPos)
        {
            var columns = m_GetColumns();
            ColumnElement hitColumn = null;

            // Find which column the pointer is over
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].worldBound.Contains(pointerPos))
                {
                    hitColumn = columns[i];
                    break;
                }
            }

            if (hitColumn == null)
            {
                m_DragState.DropIndicator?.RemoveFromHierarchy();
                m_DragState.TargetColumn = null;
                m_DragState.InsertIndex = -1;
                return;
            }

            m_DragState.TargetColumn = hitColumn;

            // Find insert index by comparing pointer Y with card midpoints
            var cards = hitColumn.GetCards();
            int insertIndex = cards.Count;

            for (int i = 0; i < cards.Count; i++)
            {
                // Skip the source card in index calculation
                if (cards[i] == m_DragState.SourceCard) continue;

                var cardBound = cards[i].worldBound;
                float midY = cardBound.y + cardBound.height * 0.5f;

                if (pointerPos.y < midY)
                {
                    insertIndex = i;
                    break;
                }
            }

            m_DragState.InsertIndex = insertIndex;

            // Position drop indicator
            var container = hitColumn.CardContainer;
            if (m_DragState.DropIndicator.parent != container)
            {
                m_DragState.DropIndicator.RemoveFromHierarchy();
                container.Add(m_DragState.DropIndicator);
            }

            // Place indicator at correct position
            m_DragState.DropIndicator.style.position = Position.Relative;
            m_DragState.DropIndicator.SendToBack();

            // Reorder: place indicator at insertIndex
            if (insertIndex < container.childCount)
                container.Insert(insertIndex, m_DragState.DropIndicator);
        }

        void CompleteDrag()
        {
            var card = m_DragState.SourceCard;
            if (card != null)
                card.RemoveFromClassList("card-dragging");

            if (m_DragState.TargetColumn != null && m_DragState.InsertIndex >= 0)
            {
                m_OnDrop?.Invoke(
                    m_DragState.SourceColumnId,
                    m_DragState.TargetColumn.ColumnId,
                    card?.TaskId,
                    m_DragState.InsertIndex
                );
            }

            m_DragState.Reset();
        }
    }
}
