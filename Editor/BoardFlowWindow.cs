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
            BuildUI();

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

            rootVisualElement.AddToClassList("boardflow-root");

            // Toolbar
            m_Toolbar = new BoardFlowToolbar();
            m_Toolbar.OnBoardSelected += OnBoardSelected;
            m_Toolbar.OnNewBoardClicked += OnNewBoard;
            m_Toolbar.OnRenameBoardClicked += OnRenameBoard;
            m_Toolbar.OnDeleteBoardClicked += OnDeleteBoard;
            m_Toolbar.OnAddColumnClicked += OnAddColumn;
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
        }

        void WireCardEvents()
        {
            foreach (var col in m_BoardView.Columns)
            {
                col.OnTitleDoubleClicked += BeginEditColumnTitle;

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
            var settings = BoardFlowDataService.Data.gridSettings;
            if (settings == null) return;

            foreach (var col in m_BoardView.Columns)
            {
                col.style.width = settings.columnWidth;
                col.style.minWidth = settings.columnWidth;
                col.style.maxWidth = settings.columnWidth;
                col.style.marginRight = settings.spacing;
            }
        }

        // --- Drag-and-drop ---

        void OnCardDrop(string fromColumnId, string toColumnId, string taskId, int insertIndex)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

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

            var newName = board.name;
            // Use a simple input dialog
            newName = EditorInputDialog.Show("Rename Board", "Board name:", board.name);
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
            m_BoardView.SetSearchFilter(searchText);
        }

        // --- Card events ---

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

            var menu = new GenericMenu();

            // Priority submenu
            menu.AddItem(new GUIContent("Priority/None"), false, () => SetCardPriority(card, Priority.None));
            menu.AddItem(new GUIContent("Priority/Low"), false, () => SetCardPriority(card, Priority.Low));
            menu.AddItem(new GUIContent("Priority/Medium"), false, () => SetCardPriority(card, Priority.Medium));
            menu.AddItem(new GUIContent("Priority/High"), false, () => SetCardPriority(card, Priority.High));
            menu.AddItem(new GUIContent("Priority/Critical"), false, () => SetCardPriority(card, Priority.Critical));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Edit Title"), false, () => BeginEditCardTitle(card));
            menu.AddItem(new GUIContent("Add Checklist Item"), false, () => AddChecklistItemToCard(card));

            // Checklist item management
            var (cardCol, cardTask) = BoardFlowDataService.FindTaskAcrossColumns(board.id, card.TaskId);
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

        void SetCardPriority(TaskCardElement card, Priority priority)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            UndoService.RecordState("Set Priority");
            BoardFlowDataService.SetTaskPriority(board.id, card.ColumnId, card.TaskId, priority);
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

        void OnColumnContextMenu(ColumnElement column)
        {
            var data = BoardFlowDataService.Data;
            var board = data.GetActiveBoard();
            if (board == null) return;

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Rename Column"), false, () => BeginEditColumnTitle(column));
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
                m_Toolbar.ClearSearch();
                m_BoardView.SetSearchFilter(null);
                rootVisualElement.focusController?.focusedElement?.Blur();
            }
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
