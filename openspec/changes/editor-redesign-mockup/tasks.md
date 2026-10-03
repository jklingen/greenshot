# Tasks

## 1. Standalone Project and Canvas

- [x] 1.1 Create `tools/Greenshot.Editor.Mockup/` as an independent WPF `net480` project, with no production project references and no entry in `src/Greenshot.sln`; verify it builds with `msbuild tools\Greenshot.Editor.Mockup\Greenshot.Editor.Mockup.csproj /p:Configuration=Debug /t:Build` and confirm the shipping solution file is unchanged.
- [x] 1.2 Implement the experimental main window, deterministic sample canvas, and image-open dialog; verify the sample appears at startup, a valid image replaces it, and a decode failure is visible while preserving the previous canvas.
- [x] 1.3 Display image and annotations in a shared coordinate space; verify annotation placement and selection remain aligned after resizing the window and loading images with different aspect ratios.

## 2. Annotation and Interaction Modes

- [x] 2.1 Implement selection plus rectangle, ellipse, line, arrow, text, and freehand objects using a local prototype model and WPF rendering; verify each tool creates a visible object and freehand is created by dragging.
- [x] 2.2 Implement classic drag-to-draw and tool-click-to-add modes for rectangle, ellipse, line, arrow, and text; verify each tool click immediately adds a centered object with a 10%-document-width default, successive additions are offset, Select remains active for moving/resizing, and freehand remains drag-drawn.
- [x] 2.3 Implement object hit-testing, selection bounds, moving, and resizing, including endpoint resizing for line and arrow objects; verify these operations work in both placement modes without altering the source image.

## 3. Properties Panel

- [x] 3.1 Add a labeled right-side properties panel for the selected object's position, dimensions, line color, fill color, and line thickness; verify editing supported values updates the selected object on the canvas immediately and selection changes refresh the panel.

## 4. Prototype Review

- [x] 4.1 Add concise project-local run instructions and perform a manual smoke review of both interaction modes, image loading and failure handling, tool-click object creation, selection, transforms, and property edits; verify the prototype remains outside Greenshot startup, solution builds, and release packaging.
