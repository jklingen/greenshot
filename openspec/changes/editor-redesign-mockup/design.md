# Design

## Context

See `proposal.md` for motivation and scope. Greenshot already uses WPF on Windows, but the production editor's `Surface`, `ISurface`, and `IEditorPlugin` remain tied to WinForms and `System.Drawing`. The prototype must not take a dependency on those production editor boundaries.

The repository's shared build properties live under `src/Directory.Build.props` and target .NET Framework 4.8. Adding a mock-up project beneath `src` or to `Greenshot.sln` would make it part of ordinary solution builds. The prototype is disposable and must not become part of release packaging.

## Goals / Non-Goals

**Goals:**

- Make the prototype easy to open, build, and run independently on the supported Windows development environment.
- Keep image, annotation, selection, and interaction state local to the prototype.
- Test the agreed interaction concepts with a deliberately small but representative set of drawing tools.
- Provide a usable right-side property editor without implying that its fields are the final production property contract.

**Non-Goals:**

- Reuse or port production `Surface`, drawable containers, filters, `ISurface`, editor plugins, capture services, or destination/export code.
- Add the prototype to the shipping application's startup flow, main solution, installer, or release artifacts.
- Save edits or produce production-quality image output.
- Define production configuration persistence or final requirements for the replacement editor.

## Decisions

### Keep the prototype as a standalone WPF project outside the shipping solution

Place the project under `tools/Greenshot.Editor.Mockup/`, with its own SDK-style project targeting `net480` and WPF. Do not add a project reference to `Greenshot.Editor` or `Greenshot.Base`, and do not add it to `src/Greenshot.sln`. It can be built directly with Visual Studio MSBuild and launched from its own output.

This keeps the experiment independent of the current WinForms surface, prevents it from being built or packaged as part of normal Greenshot solution/release workflows, and makes its disposable status visible in the repository.

Alternative considered: put a WPF window inside `Greenshot.Editor` or add a demo project to the main solution. Both approaches introduce avoidable coupling or include an unfinished UI in routine application builds.

### Use WPF controls and an in-memory prototype model

Represent the loaded image and sample canvas with WPF imaging, and represent prototype annotations with a small local model rendered as WPF shapes on an overlay canvas. Keep prototype coordinates in image space and translate pointer positions through the displayed image bounds so resizing the window does not change annotation placement.

If no image is loaded, render a deterministic synthetic sample rather than requiring a checked-in image asset. Provide a standard image-open dialog; report an unreadable or unsupported file visibly and leave the current canvas intact.

Alternative considered: reference the production drawing engine or a third-party graphics package. Neither is needed to validate this UI slice, and both would obscure whether the new interaction can stand independently.

### Use a small representative toolset with consistent placement rules

Provide selection, rectangle, ellipse, line, arrow, text, and freehand tools. In classic mode, shape tools create their object from a drag gesture. In click-to-add mode, rectangle, ellipse, line, arrow, and text create an object on click with a tool-appropriate default size and select it. Line and arrow use a default horizontal span with endpoint resizing. Freehand remains drag-drawn in either mode. In both modes, users can select, move, and resize existing objects.

Expose the mode as a prototype-only selector. Do not write it to Greenshot configuration or claim the prototype has validated configuration persistence.

Alternative considered: support every legacy tool and filter in the mock-up. That would turn a focused UI experiment into a broad editor implementation before production requirements have been agreed.

### Make selection and properties observable and editable

Use visible selection bounds and resize handles on the canvas. The right-side panel displays labeled position, width, and height controls plus representative style controls (line color, fill color, and line thickness) for the selected object; changing a control updates the prototype object and canvas immediately. Hide or disable properties when no compatible object is selected.

The small property set is chosen to validate the panel's layout and edit loop, not to specify final controls or mixed-selection behavior.

### Keep prototype state volatile and identify the UI as experimental

Image edits, annotations, and mode selection live only for the current process. Do not offer save, export, upload, print, recipe, plugin, or capture integration. Mark the window as an experimental mock-up so screenshots and demonstrations are not mistaken for a production editor.

Any user-visible labels added by implementation must follow the repository localization workflow, with the translation-manager consulted. This does not add strings to production editor language files or create a production UI commitment.

## Risks / Trade-offs

- [Prototype behavior is mistaken for a production commitment] -> Label it experimental, keep it outside the application and shipping solution, and define its behavior as a prototype-only validation target.
- [Image scaling causes pointer and selection drift] -> Store geometry in image coordinates and manually verify alignment at different window sizes and aspect ratios.
- [Click-to-add default sizes do not feel right for line-like tools] -> Use adjustable defaults, test line and arrow endpoint resizing, and record feedback for production requirements rather than hard-coding the prototype defaults as final.
- [A loaded image is too large, malformed, or unsupported] -> Use WPF image decoding with explicit failure feedback and preserve the previous canvas on failure.
- [Prototype work grows to feature parity] -> Limit tools and properties to the inventory in this design; defer all missing legacy behavior to later requirements and implementation changes.
- [The project inherits unintended Greenshot build/release behavior] -> Keep it under `tools`, outside `Greenshot.sln`, with no production project references; verify the shipping solution remains unchanged.

## Migration Plan

1. Create the standalone WPF project and verify it builds independently with Visual Studio MSBuild.
2. Implement image/sample-canvas display, the representative tools, selection/resize, and the properties panel.
3. Exercise both placement modes and image-open failure behavior manually; confirm the main Greenshot solution and packaging configuration are untouched.
4. Review the running prototype and capture user feedback.
5. Retain or discard the prototype based on that review. Record confirmed production requirements and architectural choices in a separate follow-up change; do not migrate its implementation directly into the production editor.
