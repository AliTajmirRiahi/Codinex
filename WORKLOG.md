# WORKLOG

## Purpose
This file records implementation progress, decisions made during active work, and the next concrete step.

## Entry Format
Use this format for every meaningful change:

### YYYY-MM-DD - Task Title
- Goal:
- Files touched:
- Summary of changes:
- Reason for the change:
- Result:
- Next step:

## Current Entries

### 2026-06-24 - Initial AI Context Files Added
- Goal: Add persistent project context files so AI sessions do not rely on chat memory alone.
- Files touched: `PROJECT_CONTEXT.md`, `WORKLOG.md`, `AI_HANDOFF.md`
- Summary of changes: Added a high-level project context file, a running worklog template, and a handoff template for interrupted or long-running development sessions.
- Reason for the change: Codify is a multi-file Visual Studio extension with provider abstractions, storage, UI, and execution pipeline layers. This kind of project needs a durable source of truth outside chat history.
- Result: Future AI sessions can be restarted with consistent project context and less risk of losing architectural decisions.
- Next step: Inspect and document the exact startup flow and end-to-end chat execution path from package initialization to provider response.


## 2026-06-24 - Contenteditable Composer Implementation

### Completed
- Replaced the textarea-based composer input with a `contenteditable` editor surface.
- Updated composer DOM bindings to read and write from the editable element instead of textarea-specific APIs.
- Added inline chip/token rendering support for `/`, `@`, and `#` composer selections.
- Connected composer input changes to centralized `AppState` setters.
- Updated command menu interaction so `Enter` confirms the selected menu item while the menu is open.
- Added keyboard handling for `Escape`, `ArrowUp`, and `ArrowDown` in the composer command flow.
- Kept slash command, agent, and reference data mocked inside `composerController.js` for now.

### Files Touched
- `Codify/UI/ToolWindows/Resources/Chat/view/chat-view.html`
- `Codify/UI/ToolWindows/Resources/Chat/js/views/composerView.js`
- `Codify/UI/ToolWindows/Resources/Chat/js/controllers/composerController.js`
- `Codify/UI/ToolWindows/Resources/Chat/js/state/appState.js`
- `Codify/UI/ToolWindows/Resources/Chat/js/views/chatView.js`
- `Codify/UI/ToolWindows/Resources/Chat/css/components/_inputs.css`

### Notes
- Composer text changes now update centralized state through the new composer setters.
- Inline chips are rendered inside the editable composer instead of being shown as detached tags above the input.
- The current command, agent, and reference sources are mock data and should later move behind a provider/context service.



### 2026-06-24 - Backspace Chip Removal Implementation

### Completed
- **Goal**: Allow users to remove chips (agents, commands, references) using the Backspace key in the composer.
- **Files touched**: 
  - `Codify/UI/ToolWindows/Resources/Chat/js/views/composerView.js`
  - `Codify/UI/ToolWindows/Resources/Chat/js/controllers/composerController.js`
- **Summary of changes**: 
  - Added `handleBackspace` to `composerView` to intercept keydown events.
  - Implemented logic to detect if the caret is preceded by a `.composer-chip` or trailing whitespace.
  - Wired `composer:chip-remove` event to `composerController` to ensure `AppState` is updated when a chip is deleted via keyboard.
- **Reason for the change**: Improving UX consistency, making the AI-powered editor feel more native (similar to GitHub Copilot).
- **Result**: Users can now fluidly type and delete context items using only the keyboard.


### 2026-06-24 - Composer Trigger Behavior Fixes
- **Goal**: Fix bugs related to `@`, `/`, and `#` triggers in `contenteditable`.
- **Status**: Completed
- **Summary of changes**: 
  - Implemented `removeTriggerAtCursor` to prevent double trigger characters.
  - Added trailing-space logic to ensure cursor doesn't get stuck inside chip spans.
- **Result**: Triggers now clean up correctly upon selection and cursor positioning is fluid.

### 2026-06-25 - Implement menu for # trigger
- **Goal**: Show a context menu of available file/folder references when `#` is typed.
- **Status**: Completed
- **Files touched**: 
  - `Codify/UI/ToolWindows/Resources/Chat/js/views/composerView.js`
  - `Codify/UI/ToolWindows/Resources/Chat/js/controllers/composerController.js`
- **Summary of changes**: 
  - Added `#` to the active trigger detection in `composerView`.
  - Updated `composerController` to fetch and filter workspace references when `#` is active.
- **Reason for the change**: Enable users to easily attach code context to their prompts using the `#` symbol.
- **Result**: Typing `#` now opens a searchable list of project files.
- **Next step**: Implement multi-provider support for the chat backend.

### 2026-06-26 - Add reference menu and implement reference bar
- **Status**: Completed
- Goal: Add a dedicated reference menu UI for the `#` trigger and implement selected reference chips as a reference bar inside the composer flow.
- Files touched:
  - `Codify/UI/ToolWindows/Resources/Chat/js/views/composerView.js`
  - `Codify/UI/ToolWindows/Resources/Chat/js/controllers/composerController.js`
  - `Codify/UI/ToolWindows/Resources/Chat/js/state/appState.js`
  - `Codify/UI/ToolWindows/Resources/Chat/css/components/_inputs.css`
  - `WORKLOG.md`
- Summary of changes:
  - Added a dedicated reference menu surface positioned near the caret for `#` trigger interactions.
  - Implemented grouped rendering for available references in the composer menu.
  - Added centralized `selectedReferences` state for chosen project context items.
  - Implemented reference chip rendering so selected references appear as a reference bar in the composer experience.
  - Kept chip add/remove behavior synchronized with composer keyboard interactions and state updates.
- Reason for the change: The composer needs a complete project-context attachment flow so users can discover references with `#` and see active context selections clearly before sending a prompt.
- Result: The `#` flow now supports menu-based discovery and visible selected reference chips in the composer UI.
- Next step: Connect the reference menu to backend-provided references through the bridge instead of relying on mocked data.

## 2026-07-05 – Added MethodReferenceProvider
- **Status**: Completed
### Summary
Implemented new provider: `MethodReferenceProvider`, which extracts method symbols from the current solution using Roslyn and exposes them as `ReferenceItem` objects.

### Files modified
- MethodReferenceProvider.cs
- ServiceContainer.cs

### Result
Codify can now show methods as selectable references inside Composer (e.g., #method). Fully integrated into DI.

## 2026-07-06 — Decoupled Message Building from AI Providers
- **Status**: Completed
### Summary
    Created ChatMessageBuilder to handle the assembly of system prompts, agent instructions, and user context.
    Introduced IReferenceContextFormatter to standardize how different ReferenceKind types (Files, Methods, Classes, etc.) are presented to the AI.
    Refactored GapGptProvider to focus solely on serialization and transport, removing any prompt injection logic.
    Updated SendChatMessageUseCase to use the new builder pipeline before dispatching requests to providers.
##Result:
 The system is now provider-agnostic, allowing local or cloud AI agents to receive structured, formatted context regardless of the underlying API.