---
title: ROADMAP
created: 2025-06-07
updated: 2025-06-07
version: 1.0.0
---

## Minimal Shell + File-Backed Notes

> Get a working, cross-platform "empty" app in users’ hands.
> Focus on core Elmish structure, folder layout, and basic read-only notes.

### Features

1. App Shell & Layout
    - [x] Single-window frame with a left sidebar and main content area.
    - [x] Sidebar shows static items: Inbox, Journals, Projects
    - [ ] Top menu (File, Edit, View) stubbed out but non-functional.
2. Elmish Loop
    - [x] Implement (Model, Msg, Update, View) boilerplate.
    - [ ] Render a placeholder welcome screen
        - "Welcome to the Stormlight Labs Note Taker! Click Inbox to get started."
3. Filesystem Scaffolding
    - [x] On first run, create a `~/.config/note_taker` folder (or `%AppData%/Lazo` on Windows)
      with subfolders:

       ```text
        ~/.config/
         └─ note_taker/
            ├─ Inbox/
            ├─ Journals/
            └─ Projects/
       ```
    - [x] Store a `config.json` (theme, recent-files list). If missing, write defaults.
4. Read-Only Markdown Preview
    - [ ] When the user clicks Inbox, list files in `Inbox/` (only filenames).
    - [ ] Clicking a filename loads it into a Markdown renderer pane (read-only).
5. Basic Theming
    - [x] Light vs. Dark toggle (reads/writes `config.json`).
    - [x] Persist theme choice on exit.

### Tests & Validation

- Config Load/Save Tests
    - [x] Unit tests for deserializing empty/malformed `config.json`.
    - [x] Unit tests for saving theme choice.
- File-IO Tests
    - [x] Integration tests for folder creation and sample Markdown preview.

## Capture & Quick-Entry (Alpha 0.1)

> Let users create new notes instantly with minimal friction.

### Features

1. New Note Flow
    - [ ] Keyboard shortcut (`Ctrl+N`/`⌘+N`) opens overlay: title field + folder dropdown.
    - [ ] Save writes `Title.md` into chosen folder and opens it in the editor.
2. Basic Markdown Editor
    - [ ] Text area with live-preview toggle.
    - [ ] Autosave after 300 ms of inactivity.
    - [ ] Minimal toolbar: Bold, Italic, Link.
3. Daily Journal Shortcut
    - [ ] `Ctrl+J`/`⌘+J` creates/opens `Journals/YYYY-MM-DD.md` with a date header.
4. Undo/Redo
    - [ ] In-editor undo/redo stack.
    - [ ] Shortcuts: `Ctrl+Z`/`⌘+Z`, `Ctrl+Y`/`⌘+Shift+Z`.
5. Font Settings
    - [ ] Editor font-size slider (8–24 pt).
    - [ ] Persist in `config.json`.

### Tests & Validation

- Note Creation Tests
    - [ ] Unit: writing a new note file.
    - [ ] Unit: sanitizing invalid title characters.
- Autosave Tests
    - [ ] Integration: idle typing triggers disk write.
    - [ ] Unit: undo/redo behavior.
- Journal Tests
    - [ ] Unit: same-day journal file reuse.
    - [ ] Unit: date header formatting.

### Release

- `dotnet publish`
- Users can capture notes/journals, edit with autosave, and adjust basic settings.
- Show a “Note created” toast on save.

## Core Workflow (Alpha 0.2)

> Allow users to move notes through a simple GTD funnel: Inbox → Next Actions → Projects → Archive.

### Features

1. Status & Actions
    - [ ] Status badge (Inbox, Next, Project, Archived) on each note.
    - [ ] "Change Status" dropdown moves the `.md` file to the appropriate folder.
    - [ ] Project status prompts for project name and creates a subfolder under `Projects/`.

2. Project Overview Panel
    - [ ] Clicking Projects shows a list of project names.
    - [ ] Two-pane view: list of project notes (left) and selected note (right).

3. Triage View
    - [ ] Inbox view lists notes sorted by creation date.
    - [ ] Quick buttons: “→ Next Action,” “→ Project…,” “Archive.”

4. Archive View
    - [ ] Read-only previews of archived notes.
5. Keyboard Navigation
    - [ ] Arrow keys to navigate lists, Enter to open notes, Esc to close dialogs.

### Tests & Validation

- Status Transition Tests
    - [ ] Unit: file moves and model updates.
    - [ ] Integration: project creation with duplicate names.

- Folder Structure Tests
    - [ ] Integration: on-demand creation of NextActions, Projects, Archive folders.

- Keyboard Flow Tests
    - [ ] Unit: simulate navigation and status changes.

### Release

- Triage, next-action assignment, project grouping, and archiving.
- Gather feedback on workflow intuitiveness.

## Metadata, Search, & Tags (Alpha 0.3)

> Add lightweight structure for quick retrieval without a heavyweight database.

### Features

1. Tagging Convention
    - [ ] Extract `#tag` tokens from Markdown.
    - [ ] Clickable tags filter notes across all folders.
