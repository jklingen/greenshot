# Greenshot Editor Mock-up

This is an experimental, standalone WPF prototype. It is not part of the Greenshot application or shipping solution.

From the repository root, build and run it with Visual Studio MSBuild:

```powershell
msbuild tools\Greenshot.Editor.Mockup\Greenshot.Editor.Mockup.csproj /p:Configuration=Debug /restore /t:Build
.\tools\Greenshot.Editor.Mockup\bin\Debug\net480\Greenshot.Editor.Mockup.exe
```

The mock-up opens with a generated sample canvas. Use **Open image...** to load a bitmap and select either **Tool then drag** or **Click to add**. In click-to-add mode, click a drawing-tool button to add a centered object immediately; another click on a tool adds another object. The Select tool stays active so you can immediately move or resize the selected object on the canvas. Edits are temporary and are not saved.
