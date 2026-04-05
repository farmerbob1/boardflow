using System.Collections.Generic;
using BoardFlow.Editor.Data;
using BoardFlow.Editor.DragDrop;
using BoardFlow.Editor.Services;
using BoardFlow.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor
{
    public class BoardFlowWindow : EditorWindow
    {
        BoardFlowToolbar m_Toolbar;
        BoardView m_BoardView;
        DragState m_DragState;
        SelectionManager m_Selection;

        [MenuItem("Window/BoardFlow")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<BoardFlowWindow>();
            wnd.titleContent = new GUIContent("BoardFlow");
            wnd.minSize = new Vector2(600, 400);
        }

        void OnEnable()
        {
            BoardFlowDataService.Load();
            m_DragState = new DragState();
            m_Selection = new SelectionManager();
            BuildUI();
            UpdateWindowTitle();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            if (UndoService.Instance != null)
            {
                UndoService.RestoreFromProxy();
                RefreshToolbar();
                RebuildBoard();
            }
        }

        void BuildUI()
        {
            rootVisualElement.Clear();

            AddStyleSheet("BoardFlowVariables");
            AddStyleSheet("BoardFlow");
            AddStyleSheet("Toolbar");
            AddStyleSheet("Column");
            AddStyleSheet("Card");
            AddStyleSheet("DragDrop");
            AddStyleSheet("Label");
            AddStyleSheet("CardDetail");

            rootVisualElement.AddToClassList("boardflow-root");

            // Toolbar
            m_Toolbar = new BoardFlowToolbar();
            m_Toolbar.OnBoardSelected += OnBoardSelected;
            m_Toolbar.OnNewBoardClicked += OnNewBoard;
            m_Toolbar.OnRenameBoardClicked += OnRenameBoard;
            m_Toolbar.OnDeleteBoardClicked += OnDeleteBoard;
            m_Toolbar.OnAddColumnClicked += OnAddColumn;
            m_Toolbar.OnLabelsClicked += OnLabelsManager;
            m_Toolbar.OnFieldsClicked += OnFieldsManager;
            m_Toolbar.OnStatsClicked += OnStats;
            m_Toolbar.OnGridSettingsClicked += OnGridSettings;
            m_Toolbar.OnSearchChanged += OnSearchChanged;
            rootVisualElement.Add(m_Toolbar);

            // Board view
            m_BoardView = new BoardView();
            m_BoardView.OnAddTaskClicked += OnAddTask;
            m_BoardView.OnColumnContextMenu += OnColumnContextMenu;
            rootVisualElement.Add(m_BoardView);

            RefreshToolbar();
            RebuildBoard();
        }

        internal void RebuildUI()
        {
            BuildUI();
        }

        void RefreshToolbar()
        {
            var data = BoardFlowDataService.Data;
            m_Toolbar.SetBoards(data.boards, data.activeBoardId);
        }

        void RebuildBoard()
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            m_BoardView.SetBoard(board);
            WireCardEvents();
            ApplyGridSettings();
            ApplySelectionVisuals();
            UpdateWindowTitle();
        }

        void WireCardEvents()
        {
            foreach (var col in m_BoardView.Columns)
            {
                col.OnTitleDoubleClicked += BeginEditColumnTitle;
                col.OnCollapseToggled += OnColumnCollapseToggled;

                // Column drag manipulator
                var colDragManip = new ColumnDragManipulator(
                    () => m_BoardView.Columns,
                    OnColumnDrop
                );
                col.AddManipulator(colDragManip);

                var cards = col.GetCards();
                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    card.OnContextMenu += OnCardContextMenu;
                    card.OnTitleDoubleClicked += BeginEditCardTitle;
                    card.OnCardClicked += OnCardClicked;
                    card.OnChecklistToggled += OnChecklistToggled;
                    card.OnChecklistTextChanged += OnChecklistTextChanged;

                    // Card drag manipulator
                    var dragManip = new CardDragManipulator(
                        m_DragState,
                        () => m_BoardView.Columns,
                        OnCardDrop
                    );
                    card.AddManipulator(dragManip);
                }
            }
        }

        void ApplyGridSettings()
        {
            var data = BoardFlowDataService.Data;
            var settings = data.gridSettings;
            if (settings == null) return;

            var board = data.GetActiveBoard();
            if (board == null) return;

            int colIdx = 0;
            foreach (var col in m_BoardView.Columns)
            {
                var colData = colIdx < board.columns.Count ? board.columns[colIdx] : null;
                if (colData != null && colData.isCollapsed)
                {
                    col.style.width = 40;
                    col.style.minWidth = 40;
                    col.style.maxWidth = 40;
                }
                else
                {
                    col.style.width = settings.columnWidth;
                    col.style.minWidth = settings.columnWidth;
                    col.style.maxWidth = settings.columnWidth;
                }
                col.style.marginRight = settings.spacing;
                colIdx++;
            }
        }

        void ApplySelectionVisuals()
        {
            // Prune selection for deleted tasks
            var validIds = new HashSet<string>();
            foreach (var col in m_BoardView.Columns)
            {
                foreach (var card in col.GetCards())
                {
                    validIds.Add(card.TaskId);
                    card.SetSelected(m_Selection.IsSelected(card.TaskId));
                }
            }
            m_Selection.PruneInvalid(validIds);
        }

        void UpdateWindowTitle()
        {
            var data = BoardFlowDataService.Data;
            var board = data?.GetActiveBoard();
            bool hasCritical = board != null && BoardFlowDataService.HasCriticalTasks(board.id);
            titleContent = new GUIContent(hasCritical ? "BoardFlow \u26A0" : "BoardFlow");
        }

        // --- Drag-and-drop ---

        void OnCardDrop(string fromColumnId, string toColumnId, string taskId, int insertIndex)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            m_Selection.ClearSelection();
            UndoService.RecordState("Move Task");
            BoardFlowDataService.MoveTask(board.id, fromColumnId, toColumnId, taskId, insertIndex);
            RebuildBoard();
        }

        void OnColumnDrop(string columnId, int insertIndex)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Move Column");
            BoardFlowDataService.MoveColumn(board.id, columnId, insertIndex);
            RebuildBoard();
        }

        // --- Toolbar events ---

        void OnBoardSelected(string boardName)
        {
            var data = BoardFlowDataService.Data;
            for (int i = 0; i < data.boards.Count; i++)
            {
                if (data.boards[i].name == boardName)
                {
                    m_Selection.ClearSelection();
                    UndoService.RecordState("Switch Board");
                    BoardFlowDataService.SetActiveBoard(data.boards[i].id);
                    RebuildBoard();
                    return;
                }
            }
        }

        void OnNewBoard()
        {
            UndoService.RecordState("New Board");
            BoardFlowDataService.CreateBoard("New Board");
            RefreshToolbar();
            RebuildBoard();
        }

        void OnRenameBoard()
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            var newName = EditorInputDialog.Show("Rename Board", "Board name:", board.name);
            if (!string.IsNullOrWhiteSpace(newName) && newName != board.name)
            {
                UndoService.RecordState("Rename Board");
                BoardFlowDataService.RenameBoard(board.id, newName);
                RefreshToolbar();
            }
        }

        void OnDeleteBoard()
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            if (data.boards.Count <= 1)
            {
                EditorUtility.DisplayDialog("Cannot Delete", "You must have at least one board.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Delete Board",
                $"Are you sure you want to delete \"{board.name}\" and all its contents?", "Delete", "Cancel"))
            {
                UndoService.RecordState("Delete Board");
                BoardFlowDataService.DeleteBoard(board.id);
                RefreshToolbar();
                RebuildBoard();
            }
        }

        void OnAddColumn()
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Add Column");
            BoardFlowDataService.CreateColumn(board.id, "New Column");
            RebuildBoard();
        }

        void OnLabelsManager()
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            LabelManagerPopup.Show(board.id, () => RebuildBoard());
        }

        void OnFieldsManager()
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            CustomFieldManagerPopup.Show(board.id, () => RebuildBoard());
        }

        void OnStats()
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            BoardStatisticsPopup.Show(board.id);
        }

        void OnGridSettings()
        {
            GridSettingsPopup.Show(BoardFlowDataService.Data.gridSettings, settings =>
            {
                UndoService.RecordState("Change Grid Settings");
                BoardFlowDataService.SaveGridSettings(settings);
                ApplyGridSettings();
            });
        }

        void OnSearchChanged(string searchText)
        {
            m_Selection.ClearSelection();
            m_BoardView.SetSearchFilter(searchText);
        }

        // --- Card events ---

        void OnCardClicked(TaskCardElement card, EventModifiers modifiers)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            if ((modifiers & EventModifiers.Control) != 0)
            {
                m_Selection.ToggleSelect(card.TaskId, card.ColumnId);
                ApplySelectionVisuals();
                return;
            }

            if ((modifiers & EventModifiers.Shift) != 0)
            {
                m_Selection.RangeSelect(card.TaskId, card.ColumnId, m_BoardView.Columns);
                ApplySelectionVisuals();
                return;
            }

            // No modifier — open detail view
            m_Selection.ClearSelection();
            ApplySelectionVisuals();
            CardDetailWindow.Show(board.id, card.TaskId, () => RebuildBoard());
        }

        void OnAddTask(string columnId)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Add Task");
            BoardFlowDataService.CreateTask(board.id, columnId, "New Task");
            RebuildBoard();
        }

        void OnCardContextMenu(TaskCardElement card)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            // Multi-selection context menu
            if (m_Selection.Count > 1 && m_Selection.IsSelected(card.TaskId))
            {
                ShowBulkContextMenu(board);
                return;
            }

            var menu = new GenericMenu();

            var (cardCol, cardTask) = BoardFlowDataService.FindTaskAcrossColumns(board.id, card.TaskId);

            // Priority submenu
            menu.AddItem(new GUIContent("Priority/None"), cardTask != null && cardTask.priority == Priority.None, () => SetCardPriority(card, Priority.None));
            menu.AddItem(new GUIContent("Priority/Low"), cardTask != null && cardTask.priority == Priority.Low, () => SetCardPriority(card, Priority.Low));
            menu.AddItem(new GUIContent("Priority/Medium"), cardTask != null && cardTask.priority == Priority.Medium, () => SetCardPriority(card, Priority.Medium));
            menu.AddItem(new GUIContent("Priority/High"), cardTask != null && cardTask.priority == Priority.High, () => SetCardPriority(card, Priority.High));
            menu.AddItem(new GUIContent("Priority/Critical"), cardTask != null && cardTask.priority == Priority.Critical, () => SetCardPriority(card, Priority.Critical));

            // Card Color submenu
            if (cardTask != null)
            {
                string currentColor = cardTask.color ?? string.Empty;
                var currentMode = cardTask.colorMode;

                menu.AddItem(new GUIContent("Card Color/Color/Red"), currentColor == "#e74c3c", () => SetCardColor(card, "#e74c3c", currentMode));
                menu.AddItem(new GUIContent("Card Color/Color/Orange"), currentColor == "#e67e22", () => SetCardColor(card, "#e67e22", currentMode));
                menu.AddItem(new GUIContent("Card Color/Color/Yellow"), currentColor == "#f1c40f", () => SetCardColor(card, "#f1c40f", currentMode));
                menu.AddItem(new GUIContent("Card Color/Color/Green"), currentColor == "#2ecc71", () => SetCardColor(card, "#2ecc71", currentMode));
                menu.AddItem(new GUIContent("Card Color/Color/Blue"), currentColor == "#3498db", () => SetCardColor(card, "#3498db", currentMode));
                menu.AddItem(new GUIContent("Card Color/Color/Purple"), currentColor == "#9b59b6", () => SetCardColor(card, "#9b59b6", currentMode));
                menu.AddItem(new GUIContent("Card Color/Color/Pink"), currentColor == "#e91e63", () => SetCardColor(card, "#e91e63", currentMode));
                menu.AddItem(new GUIContent("Card Color/Color/Teal"), currentColor == "#00bcd4", () => SetCardColor(card, "#00bcd4", currentMode));
                menu.AddItem(new GUIContent("Card Color/Mode/Stripe"), currentMode == CardColorMode.Stripe, () => SetCardColor(card, currentColor, CardColorMode.Stripe));
                menu.AddItem(new GUIContent("Card Color/Mode/Background"), currentMode == CardColorMode.Background, () => SetCardColor(card, currentColor, CardColorMode.Background));
                menu.AddItem(new GUIContent("Card Color/Clear"), false, () => SetCardColor(card, string.Empty, CardColorMode.None));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Edit Title"), false, () => BeginEditCardTitle(card));
            menu.AddItem(new GUIContent("Open Details"), false, () =>
            {
                m_Selection.ClearSelection();
                ApplySelectionVisuals();
                CardDetailWindow.Show(board.id, card.TaskId, () => RebuildBoard());
            });
            menu.AddItem(new GUIContent("Add Checklist Item"), false, () => AddChecklistItemToCard(card));

            // Labels submenu
            if (cardTask != null && board.labels.Count > 0)
            {
                menu.AddSeparator("");
                for (int i = 0; i < board.labels.Count; i++)
                {
                    var label = board.labels[i];
                    var colId = cardCol.id;
                    bool hasLabel = cardTask.labelIds.Contains(label.id);
                    menu.AddItem(new GUIContent($"Labels/{label.name}"), hasLabel, () =>
                    {
                        if (hasLabel)
                        {
                            UndoService.RecordState("Remove Label");
                            BoardFlowDataService.RemoveLabelFromTask(board.id, colId, card.TaskId, label.id);
                        }
                        else
                        {
                            UndoService.RecordState("Add Label");
                            BoardFlowDataService.AddLabelToTask(board.id, colId, card.TaskId, label.id);
                        }
                        RebuildBoard();
                    });
                }
            }

            // Checklist item management
            if (cardTask != null && cardTask.checklist.Count > 0)
            {
                menu.AddSeparator("");
                for (int i = 0; i < cardTask.checklist.Count; i++)
                {
                    var item = cardTask.checklist[i];
                    var colId = cardCol.id;
                    menu.AddItem(new GUIContent($"Checklist/Delete \"{TruncateText(item.text, 20)}\""), false, () =>
                    {
                        UndoService.RecordState("Delete Checklist Item");
                        BoardFlowDataService.RemoveChecklistItem(board.id, colId, card.TaskId, item.id);
                        RebuildBoard();
                    });
                }
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Delete Task"), false, () => DeleteCard(card));

            menu.ShowAsContext();
        }

        void ShowBulkContextMenu(BoardData board)
        {
            var menu = new GenericMenu();
            int count = m_Selection.Count;

            menu.AddItem(new GUIContent($"Delete {count} Selected Tasks"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Delete Tasks",
                    $"Delete {count} selected tasks?", "Delete", "Cancel"))
                {
                    UndoService.RecordState("Delete Selected Tasks");
                    var toDelete = new List<(string columnId, string taskId)>();
                    foreach (var col in m_BoardView.Columns)
                    {
                        foreach (var c in col.GetCards())
                        {
                            if (m_Selection.IsSelected(c.TaskId))
                                toDelete.Add((col.ColumnId, c.TaskId));
                        }
                    }
                    BoardFlowDataService.DeleteTasks(board.id, toDelete);
                    m_Selection.ClearSelection();
                    RebuildBoard();
                }
            });

            menu.AddSeparator("");

            // Move to column submenu
            for (int i = 0; i < board.columns.Count; i++)
            {
                var targetCol = board.columns[i];
                menu.AddItem(new GUIContent($"Move Selected To/{targetCol.title}"), false, () =>
                {
                    UndoService.RecordState("Move Selected Tasks");
                    var toMove = new List<(string fromColumnId, string taskId)>();
                    foreach (var col in m_BoardView.Columns)
                    {
                        foreach (var c in col.GetCards())
                        {
                            if (m_Selection.IsSelected(c.TaskId) && col.ColumnId != targetCol.id)
                                toMove.Add((col.ColumnId, c.TaskId));
                        }
                    }
                    BoardFlowDataService.MoveTasks(board.id, toMove, targetCol.id);
                    m_Selection.ClearSelection();
                    RebuildBoard();
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear Selection"), false, () =>
            {
                m_Selection.ClearSelection();
                ApplySelectionVisuals();
            });

            menu.ShowAsContext();
        }

        void SetCardPriority(TaskCardElement card, Priority priority)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Set Priority");
            BoardFlowDataService.SetTaskPriority(board.id, card.ColumnId, card.TaskId, priority);
            RebuildBoard();
        }

        void SetCardColor(TaskCardElement card, string color, CardColorMode mode)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            if (!string.IsNullOrEmpty(color) && mode == CardColorMode.None)
                mode = CardColorMode.Stripe;
            if (string.IsNullOrEmpty(color) && mode != CardColorMode.None)
                color = "#3498db";

            UndoService.RecordState("Set Card Color");
            BoardFlowDataService.SetTaskColor(board.id, card.ColumnId, card.TaskId, color, mode);
            RebuildBoard();
        }

        void BeginEditCardTitle(TaskCardElement card)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            var (col, task) = BoardFlowDataService.FindTaskAcrossColumns(board.id, card.TaskId);
            if (task == null) return;

            var titleLabel = card.Q<Label>(className: "task-card-title");
            if (titleLabel == null) return;

            var textField = new TextField();
            textField.AddToClassList("task-card-title-field");
            textField.value = task.title;
            textField.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

            var parent = titleLabel.parent;
            int index = parent.IndexOf(titleLabel);
            parent.Remove(titleLabel);
            parent.Insert(index, textField);

            textField.schedule.Execute(() => textField.Focus());

            bool committed = false;
            void CommitEdit()
            {
                if (committed) return;
                committed = true;

                var newTitle = textField.value;
                if (!string.IsNullOrWhiteSpace(newTitle) && newTitle != task.title)
                {
                    UndoService.RecordState("Edit Task Title");
                    BoardFlowDataService.UpdateTaskTitle(board.id, col.id, task.id, newTitle);
                }
                RebuildBoard();
            }

            textField.RegisterCallback<FocusOutEvent>(evt => CommitEdit());
            textField.RegisterCallback<KeyDownEvent>(evt =>
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
                    RebuildBoard();
                }
            });
        }

        void AddChecklistItemToCard(TaskCardElement card)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Add Checklist Item");
            BoardFlowDataService.AddChecklistItem(board.id, card.ColumnId, card.TaskId, "New item");
            RebuildBoard();
        }

        void DeleteCard(TaskCardElement card)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Delete Task");
            BoardFlowDataService.DeleteTask(board.id, card.ColumnId, card.TaskId);
            RebuildBoard();
        }

        void OnChecklistToggled(string taskId, string itemId, bool value)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            var (col, task) = BoardFlowDataService.FindTaskAcrossColumns(board.id, taskId);
            if (col == null || task == null) return;

            UndoService.RecordState("Toggle Checklist");
            BoardFlowDataService.ToggleChecklistItem(board.id, col.id, taskId, itemId);
            RebuildBoard();
        }

        void OnChecklistTextChanged(string taskId, string itemId, string newText)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            var (col, task) = BoardFlowDataService.FindTaskAcrossColumns(board.id, taskId);
            if (col == null || task == null) return;

            UndoService.RecordState("Edit Checklist Item");
            BoardFlowDataService.UpdateChecklistItemText(board.id, col.id, taskId, itemId, newText);
        }

        // --- Column events ---

        void OnColumnCollapseToggled(ColumnElement column, bool collapsed)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Toggle Column Collapse");
            BoardFlowDataService.SetColumnCollapsed(board.id, column.ColumnId, collapsed);
            RebuildBoard();
        }

        void OnColumnContextMenu(ColumnElement column)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            var colData = BoardFlowDataService.FindColumn(board.id, column.ColumnId);

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Rename Column"), false, () => BeginEditColumnTitle(column));

            // WIP Limit
            menu.AddItem(new GUIContent("Set WIP Limit..."), false, () =>
            {
                var currentLimit = colData != null ? colData.wipLimit : 0;
                var result = EditorInputDialog.Show("WIP Limit", "Max tasks (0 = unlimited):", currentLimit.ToString());
                if (result != null && int.TryParse(result, out int newLimit))
                {
                    UndoService.RecordState("Set WIP Limit");
                    BoardFlowDataService.SetColumnWipLimit(board.id, column.ColumnId, newLimit);
                    RebuildBoard();
                }
            });

            // Sort submenu
            menu.AddSeparator("");
            var currentSort = colData != null ? colData.sortMode : SortMode.Manual;
            menu.AddItem(new GUIContent("Sort By/Manual (Drag Order)"), currentSort == SortMode.Manual, () => SetColumnSort(board.id, column.ColumnId, SortMode.Manual));
            menu.AddItem(new GUIContent("Sort By/Priority (High to Low)"), currentSort == SortMode.PriorityDesc, () => SetColumnSort(board.id, column.ColumnId, SortMode.PriorityDesc));
            menu.AddItem(new GUIContent("Sort By/Created (Newest First)"), currentSort == SortMode.CreatedNewest, () => SetColumnSort(board.id, column.ColumnId, SortMode.CreatedNewest));
            menu.AddItem(new GUIContent("Sort By/Created (Oldest First)"), currentSort == SortMode.CreatedOldest, () => SetColumnSort(board.id, column.ColumnId, SortMode.CreatedOldest));
            menu.AddItem(new GUIContent("Sort By/Alphabetical (A-Z)"), currentSort == SortMode.AlphabeticalAZ, () => SetColumnSort(board.id, column.ColumnId, SortMode.AlphabeticalAZ));
            menu.AddItem(new GUIContent("Sort By/Alphabetical (Z-A)"), currentSort == SortMode.AlphabeticalZA, () => SetColumnSort(board.id, column.ColumnId, SortMode.AlphabeticalZA));

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Column"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Delete Column",
                    "Are you sure you want to delete this column and all its tasks?", "Delete", "Cancel"))
                {
                    UndoService.RecordState("Delete Column");
                    BoardFlowDataService.DeleteColumn(board.id, column.ColumnId);
                    RebuildBoard();
                    RefreshToolbar();
                }
            });

            menu.ShowAsContext();
        }

        void SetColumnSort(string boardId, string columnId, SortMode mode)
        {
            UndoService.RecordState("Change Sort Mode");
            BoardFlowDataService.SetColumnSortMode(boardId, columnId, mode);
            RebuildBoard();
        }

        void BeginEditColumnTitle(ColumnElement column)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            var colData = BoardFlowDataService.FindColumn(board.id, column.ColumnId);
            if (colData == null) return;

            var titleLabel = column.Q<Label>(className: "column-title");
            if (titleLabel == null) return;

            var textField = new TextField();
            textField.value = colData.title;
            textField.style.flexGrow = 1;
            textField.AddToClassList("column-title-field");

            var parent = titleLabel.parent;
            int index = parent.IndexOf(titleLabel);
            parent.Remove(titleLabel);
            parent.Insert(index, textField);

            textField.schedule.Execute(() => textField.Focus());

            bool committed = false;
            void CommitEdit()
            {
                if (committed) return;
                committed = true;

                var newTitle = textField.value;
                if (!string.IsNullOrWhiteSpace(newTitle) && newTitle != colData.title)
                {
                    UndoService.RecordState("Rename Column");
                    BoardFlowDataService.RenameColumn(board.id, column.ColumnId, newTitle);
                }
                RebuildBoard();
                RefreshToolbar();
            }

            textField.RegisterCallback<FocusOutEvent>(evt => CommitEdit());
            textField.RegisterCallback<KeyDownEvent>(evt =>
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
                    RebuildBoard();
                }
            });
        }

        // --- Keyboard shortcuts ---

        void OnGUI()
        {
            var evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown) return;

            if (evt.control && evt.keyCode == KeyCode.N)
            {
                evt.Use();
                OnNewBoard();
            }
            else if (evt.control && evt.keyCode == KeyCode.F)
            {
                evt.Use();
                var searchField = rootVisualElement.Q<TextField>(className: "toolbar-search");
                searchField?.Focus();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                evt.Use();
                m_Selection.ClearSelection();
                ApplySelectionVisuals();
                m_Toolbar.ClearSearch();
                m_BoardView.SetSearchFilter(null);
                rootVisualElement.focusController?.focusedElement?.Blur();
            }
            else if (evt.keyCode == KeyCode.Delete && m_Selection.Count > 0)
            {
                evt.Use();
                var data = BoardFlowDataService.Data;
                var board = data.GetActiveBoard();
                if (board != null)
                    ShowBulkContextMenu(board);
            }
        }

        // --- Public API ---

        public void RefreshBoard()
        {
            BoardFlowDataService.Load();
            RefreshToolbar();
            RebuildBoard();
        }

        // --- Helpers ---

        static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        // --- Style loading ---

        void AddStyleSheet(string name)
        {
            var sheet = LoadStyleSheet(name);
            if (sheet != null)
                rootVisualElement.styleSheets.Add(sheet);
        }

        static StyleSheet LoadStyleSheet(string name)
        {
            var guids = AssetDatabase.FindAssets($"t:StyleSheet {name}");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.Contains("com.boardflow"))
                    return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }
            return null;
        }
    }
}