2. Full-Text Search
    - [ ] Build a simple index (SQLite or LiteDB.FSharp).
    - [ ] Search bar filters results live (title + snippet).
3. Advanced Filters
    - [ ] Sidebar “Filters”: Unassigned, Due Today (front-matter `due:`), Starred.
    - [ ] Store metadata in front-matter (YAML or JSON).
4. Metadata Panel
    - [ ] "⋮" menu allows editing metadata: title, tags, due date (calendar picker), star toggle.
    - [ ] Auto-update front-matter.
5. Project Rename
    - [ ] Renaming a project folder on disk, with conflict confirmation.

### Tests & Validation

- Tag Extraction Tests
    - [ ] Unit: parse tags from sample Markdown.
- Search Tests
    - [ ] Integration: test corpus search accuracy and snippet highlighting.
- Filter Tests
    - [ ] Unit: due-date parsing, star toggling.
- Project Rename Tests
    - [ ] Integration: handling name conflicts gracefully.

### Release

- Tagging, search, and filters.
- Get feedback on search speed and tag UX.

---

## Customization & Power-User Tools (Beta 0.4)

> Expose deeper settings, snapshot history, and plugin foundations.

### Features

1. Settings Window
    * Theme (Light, Dark, System), editor font face/size, keyboard shortcuts mapping, default
      folders, backup location.
2. Undo Across Sessions
    * Save previous versions in `History/<note-id>/timestamp.md`.
    * UI to restore snapshots.
3. Plugin/Script Hooks
    * Plugin manifest schema in `~/.lazo/plugins/`.
    * “Run Plugin Script” menu option, spawning external executables with note path.
4. Saved Searches (Smart Folders)
    * Define saved queries that appear in sidebar.
    * Simple query builder UI.
5. Bulk Actions
    * Multi-select notes for bulk status changes or tag edits.

### Tests & Validation

- Settings Persistence Tests
    - [ ] Unit: verify config updates.
- History Tests
    - [ ] Integration: multiple snapshots; correct restore.
- Plugin Tests
    - [ ] Unit: spawn dummy script with correct args.
- Smart Folder Tests
    - [ ] Integration: saved queries shown and run correctly.

### Release

- Turns app into a customizable workspace with session history and plugin foundations.
- Encourage developers to build a sample plugin.

---

## Phase 6: Cloud Sync & Mobile Companion (Beta 0.5+)

> Provide cross-device access and sync, while keeping desktop app offline-first.

### Must-Have Features

1. **Git-Based Sync**

    * Treat `~/.lazo` as a Git repo; auto-commit on save; “Sync Now” for push/pull.

2. **Dropbox/Nextcloud Sync (Optional)**

    * REST-based upload/download, conflict prompts.

3. **Mobile Companion Stub**

    * Read-only Android/iOS preview fetching notes via Git or REST.

4. **Conflict Resolution UI**

    * Side-by-side diff with “Keep Local,” “Keep Remote,” or “Merge.”

5. **Encrypted Backups**

    * Export `~/.lazo` as AES-256 encrypted zip.

### Tests & Validation

* **Git Sync Tests**

    * Integration: init, commit, pull dry-run.
* **Dropbox API Tests**

    * Unit: stub server simulation.
* **Mobile Companion Tests**

    * Integration: demo APK/IPA showing note list.
* **Conflict Tests**

    * Integration: simulated conflicting edits.

### Release Hook

* Beta 0.6 offers desktop + mobile preview with Git-sync.
* Gather feedback on offline reliability and conflict UX.

---

## Phase 7: Polish & 1.0 Stable (Release 1.0)

> Harden for production use with polish, performance tuning, installers, and documentation.

### Must-Have Features

1. **UI/UX Polish**

    * Refined typography, padding, icons, animations, accessibility (high-contrast mode,
      screen-reader labels).

2. **Performance Tuning**

    * Lazy-load notebooks, debounce heavy tasks, memory profiling.

3. **Installers & Auto-Update**

    * Windows MSI, macOS DMG/Homebrew, Linux AppImage/Deb/RPM; auto-update via Squirrel/Sparkle.

4. **Plugin API & Marketplace**

    * Document plugin schema; built-in Plugin Manager for install/uninstall.

5. **Complete Documentation**

    * User manual (installation, workflows, shortcuts, plugin guide).
    * Developer docs (architecture, folder layout, metadata spec, plugin interface).

6. **Internationalization**

    * Resource files for localization; at least one non-English locale.

### Tests & Validation

* **End-to-End QA** on all platforms.
* **Installer Testing** in clean VMs.
* **Plugin Lifecycle Tests** through Plugin Manager.

### Release Hook

* 1.0 Stable with installers, auto-update, full docs, and a plugin marketplace.
* Publish release notes highlighting ADHD-friendly design and accessibility.

---

### Accessibility Considerations

- Minimal onboarding flow.
- Keyboard-first interactions.
- Clear visual cues with minimal clutter.
- Progressive disclosure of advanced features behind an “Expert Mode” toggle.
- Instant feedback via toasts/snackbars.
