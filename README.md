# BoardFlow

A Kanban-style project task tracker built directly into the Unity Editor. Keep your task management inside the editor instead of switching to external tools.

![Unity 6+](https://img.shields.io/badge/Unity-6000.0%2B-blue) ![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Multiple Boards** - Create and switch between separate boards for different workflows
- **Customizable Columns** - Add, rename, reorder, and delete columns to match your process
- **Task Cards** - Create tasks with editable titles, descriptions, priority levels, and checklists
- **Labels/Tags** - Color-coded board-level labels for categorizing tasks, assignable via context menu or detail view
- **Card Detail View** - Click a card to open a full editing window with title, priority, labels, description, and checklist
- **Drag & Drop** - Reorder cards within and across columns, and reorder columns themselves
- **Card Colors** - Per-card color with two display modes: colored stripe bar or translucent background tint
- **Priority Levels** - None, Low, Medium, High, and Critical with colored indicator bars
- **Checklists** - Add checklist items to tasks with toggleable checkboxes and progress bars
- **Inline Editing** - Double-click any title or checklist item to edit in place
- **Search & Filter** - Filter cards in real-time across all columns by title, description, or label name
- **Undo/Redo** - Full integration with Unity's Ctrl+Z / Ctrl+Y
- **Persistent Data** - Saved as JSON in `ProjectSettings/BoardFlow/`, version-control friendly
- **Dark Theme** - Styled to match the Unity editor skin

## Installation

### Git URL (recommended)

1. Open **Window > Package Manager** in Unity
2. Click **+** > **Add package from git URL...**
3. Enter: `https://github.com/farmerbob1/boardflow.git`

### Local

1. Clone or download this repository into your project's `Packages/` folder as `com.boardflow`

## Usage

Open the board from the menu: **Window > BoardFlow**

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New board |
| `Ctrl+F` | Focus search field |
| `Escape` | Clear search / unfocus |

### Editing

- **Single-click** a card to open the detail view for full editing (title, priority, labels, description, checklist)
- **Double-click** a column title, task title, or checklist item to edit it inline
- **Right-click** a column header for rename/delete options
- **Right-click** a task card to set priority, set card color, assign labels, edit title, manage checklist items, or delete
- Use the **Labels** toolbar button to create, edit, and delete board-level labels

### Drag & Drop

- Drag cards by the `⠇` handle to reorder within a column or move between columns
- Drag columns by the `☰` handle in the header to reorder them

## Package Structure

```
com.boardflow/
  package.json
  Editor/
    BoardFlowWindow.cs           # Main EditorWindow
    Data/                         # Serializable data model classes
    Services/                     # JSON persistence and undo system
    UI/                           # Visual element classes
    DragDrop/                     # Drag manipulators and state
    Styles/                       # USS stylesheets
```

## Data Storage

Board data is saved to `ProjectSettings/BoardFlow/boardflow-data.json` as pretty-printed JSON. This location is version-control friendly and shareable across a team without requiring Unity's AssetDatabase.

## Roadmap (v2)

### High Value
- [x] Task descriptions - Multi-line body/notes field beyond just the title
- [x] Labels/tags - Color-coded tags for categorizing tasks (e.g., "Bug", "Feature", "Art") with filtering
- [ ] Column WIP limits - Max task count per column with visual warning when exceeded
- [ ] Due dates - Date field on tasks with overdue highlighting
- [ ] Board templates - Preset column layouts (Scrum, Bug Triage, Art Pipeline, etc.) when creating a new board

### Quality of Life
- [x] Card expand/detail view - Click to open a larger popup for editing everything in one place
- [ ] Collapse columns - Minimize columns you don't need to see right now
- [ ] Virtual scrolling - Card count limits on scroll for boards with 100+ cards
- [ ] Import/export - CSV or Trello JSON import so teams can migrate existing boards
- [ ] Multiple selection - Shift/Ctrl+click to select multiple cards for bulk move or delete

### Visual Polish
- [x] Card color/cover - Background color or colored stripe per card
- [ ] Assignee avatars - Assign team members to tasks pulled from project contributors
- [ ] Swimlanes - Horizontal grouping rows within the board (by priority, assignee, etc.)
- [ ] Column sorting - Sort cards by priority, date created, or alphabetical

### Power User
- [ ] Board-level statistics - Small dashboard showing cards per column, completion rate, velocity
- [ ] Notification dot on menu item - Show when tasks are overdue
- [ ] Custom fields - User-defined key/value pairs on cards for project-specific data

## Requirements

- Unity 6 (6000.0) or later
