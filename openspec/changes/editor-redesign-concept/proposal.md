# Proposal

## Why

Greenshot's image editor combines a mature set of user-facing features with a UI and drawing surface built around WinForms and GDI+ types. A replacement must preserve this capability and integration surface while enabling a more modern editing experience, so the current behavior and architectural constraints need to be understood before the work is divided into implementation changes.

## What Changes

- Record the current editor feature set, configuration, and integration boundaries as the source inventory for a future feature-parity checklist.
- Recommend WPF as the working direction for the replacement UI because Greenshot already uses WPF, while explicitly deferring the production rendering technology and the amount of reusable editor logic until validated.
- Define an interaction concept with two globally configured modes: the existing select-tool-then-drag workflow, and a click-to-add workflow for every drawing tool except freehand. In click-to-add mode, an item is inserted with tool-appropriate default dimensions and remains selected for adjustment; validate the details in the mock-up.
- Keep the interactive, isolated WPF mock-up out of this concept change. Plan it as a separate change after this concept is reviewed; use it to validate a right-side, labeled properties panel and the drawing interactions before defining final production requirements.
- Use the reviewed concept and mock-up findings to define durable editor requirements and break production work into smaller changes.

## Capabilities

This change delivers an analysis and migration concept, not a change to production editor behavior. It intentionally has no delta specifications; final capability boundaries and requirements follow the mock-up.

### New Capabilities

None in this concept change.

### Modified Capabilities

None in this concept change.

## Impact

- Current editor UI and drawing implementation in `src/Greenshot.Editor/`, including `ImageEditorForm`, `Surface`, drawable containers, fields, filters, and editor configuration.
- Editor-facing interfaces and plugin entry points in `src/Greenshot.Base/Interfaces/` and editor plugins, which currently expose WinForms and drawing-related types.
- Existing WPF infrastructure in `src/Greenshot.Base/Wpf/` and `src/Greenshot/Forms/Wpf/`, which informs the working UI direction and future prototype.
- Windows and .NET Framework 4.8 compatibility remain constraints. This change adds no production code and does not decide final editor behavior, renderer, integration strategy, or migration mechanics.
