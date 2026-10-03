# Proposal

## Why

The editor redesign concept identifies WPF as the leading direction for a modern editor UI, but the existing editor surface and plugin contracts are tightly coupled to WinForms. Before committing to production requirements or migration architecture, an isolated interactive prototype is needed to evaluate the proposed canvas workflow and right-side properties panel without changing Greenshot's production editor.

## What Changes

- Add a standalone WPF desktop mock-up project for experimentation. It is not launched from, referenced by, or shipped with the Greenshot application.
- Let a user open an image and provide a generated sample canvas when no image is opened.
- Demonstrate a deliberately limited annotation toolset, object selection and move/resize, and a right-side properties panel with visible labels and editable representative properties.
- Demonstrate the existing select-tool-then-drag interaction and a tool-click-to-add interaction for each drawing tool in the mock-up except freehand. In click-to-add mode, clicking a tool immediately creates a centered object with a default width of 10% of the document width and, where applicable, a golden-ratio aspect ratio. Successive objects are slightly offset. The new object remains selected while the active tool stays Select, so canvas gestures can move or resize it; clicking a tool again adds another object of that kind. Freehand remains drag-drawn. The mode selector is local to the prototype and does not change Greenshot's global configuration.
- Keep the prototype disposable and limited to design validation. It does not implement feature parity, production rendering, capture integration, saving/export, plugins, recipes, or production settings.
- Use feedback from the running mock-up to inform a later requirements and architecture change.

## Capabilities

This is an isolated design prototype, not a change to durable Greenshot product behavior. It intentionally has no delta specifications; prototype observations will be reviewed before production capabilities and requirements are defined.

### New Capabilities

None in this prototype change.

### Modified Capabilities

None in this prototype change.

## Impact

- A new standalone WPF mock-up project outside the Greenshot solution, with no reference from the shipping Greenshot application or `Greenshot.Editor`.
- Windows and the repository's .NET Framework 4.8/MSBuild environment.
- No production editor APIs, settings, language files, plugins, capture/export destinations, installer, or released application behavior are changed.
