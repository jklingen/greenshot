# Design

## Context

See `proposal.md` for the motivation and scope. This design records the current feature and integration surface and recommends a direction for investigation; it is not a production implementation design.

The editor is concentrated in `Greenshot.Editor`, but its boundaries are not UI-neutral:

- `ImageEditorForm` owns the WinForms window, menus, toolbars, keyboard handling, status, export actions, and editor-plugin initialization.
- `Surface` is itself a WinForms `Control`. It handles mouse input, zoom and image-coordinate conversion, selection, drawing, and invalidation.
- Drawable containers and filters hold much of the editing behavior, but use `System.Drawing` types such as `Graphics`, `Bitmap`, `Matrix`, and `Image`.
- `ISurface` exposes WinForms types, including `Control.ControlCollection` and `ContextMenuStrip`, as well as drawing and clipboard operations. `IEditorPlugin` takes a WinForms `Form` and `ToolStripMenuItem`.
- Greenshot already has WPF windows, shared WPF theme and translation support, and WPF controls in its existing Windows desktop application. The solution targets .NET Framework 4.8 and is not cross-platform.

### Current capability inventory

The following inventory is derived from `ImageEditorForm`, its designer, `Surface`, drawable containers, and `ISurface`. It is the starting point for future parity analysis, not a claim that each item must be implemented in the mock-up.

| Area | Existing capabilities to preserve or explicitly disposition |
|---|---|
| Editor lifecycle and document | Open an editor for a capture or an empty image; multiple editor windows; optional editor reuse; editor sizing to the capture; persisted window placement; close prompts for unsaved edits. |
| Annotations and inserted objects | Select tool; rectangle, ellipse, line, arrow, freehand, text, speech bubble, numbered step label, emoji, and highlight/obfuscation regions. The surface also supports image, icon, cursor, SVG, metafile, and vector objects through drawing containers and/or insertion APIs. |
| Selection and object editing | Select one or multiple objects; move and resize through adorners; edit text; change z-order; duplicate, delete, cut, copy, and paste; select all; save and load reusable drawing elements. Context-menu operations include fitting or snapping objects to canvas edges and pushing objects out toward an edge. |
| Object properties | Properties are collected dynamically from selected objects or the active tool. Current fields include line/fill colors, line thickness, shadow, arrow heads, font family/size/bold/italic, text alignment, filter parameters, crop mode, and step-label counter. Last-used field values are persisted. |
| Filters and redaction | Blur, pixelization, brightness, grayscale, highlight modes (text, area, grayscale, magnification), obfuscation presets, and a text-obfuscation workflow with matching options. Some text-obfuscation functionality is restricted to beta testers. |
| Image and canvas operations | Crop modes (default, horizontal, vertical, auto); resize; canvas expansion/contraction; rotate clockwise/counter-clockwise; border; drop shadow; torn edges; grayscale; invert; remove transparency; clear image. |
| Navigation and history | Undo/redo with action labels; zoom presets, actual size, best fit, and mouse-wheel zoom; image dimensions and status information; DPI-aware editor and canvas adjustment. |
| Output and application integration | Save and export through available destinations; clipboard and print; file-path actions; capture details and export image access; Recipes and editor plugins. Export destinations are discovered dynamically. |
| Configuration, localization, and help | Editor-specific settings include capture sizing, window state, editor reuse, default empty-editor size, freehand sensitivity, close behavior, recent colors/emoji, and text-redaction options. UI strings use the repository's language infrastructure; help and about actions are available. |

This is a behavioral inventory, not a requirement that every legacy menu or implementation detail be carried forward unchanged. During production planning, each entry should be marked as preserve, intentionally change, replace, or retire, with user-visible impact recorded.

## Goals / Non-Goals

**Goals:**

- Establish the current feature and integration surface against which a future replacement can be assessed.
- Prefer WPF as the working direction for the replacement UI and use an isolated WPF mock-up to validate the user experience.
- Explore a right-side, labeled properties panel and the two globally configured drawing interactions described in the proposal.
- Keep the mock-up, final behavior contracts, and production migration in separately reviewable changes.

**Non-Goals:**

