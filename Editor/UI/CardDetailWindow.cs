using System;
using System.Collections.Generic;
using BoardFlow.Editor.Data;
using BoardFlow.Editor.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class CardDetailWindow : EditorWindow
    {
        string m_BoardId;
        string m_TaskId;
        Action m_OnChanged;

        TextField m_TitleField;
        EnumField m_PriorityField;
        TextField m_DescriptionField;
        VisualElement m_LabelsContainer;
        VisualElement m_ChecklistContainer;

        public static void Show(string boardId, string taskId, Action onChanged)
        {
            var wnd = GetWindow<CardDetailWindow>(utility: true, title: "Card Details");
            wnd.m_BoardId = boardId;
            wnd.m_TaskId = taskId;
            wnd.m_OnChanged = onChanged;
            wnd.minSize = new Vector2(380, 450);
            wnd.BuildContent();
        }

        void OnEnable()
        {
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
                BuildContent();
                m_OnChanged?.Invoke();
            }
        }

        (ColumnData col, TaskCardData task) GetTask()
        {
            return BoardFlowDataService.FindTaskAcrossColumns(m_BoardId, m_TaskId);
        }

        void BuildContent()
        {
            rootVisualElement.Clear();

            // Load stylesheets
            AddStyleSheet("BoardFlowVariables");
            AddStyleSheet("CardDetail");
            AddStyleSheet("Label");

            var (col, task) = GetTask();
            if (col == null || task == null)
            {
                rootVisualElement.Add(new Label("Task not found. It may have been deleted."));
                return;
            }

            var board = BoardFlowDataService.FindBoard(m_BoardId);
            if (board == null) return;

            var root = new ScrollView(ScrollViewMode.Vertical);
            root.AddToClassList("card-detail-root");
            rootVisualElement.Add(root);

            // --- Title ---
            var titleSection = new VisualElement();
            titleSection.AddToClassList("card-detail-section");
            root.Add(titleSection);

            var titleHeader = new Label("TITLE");
            titleHeader.AddToClassList("card-detail-section-header");
            titleSection.Add(titleHeader);

            m_TitleField = new TextField();
            m_TitleField.AddToClassList("card-detail-title");
            m_TitleField.value = task.title;
            m_TitleField.isDelayed = true;
            m_TitleField.RegisterValueChangedCallback(evt =>
            {
                var (c, t) = GetTask();
                if (t == null || string.IsNullOrWhiteSpace(evt.newValue)) return;
                if (evt.newValue != t.title)
                {
                    UndoService.RecordState("Edit Task Title");
                    BoardFlowDataService.UpdateTaskTitle(m_BoardId, c.id, m_TaskId, evt.newValue);
                    m_OnChanged?.Invoke();
                }
            });
            titleSection.Add(m_TitleField);

            // --- Priority ---
            var prioritySection = new VisualElement();
            prioritySection.AddToClassList("card-detail-section");
            root.Add(prioritySection);

            var priorityHeader = new Label("PRIORITY");
            priorityHeader.AddToClassList("card-detail-section-header");
            prioritySection.Add(priorityHeader);

            m_PriorityField = new EnumField(task.priority);
            m_PriorityField.AddToClassList("card-detail-priority");
            m_PriorityField.RegisterValueChangedCallback(evt =>
            {
                var (c, t) = GetTask();
                if (t == null) return;
                var newPriority = (Priority)evt.newValue;
                if (newPriority != t.priority)
                {
                    UndoService.RecordState("Set Priority");
                    BoardFlowDataService.SetTaskPriority(m_BoardId, c.id, m_TaskId, newPriority);
                    m_OnChanged?.Invoke();
                }
            });
            prioritySection.Add(m_PriorityField);

            // --- Color ---
            var colorSection = new VisualElement();
            colorSection.AddToClassList("card-detail-section");
            root.Add(colorSection);

            var colorHeader = new Label("COLOR");
            colorHeader.AddToClassList("card-detail-section-header");
            colorSection.Add(colorHeader);

            var colorRow = new VisualElement();
            colorRow.AddToClassList("card-detail-color-row");
            colorSection.Add(colorRow);

            // Color picker via IMGUIContainer
            Color currentColor;
            if (!string.IsNullOrEmpty(task.color) && ColorUtility.TryParseHtmlString(task.color, out currentColor))
            { }
            else
                currentColor = Color.white;

            Color pickerColor = currentColor;
            var colorPicker = new IMGUIContainer(() =>
            {
                var newColor = EditorGUILayout.ColorField(GUIContent.none, pickerColor, false, false, false, GUILayout.Width(36), GUILayout.Height(20));
                if (newColor != pickerColor)
                {
                    pickerColor = newColor;
                    string hex = "#" + ColorUtility.ToHtmlStringRGB(newColor);
                    var (c2, t2) = GetTask();
                    if (t2 != null)
                    {
                        var mode = t2.colorMode;
                        if (mode == CardColorMode.None)
                            mode = CardColorMode.Stripe;
                        UndoService.RecordState("Set Card Color");
                        BoardFlowDataService.SetTaskColor(m_BoardId, c2.id, m_TaskId, hex, mode);
                        m_OnChanged?.Invoke();
                        BuildContent();
                    }
                }
            });
            colorPicker.AddToClassList("card-detail-color-field");
            colorRow.Add(colorPicker);

            // Mode dropdown
            var modeField = new EnumField(task.colorMode);
            modeField.AddToClassList("card-detail-color-mode");
            modeField.RegisterValueChangedCallback(evt =>
            {
                var (c2, t2) = GetTask();
                if (t2 == null) return;
                var newMode = (CardColorMode)evt.newValue;
                if (newMode != t2.colorMode)
                {
                    var colorVal = t2.color;
                    if (string.IsNullOrEmpty(colorVal) && newMode != CardColorMode.None)
                        colorVal = "#3498db";
                    UndoService.RecordState("Set Card Color Mode");
                    BoardFlowDataService.SetTaskColor(m_BoardId, c2.id, m_TaskId, colorVal, newMode);
                    m_OnChanged?.Invoke();
                    BuildContent();
                }
            });
            colorRow.Add(modeField);

            // Clear button
            var clearColorBtn = new Button(() =>
            {
                var (c2, t2) = GetTask();
                if (t2 == null) return;
                UndoService.RecordState("Clear Card Color");
                BoardFlowDataService.SetTaskColor(m_BoardId, c2.id, m_TaskId, string.Empty, CardColorMode.None);
                m_OnChanged?.Invoke();
                BuildContent();
            });
            clearColorBtn.text = "Clear Color";
            clearColorBtn.AddToClassList("card-detail-color-clear");
            colorSection.Add(clearColorBtn);

            // --- Labels ---
            var labelsSection = new VisualElement();
            labelsSection.AddToClassList("card-detail-section");
            root.Add(labelsSection);

            var labelsHeader = new Label("LABELS");
            labelsHeader.AddToClassList("card-detail-section-header");
            labelsSection.Add(labelsHeader);

            m_LabelsContainer = new VisualElement();
            m_LabelsContainer.AddToClassList("card-detail-labels-container");
            labelsSection.Add(m_LabelsContainer);

            if (board.labels.Count == 0)
            {
                var noLabels = new Label("No labels defined. Use the Labels button in the toolbar.");
                noLabels.style.fontSize = 11;
                noLabels.style.color = new StyleColor(new Color(0.62f, 0.62f, 0.62f));
                m_LabelsContainer.Add(noLabels);
            }
            else
            {
                for (int i = 0; i < board.labels.Count; i++)
                {
                    var labelData = board.labels[i];
                    bool isActive = task.labelIds.Contains(labelData.id);
                    var chip = new LabelChipElement(labelData, isActive);
                    chip.AddToClassList("card-detail-label-toggle");

                    var capturedId = labelData.id;
                    var capturedActive = isActive;
                    chip.RegisterCallback<ClickEvent>(evt =>
                    {
                        var (c, t) = GetTask();
                        if (t == null) return;

                        if (t.labelIds.Contains(capturedId))
                        {
                            UndoService.RecordState("Remove Label");
                            BoardFlowDataService.RemoveLabelFromTask(m_BoardId, c.id, m_TaskId, capturedId);
                        }
                        else
                        {
                            UndoService.RecordState("Add Label");
                            BoardFlowDataService.AddLabelToTask(m_BoardId, c.id, m_TaskId, capturedId);
                        }
                        m_OnChanged?.Invoke();
                        BuildContent();
                    });
                    m_LabelsContainer.Add(chip);
                }
            }

            // --- Description ---
            var descSection = new VisualElement();
            descSection.AddToClassList("card-detail-section");
            root.Add(descSection);

            var descHeader = new Label("DESCRIPTION");
            descHeader.AddToClassList("card-detail-section-header");
            descSection.Add(descHeader);

            m_DescriptionField = new TextField();
            m_DescriptionField.AddToClassList("card-detail-description");
            m_DescriptionField.multiline = true;
            m_DescriptionField.value = task.description ?? string.Empty;
            m_DescriptionField.RegisterCallback<FocusOutEvent>(evt =>
            {
                var (c, t) = GetTask();
                if (t == null) return;
                var newDesc = m_DescriptionField.value;
                if (newDesc != t.description)
                {
                    UndoService.RecordState("Edit Description");
                    BoardFlowDataService.UpdateTaskDescription(m_BoardId, c.id, m_TaskId, newDesc);
                    m_OnChanged?.Invoke();
                }
            });
            descSection.Add(m_DescriptionField);

            // --- Checklist ---
            var checkSection = new VisualElement();
            checkSection.AddToClassList("card-detail-section");
            root.Add(checkSection);

            var checkHeader = new Label("CHECKLIST");
            checkHeader.AddToClassList("card-detail-section-header");
            checkSection.Add(checkHeader);

            m_ChecklistContainer = new VisualElement();
            checkSection.Add(m_ChecklistContainer);

            for (int i = 0; i < task.checklist.Count; i++)
            {
                var item = task.checklist[i];
                var capturedItemId = item.id;

                var row = new VisualElement();
                row.AddToClassList("card-detail-checklist-item");

                var toggle = new Toggle();
                toggle.AddToClassList("card-detail-checklist-toggle");
                toggle.value = item.isCompleted;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    var (c, t) = GetTask();
                    if (t == null) return;
                    UndoService.RecordState("Toggle Checklist");
                    BoardFlowDataService.ToggleChecklistItem(m_BoardId, c.id, m_TaskId, capturedItemId);
                    m_OnChanged?.Invoke();
                });
                row.Add(toggle);

                var textField = new TextField();
                textField.AddToClassList("card-detail-checklist-text");
                textField.value = item.text;
                textField.isDelayed = true;
                textField.RegisterValueChangedCallback(evt =>
                {
                    var (c, t) = GetTask();
                    if (t == null) return;
                    if (!string.IsNullOrWhiteSpace(evt.newValue) && evt.newValue != item.text)
                    {
                        UndoService.RecordState("Edit Checklist Item");
                        BoardFlowDataService.UpdateChecklistItemText(m_BoardId, c.id, m_TaskId, capturedItemId, evt.newValue);
                        m_OnChanged?.Invoke();
                    }
                });
                row.Add(textField);

                var deleteBtn = new Button(() =>
                {
                    var (c, t) = GetTask();
                    if (t == null) return;
                    UndoService.RecordState("Delete Checklist Item");
                    BoardFlowDataService.RemoveChecklistItem(m_BoardId, c.id, m_TaskId, capturedItemId);
                    m_OnChanged?.Invoke();
                    BuildContent();
                });
                deleteBtn.text = "X";
                deleteBtn.AddToClassList("card-detail-checklist-delete");
                row.Add(deleteBtn);

                m_ChecklistContainer.Add(row);
            }

            var addItemBtn = new Button(() =>
            {
                var (c, t) = GetTask();
                if (t == null) return;
                UndoService.RecordState("Add Checklist Item");
                BoardFlowDataService.AddChecklistItem(m_BoardId, c.id, m_TaskId, "New item");
                m_OnChanged?.Invoke();
                BuildContent();
            });
            addItemBtn.text = "+ Add Item";
            addItemBtn.AddToClassList("card-detail-add-item");
            checkSection.Add(addItemBtn);
        }

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
