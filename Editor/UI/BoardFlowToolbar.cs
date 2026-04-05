using System;
using System.Collections.Generic;
using BoardFlow.Editor.Data;
using UnityEngine.UIElements;

namespace BoardFlow.Editor.UI
{
    public class BoardFlowToolbar : VisualElement
    {
        readonly DropdownField m_BoardDropdown;
        readonly Button m_NewBoardButton;
        readonly Button m_AddColumnButton;
        readonly Button m_GridSettingsButton;
        readonly Button m_LabelsButton;
        readonly TextField m_SearchField;

        public event Action<string> OnBoardSelected;
        public event Action OnNewBoardClicked;
        public event Action OnRenameBoardClicked;
        public event Action OnDeleteBoardClicked;
        public event Action OnAddColumnClicked;
        public event Action OnLabelsClicked;
        public event Action OnFieldsClicked;
        public event Action OnStatsClicked;
        public event Action OnGridSettingsClicked;
        public event Action<string> OnSearchChanged;

        public BoardFlowToolbar()
        {
            AddToClassList("toolbar");

            // Left section
            var leftSection = new VisualElement();
            leftSection.AddToClassList("toolbar-left");
            Add(leftSection);

            m_BoardDropdown = new DropdownField();
            m_BoardDropdown.AddToClassList("toolbar-board-dropdown");
            m_BoardDropdown.RegisterValueChangedCallback(evt =>
            {
                OnBoardSelected?.Invoke(evt.newValue);
            });
            leftSection.Add(m_BoardDropdown);

            m_NewBoardButton = new Button(() => OnNewBoardClicked?.Invoke());
            m_NewBoardButton.text = "+";
            m_NewBoardButton.tooltip = "New Board";
            m_NewBoardButton.AddToClassList("toolbar-icon-button");
            leftSection.Add(m_NewBoardButton);

            var renameBoardButton = new Button(() => OnRenameBoardClicked?.Invoke());
            renameBoardButton.text = "Rename";
            renameBoardButton.tooltip = "Rename Board";
            renameBoardButton.AddToClassList("toolbar-button");
            leftSection.Add(renameBoardButton);

            var deleteBoardButton = new Button(() => OnDeleteBoardClicked?.Invoke());
            deleteBoardButton.text = "Delete";
            deleteBoardButton.tooltip = "Delete Board";
            deleteBoardButton.AddToClassList("toolbar-button");
            deleteBoardButton.AddToClassList("toolbar-button--danger");
            leftSection.Add(deleteBoardButton);

            m_AddColumnButton = new Button(() => OnAddColumnClicked?.Invoke());
            m_AddColumnButton.text = "+ Column";
            m_AddColumnButton.tooltip = "Add Column";
            m_AddColumnButton.AddToClassList("toolbar-button");
            leftSection.Add(m_AddColumnButton);

            m_LabelsButton = new Button(() => OnLabelsClicked?.Invoke());
            m_LabelsButton.text = "Labels";
            m_LabelsButton.tooltip = "Manage Labels";
            m_LabelsButton.AddToClassList("toolbar-button");
            leftSection.Add(m_LabelsButton);

            var fieldsButton = new Button(() => OnFieldsClicked?.Invoke());
            fieldsButton.text = "Fields";
            fieldsButton.tooltip = "Manage Custom Fields";
            fieldsButton.AddToClassList("toolbar-button");
            leftSection.Add(fieldsButton);

            // Right section
            var rightSection = new VisualElement();
            rightSection.AddToClassList("toolbar-right");
            Add(rightSection);

            m_SearchField = new TextField();
            m_SearchField.AddToClassList("toolbar-search");
            m_SearchField.value = string.Empty;
            var placeholder = m_SearchField.Q<TextElement>();
            if (placeholder != null)
                m_SearchField.Q<TextElement>().text = "";
            m_SearchField.RegisterValueChangedCallback(evt => OnSearchChanged?.Invoke(evt.newValue));
            // Add a placeholder via a label
            var searchLabel = new Label("Search...");
            searchLabel.AddToClassList("toolbar-search-placeholder");
            searchLabel.pickingMode = PickingMode.Ignore;
            m_SearchField.Add(searchLabel);
            m_SearchField.RegisterCallback<FocusInEvent>(evt =>
            {
                searchLabel.style.display = DisplayStyle.None;
            });
            m_SearchField.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (string.IsNullOrEmpty(m_SearchField.value))
                    searchLabel.style.display = DisplayStyle.Flex;
            });
            rightSection.Add(m_SearchField);

            var statsButton = new Button(() => OnStatsClicked?.Invoke());
            statsButton.text = "Stats";
            statsButton.tooltip = "Board Statistics";
            statsButton.AddToClassList("toolbar-button");
            rightSection.Add(statsButton);

            m_GridSettingsButton = new Button(() => OnGridSettingsClicked?.Invoke());
            m_GridSettingsButton.text = "Settings";
            m_GridSettingsButton.tooltip = "Grid Settings";
            m_GridSettingsButton.AddToClassList("toolbar-button");
            rightSection.Add(m_GridSettingsButton);
        }

        public void SetBoards(List<BoardData> boards, string activeBoardId)
        {
            var choices = new List<string>();
            string activeChoice = null;

            for (int i = 0; i < boards.Count; i++)
            {
                choices.Add(boards[i].name);
                if (boards[i].id == activeBoardId)
                    activeChoice = boards[i].name;
            }

            m_BoardDropdown.choices = choices;
            if (activeChoice != null)
                m_BoardDropdown.SetValueWithoutNotify(activeChoice);
        }

        public void ClearSearch()
        {
            m_SearchField.SetValueWithoutNotify(string.Empty);
            var placeholder = m_SearchField.Q<Label>(className: "toolbar-search-placeholder");
            if (placeholder != null)
                placeholder.style.display = DisplayStyle.Flex;
        }
    }
}
