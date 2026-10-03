# Tasks

## 1. Record the current editor and architecture

- [x] 1.1 Inventory the existing editor's user-facing features, settings, and integrations in `design.md`; verify that the inventory covers tools, object editing, properties, filters, image operations, navigation, output, and application integration.
- [x] 1.2 Document the WinForms and `System.Drawing` dependencies at the editor UI, surface, and plugin boundaries; verify the design names `ImageEditorForm`, `Surface`, `ISurface`, and `IEditorPlugin`.

## 2. Define the concept and follow-up

- [x] 2.1 Record WPF as the working UI direction, with production rendering and core reuse deferred; verify that alternatives and the rationale are documented in `design.md`.
- [x] 2.2 Separate the isolated interactive mock-up from this concept and record its limited evaluation scope; verify `proposal.md` and `design.md` explicitly defer implementation and production integration.
- [x] 2.3 Record the sequence from concept review to mock-up, final requirements, and smaller production changes; verify the migration plan in `design.md` keeps the existing editor in production until a later migration plan is approved.
