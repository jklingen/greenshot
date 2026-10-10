# Greenshot Editor Mock-up

This is an experimental, standalone WPF prototype. It is not part of the Greenshot application or shipping solution.

From the repository root, build and run it with Visual Studio MSBuild:

```powershell
msbuild tools\Greenshot.Editor.Mockup\Greenshot.Editor.Mockup.csproj /p:Configuration=Debug /restore /t:Build
.\tools\Greenshot.Editor.Mockup\bin\Debug\net480\Greenshot.Editor.Mockup.exe
```

The mock-up opens with a generated sample canvas. Use **Open image...** to load a bitmap and select either **Tool then drag** or **Click to add**. In click-to-add mode, click a drawing-tool button to add a centered object immediately; another click on a tool adds another object. The Select tool stays active so you can immediately move or resize the selected object on the canvas. Edits are temporary and are not saved.

Double-click a text annotation to edit it inline. The **Text** properties group changes formatting for selected characters while editing, or for the entire text annotation when no characters are selected. Press **Esc** to finish editing; **F2** starts editing the selected text annotation from the keyboard.

Every element except freehand can carry text: select one element and just start typing (or double-click / F2). Text is centered in rectangles and ellipses and runs centered above lines and arrows, always readable left to right. Vertical alignment has no effect on lines.

## Keyboard

- F6 / Shift+F6: switch region (toolbar, tools, canvas, properties)
- Canvas: Tab / Shift+Tab select next / previous object (Ctrl+Tab moves only the focus frame), Space toggles the framed object, Ctrl+A, Esc, Delete
- Arrows move the selection (Shift = 10 px), Alt+arrows resize it, Enter jumps to the Layout fields, F2 edits a selected text object, Esc returns to the canvas or finishes text editing