- Implement the mock-up, new editor, drawing engine, or any production feature in this concept change.
- Specify final feature-by-feature behavior, compatibility guarantees for editor plugins, serialization migration, renderer, or rollout strategy.
- Make the mock-up a feature-parity prototype or connect it to production captures, exports, or plugin services.

## Decisions

### Use WPF as the working direction, not as a finalized production commitment

WPF is already used in Greenshot and fits the Windows desktop and .NET Framework 4.8 constraints. It is the most direct candidate for a modern UI and can be evaluated with an isolated prototype.

Alternatives considered:

- Keep WinForms and modernize its controls: this would not meet the goal of moving the editor UI away from WinForms.
- Select a different UI framework now: no requirement or repository evidence currently justifies adding a new framework before a WPF experience has been tested.

The prototype validates the interaction and UI direction only. It does not settle the production canvas renderer or the degree to which current drawing logic can be reused.

### Treat UI replacement and editing-core migration as separate architectural concerns

Some existing drawing behavior may be reusable, but the current `Surface`, `ISurface`, and plugin contract directly depend on WinForms and GDI+/`System.Drawing`. A WPF window placed around the existing surface would be a useful throwaway experiment at most; it would not constitute a WinForms-independent editor.

The production architecture should be decided after the prototype and a focused technical investigation. It should distinguish the editor document and editing operations from UI-specific input, controls, rendering, and plugin integration. No compatibility adapter or rewrite boundary is selected by this concept.

Alternative considered: port the entire editor and all plugins in one step. This concentrates risk and makes feature parity hard to review; the production work should instead be split into small, independently reviewable changes after requirements are agreed.

### Keep the mock-up isolated and use it to resolve interaction details

The next mock-up is a separate OpenSpec change. It should be an interactive WPF prototype that can show a test or user-opened image and exercise a limited canvas, selection/resizing, a right-side labeled properties panel, and the two drawing modes. It must be clearly identified as experimental and must not be presented as production feature parity.

In the classic mode, the user selects a tool and draws by dragging, matching the current interaction. In click-to-add mode, a click adds the selected tool's item with a tool-appropriate starting size and leaves it selected for adjustment; freehand remains gesture-based. These are the agreed concept-level interactions. Exact default dimensions, placement for line-like tools, and the complete final control behavior should be validated before production specifications are written.

Alternatives considered: integrating a small subset into the production editor immediately, or producing a static visual only. The isolated interactive prototype is preferred because it tests actual canvas interaction without binding the experiment to legacy integration contracts.

### Defer final requirements until after prototype review

No production capability delta is specified here. After reviewing the prototype, convert confirmed behavior into focused capability specs and split implementation work by architectural and feature boundaries. Do not backfill the whole application; specs should grow around the editor behavior being changed.

## Risks / Trade-offs

- [Legacy feature or integration is missed] -> Use the inventory above as a checklist, validate it against the existing editor during production planning, and track an explicit disposition for every entry.
- [A WPF shell is mistaken for a decoupled replacement] -> Keep the prototype isolated and document the WinForms dependencies in `Surface`, `ISurface`, and `IEditorPlugin`; assess these separately before choosing a production architecture.
- [Rewriting the rendering/core behavior changes output or breaks plugins] -> Do not infer reuse or compatibility from the UI prototype; plan focused parity and integration validation before production migration.
- [Click-to-add defaults are awkward for some tools] -> Prototype all relevant tool categories, especially line-like tools; keep freehand gesture-based as agreed and settle exact behavior in later requirements.
- [The prototype expands into a production editor] -> Keep its scope, entry point, and supported interactions explicit in its separate change; no production integrations are included in this concept.
- [Platform constraints are misread as cross-platform support] -> Keep Windows and .NET Framework 4.8 compatibility in scope; this modernization does not imply a cross-platform target.

## Migration Plan

1. Review this concept and its feature inventory.
2. Create a separate change for the isolated WPF mock-up and implement only the interaction slice needed for evaluation.
3. Review the mock-up and use the findings to confirm final user-visible behavior and technical constraints.
4. Write production capability specifications, including explicit feature-parity dispositions, then split the implementation into smaller changes.
5. Decide production migration, integration, compatibility, and rollout strategy in those implementation designs. Until then, the existing editor remains the production editor.
