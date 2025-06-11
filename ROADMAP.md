---
title: ROADMAP
created: 2025-06-07
updated: 2025-06-11
version: 2.0.0
---

## Overview

This document has a phased plan to build a desktop writing app. Each 
phase produces a standalone binary for feedback and iteration.

### Tech Stack

- Functional `.NET`: F#, Avalonia with FuncUI, and Elmish
    - Pages are stored as markdown files on the filesystem
    - SQLite is used solely for full-text search and indexing. 

## Basic Editing (Alpha 0.1)

1. File-based Model
    - Represent each page as a markdown file under `data/pages/` (e.g. `MyPage.md`)
    - Load and parse markdown files into an in-memory model on startup
    - FileSystemWatcher to detect external edits and reload pages
3. Editor UI
    - Text editor for raw markdown using Avalonia.FuncUI controls
    - On-edit autosave writes changes back to the markdown file

## Markdown Preview, Hierarchy, and History Stack (Alpha 0.2)

1. Markdown Preview
    - Render markdown in a side-by-side or toggle-preview pane
    - Support headings, lists, bold, italics, links
2. Block Hierarchy
    - Infer nested lists and headings from markdown structure
    - UI controls for indent/outdent and drag-reorder update file content
3. Undo/Redo
    - In-memory command stack capturing text edits and file writes
    - Persist history snapshots in memory during session

## Indexing and Search (Alpha 0.3)

1. Indexing Service
    - On startup and on file change, index markdown files into SQLite FTS table
    - Store file path, title (first heading), and content in index
2. Search UI
    - Search bar querying SQLite FTS for instant filtering of pages
    - Show results with snippet previews and open-on-click

## Navigation and Themes (Beta 0.1)

1. Sidebar
    - List markdown files alphabetically or by folder
    - Create, rename, delete markdown files from UI
2. Themes
    - Light and dark UI themes
    - Option to load custom CSS for preview pane from `assets/themes/`

## Productivity (Beta 0.2)

1. Inline Tasks
    - Recognize task syntax (`- [ ]`, `- [x]`) in markdown
    - Task pane filtering by status and due date metadata (YAML front matter)
2. Daily Notes
    - Automatically open or create `data/daily/YYYY-MM-DD.md` on startup
    - Calendar control to navigate and open daily notes

## Synchronization Engine (Beta 0.3)

1. Change Events
    - Track file changes and local edits in a change queue
2. Remote Sync Interface
    - REST API client module for push/pull of markdown files
    - Merge remote changes by overwriting or three-way merge on markdown text
3. Status Indicator

## Collections, Workflows, and AI (Beta 0.4)

1. Collections
    - Query pages by tags or front-matter fields using SQLite index
    - Display query results as tables or card galleries
2. Workflow Templates
    - Predefined folder structures and file templates (meeting notes, outlines)
    - UI to apply a workflow by copying templates into `data/pages/`
3. API Key Management
    - Settings page to store user-provided API keys
    - Hooks for external services (translation, summarization) invoked with file content

## Export and Local Preview Server (Version 1.0.0 & Beyond)

1. Export Formats
    - Export a markdown page or selection to HTML or PDF
2. Local HTTP Server
    - Lightweight server serving rendered markdown for preview or sharing on LAN

### Getting Things Done Workflow

1. Implementation
    - Inbox page and global 'Inbox' capture folder for quick note dumping
    - Next Actions view filtered by next-action: true metadata
    - Project pages grouping related tasks under a common project tag
    - Waiting For list showing items tagged waiting-for
    - Someday/Maybe section for ideas tagged someday
    - Reference library folder for non-actionable notes
2. UI
    - UI to move items between lists and update metadata front matter

## Testing and Automation

1. Unit Tests with Expecto for parsing and model logic
2. File-based integration tests using temporary directories
3. UI automation tests via Avalonia automation API
4. CI pipelines for cross-platform builds and packaging

## Distribution

1. Windows: MSIX or Squirrel
2. macOS: DMG with notarization
3. Linux: AppImage or distribution packages
4. Auto-update mechanism querying a release feed