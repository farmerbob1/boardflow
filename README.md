# BoardFlow

A Kanban-style project task tracker built directly into the Unity Editor. Keep your task management inside the editor instead of switching to external tools.

![Unity 6+](https://img.shields.io/badge/Unity-6000.0%2B-blue) ![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Multiple Boards** - Create and switch between separate boards for different workflows
- **Customizable Columns** - Add, rename, reorder, and delete columns to match your process
- **Task Cards** - Create tasks with editable titles, priority levels, and checklists
- **Drag & Drop** - Reorder cards within and across columns, and reorder columns themselves
- **Priority Levels** - None, Low, Medium, High, and Critical with colored indicator bars
- **Checklists** - Add checklist items to tasks with toggleable checkboxes and progress bars
- **Inline Editing** - Double-click any title or checklist item to edit in place
- **Search & Filter** - Filter cards in real-time across all columns
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

- **Double-click** a column title, task title, or checklist item to edit it inline
- **Right-click** a column header for rename/delete options
- **Right-click** a task card to set priority, edit title, manage checklist items, or delete

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

## Requirements

- Unity 6 (6000.0) or later
