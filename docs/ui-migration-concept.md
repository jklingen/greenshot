# Greenshot UI Migration Concept

## Executive Summary

This document outlines a comprehensive plan to migrate Greenshot's legacy Windows Forms UI to a modern, scalable UI framework. The migration will be gradual, opt-in during development, and maintain backward compatibility until completion.

## Current State Analysis

### Existing UI Technology Stack

**Primary Framework**: Windows Forms (.NET Framework 4.7.2)
- **Main Application**: `src/Greenshot/Forms/` - 13 form files
- **Image Editor**: `src/Greenshot.Editor/Forms/` - 11 form files  
- **Plugins**: Each plugin has 1-4 forms
- **Total Lines**: ~13,785 lines across all forms

**Key UI Components**:
1. **System Tray Icon & Context Menu** (`MainForm.cs`)
   - Capture options (area, window, fullscreen, last region, IE)
   - File operations (open from clipboard/file)
   - Quick settings and preferences
   - Help, donate, about, exit

2. **Main Forms**:
   - `AboutForm.cs` (389 lines) - Custom animated "G" logo
   - `SettingsForm.cs` (861 lines) - Complex preferences with tabs
   - `MainForm.cs` (2000+ lines) - Application shell and orchestration
   - `LanguageDialog.cs` - Language selection
   - `BugReportForm.cs` - Error reporting
   - `CaptureForm.cs` - Capture preview/selection
   - `PrintOptionsDialog.cs` - Print settings

3. **Image Editor** (`Greenshot.Editor/Forms/`):
   - `ImageEditorForm.cs` (2045 lines) - Main editor window
   - `ColorDialog.cs` - Color picker
   - `ResizeSettingsForm.cs` - Image resize options
   - `DropShadowSettingsForm.cs` - Drop shadow effects
   - `TornEdgeSettingsForm.cs` - Torn edge effects
   - `MovableShowColorForm.cs` - Color preview widget

### Translation System

**Current Implementation**: Custom XML-based system
- **Location**: `src/Greenshot/Languages/` and plugin-specific `Languages/` folders
- **Format**: XML files named `language-{IETF}.xml` (e.g., `language-en-US.xml`)
- **Loader**: `src/Greenshot.Base/Core/Language.cs` - Custom parser
- **Structure**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<language description="English" ietf="en-US" version="1.0.4" languagegroup="1">
  <resources>
    <resource name="about_title">About Greenshot</resource>
    <resource name="contextmenu_settings">Preferences...</resource>
    <!-- 300+ resources per language -->
  </resources>
</language>
```

**Supported Languages**: 25+ languages including:
- English (en-US), German (de-DE), French (fr-FR), Spanish (es-ES)
- Chinese (zh-CN, zh-TW), Japanese (ja-JP), Korean (ko-KR)
- Russian (ru-RU), Polish (pl-PL), Czech (cs-CZ)
- And many more...

**Usage in Code**:
```csharp
Language.GetString(LangKey.about_title)
Language.GetFormattedString(LangKey.error_openlink, link)
```

**Issues**:
- Not human-readable (XML overhead)
- No IDE/tooling support for translations
- Difficult integration with modern translation platforms
- No machine translation compatibility
- Hard to maintain consistency across 25+ languages

### Configuration System

**Implementation**: Custom INI file system
- **File**: `greenshot.ini` (created at runtime in user profile)
- **Library**: `src/Greenshot.Base/IniFile/` - Custom INI parser with attributes
- **Main Config**: `CoreConfiguration.cs` with `[IniProperty]` attributes

**Example Configuration**:
```csharp
[IniSection("Core", Description = "Greenshot core configuration")]
public class CoreConfiguration : IniSection
{
    [IniProperty("Language", Description = "The language in IETF format")]
    public string Language { get; set; }
    
    [IniProperty("IsFirstLaunch", DefaultValue = "true")]
    public bool IsFirstLaunch { get; set; }
}
```

**Access Pattern**:
```csharp
var conf = IniConfig.GetIniSection<CoreConfiguration>();
conf.Language = "de-DE";
```

### Existing Modern UI Components

**WPF Already in Use**: The Confluence plugin already uses WPF!
- **Location**: `src/Greenshot.Plugin.Confluence/Forms/*.xaml`
- **Technology**: WPF with XAML
- **Custom Markup Extension**: `{support:Translate}` for localization
- **Files**:
  - `ConfluenceConfigurationForm.xaml` - Settings dialog
  - `ConfluencePagePicker.xaml` - Page selection
  - `ConfluenceSearch.xaml` - Search interface
  - `ConfluenceTreePicker.xaml` - Tree navigation
  - `ConfluenceUpload.xaml` - Upload dialog

**Project Configuration**: 
- `Microsoft.NET.Sdk.WindowsDesktop` SDK
- `UseWPF` and `UseWindowsForms` both enabled
- Target Framework: .NET Framework 4.7.2

**This is significant**: We already have proof that WPF works in the Greenshot architecture!

### Known Issues with Current UI

1. **Scaling Problems**: Windows Forms doesn't handle DPI scaling well
2. **Translation Overflow**: Text longer in translation causes layout issues
3. **Dated Appearance**: 2000s-era look and feel
4. **Limited Flexibility**: Hard to theme or customize
5. **Accessibility**: Limited support for screen readers and accessibility features
6. **Cross-platform**: Windows Forms is Windows-only

## Technology Recommendations

### Option 1: WPF (Windows Presentation Foundation) ⭐ RECOMMENDED

**Pros**:
- ✅ **Already in use** in Confluence plugin - proven compatibility
- ✅ **Native .NET Framework 4.7.2** support - no version upgrade needed
- ✅ **Mature and stable** - 15+ years of production use
- ✅ **Excellent DPI scaling** - built-in high-DPI support
- ✅ **XAML declarative UI** - separation of concerns
- ✅ **Rich styling** - themes, templates, animations
- ✅ **Data binding** - reduces boilerplate code
- ✅ **Designer support** - Visual Studio XAML designer
- ✅ **Side-by-side with WinForms** - can mix technologies during migration
- ✅ **Large community** - extensive resources and libraries
- ✅ **Accessibility** - built-in UI Automation support

**Cons**:
- ❌ **Windows-only** - no cross-platform support
- ❌ **Learning curve** - XAML and MVVM pattern
- ❌ **Not "modern"** - Microsoft focus is on newer technologies

**Migration Complexity**: LOW - We already have working examples in the codebase

### Option 2: Avalonia UI

**Pros**:
- ✅ **Cross-platform** - Windows, Linux, macOS
- ✅ **XAML-based** - similar to WPF, easier for team to learn
- ✅ **Modern** - actively developed
- ✅ **.NET Standard/Core** - future-proof
- ✅ **Good styling** - flexible theming

**Cons**:
- ❌ **Requires .NET upgrade** - needs .NET 6+ for best experience
- ❌ **Less mature** - smaller ecosystem than WPF
- ❌ **Migration effort** - larger codebase changes
- ❌ **No proven use** - not currently in Greenshot
- ❌ **Designer limitations** - tooling not as mature

**Migration Complexity**: HIGH - Requires .NET version upgrade and significant architectural changes

### Option 3: MAUI (Multi-platform App UI)

**Pros**:
- ✅ **Cross-platform** - Windows, Android, iOS, macOS
- ✅ **Microsoft official** - successor to Xamarin.Forms
- ✅ **Modern** - latest Microsoft technology

**Cons**:
- ❌ **Requires .NET 6+** - complete framework upgrade
- ❌ **Desktop support immature** - mobile-first focus
- ❌ **Not suitable for desktop tools** - better for consumer apps
- ❌ **Massive migration effort** - complete rewrite
- ❌ **Overkill** - Greenshot doesn't need mobile support

**Migration Complexity**: VERY HIGH - Not recommended for desktop screenshot tool

### Option 4: Blazor Hybrid

**Pros**:
- ✅ **Web technologies** - HTML/CSS/JavaScript
- ✅ **Cross-platform** - via WebView2
- ✅ **Modern** - cutting edge

**Cons**:
- ❌ **Not suitable for desktop apps** - web-based approach
- ❌ **Performance concerns** - screenshot tool needs native performance
- ❌ **WebView2 dependency** - additional runtime requirement
- ❌ **Wrong tool for job** - desktop native is better for this use case

**Migration Complexity**: VERY HIGH - Not recommended

### Recommendation: WPF

**Rationale**:
1. ✅ **Minimal risk** - Already proven to work (Confluence plugin)
2. ✅ **No framework upgrade** - Works with current .NET Framework 4.7.2
3. ✅ **Gradual migration** - Can coexist with Windows Forms
4. ✅ **Team familiarity** - Code already exists in the project as reference
5. ✅ **Solves core issues** - DPI scaling, modern look, flexible layouts
6. ✅ **Fastest path to value** - Lowest migration effort

**Future-proofing Note**: When Greenshot is ready for cross-platform (future release), WPF XAML skills and UI structure will translate well to Avalonia UI, making a later cross-platform migration easier.

## Modern Translation System Recommendations

### Current System Issues
- XML overhead makes files less human-readable
- No standard tooling support
- Poor integration with translation platforms (Crowdin, Transifex, Weblate)
- Difficult for machine translation
- Custom parser maintenance burden

### Option 1: RESX with ResX Resource Manager ⭐ RECOMMENDED for .NET Framework

**Pros**:
- ✅ **Native .NET** - built into Visual Studio
- ✅ **Tooling support** - Visual Studio Resource Editor
- ✅ **Strongly typed** - compile-time checking
- ✅ **Works with WPF** - excellent integration
- ✅ **Established** - industry standard for .NET

**Cons**:
- ❌ **Less human-readable** - binary .resx format
- ❌ **Limited platform support** - requires conversion for Crowdin etc.

**RESX + Resource Manager Pattern**:
```xml
<!-- Resources.en-US.resx -->
<data name="about_title">
  <value>About Greenshot</value>
</data>
```

```csharp
// Usage
Resources.Culture = new CultureInfo("de-DE");
string title = Resources.about_title;
```

### Option 2: JSON with Custom or Library-based Solution ⭐ RECOMMENDED for Human-Readability

**Libraries to Consider**:
- **I18Next.Net** - Port of popular JavaScript i18next library
- **Westwind.Globalization** - Mature .NET globalization library
- **Custom JSON parser** - Lightweight, full control

**Pros**:
- ✅ **Human-readable** - Clean, simple format
- ✅ **Platform friendly** - Easy upload to Crowdin, Weblate, Transifex
- ✅ **Machine translation** - Easy to parse for AI translation
- ✅ **Version control friendly** - Better diff/merge
- ✅ **Modern** - Industry standard for web apps
- ✅ **Tooling** - Many translation tools support JSON

**Cons**:
- ❌ **Custom integration needed** - More setup than RESX
- ❌ **Runtime loading** - Not compiled into assemblies

**JSON Example**:
```json
{
  "about": {
    "title": "About Greenshot",
    "bugs": "Please report bugs to",
    "donations": "If you like Greenshot, you are welcome to support us"
  },
  "contextmenu": {
    "settings": "Preferences...",
    "exit": "Exit",
    "capturearea": "Capture region"
  }
}
```

**Usage with I18Next.Net**:
```csharp
// Setup
var translator = new I18NextNet.Translator();
translator.AddTranslation("en-US", jsonContent);

// Usage
string title = translator.Translate("about.title");
```

### Option 3: PO Files (Gettext)

**Pros**:
- ✅ **Industry standard** - Used by thousands of open source projects
- ✅ **Human-readable** - Plain text format
- ✅ **Excellent tooling** - Poedit, Lokalize, many web platforms
- ✅ **Proven** - Decades of use

**Cons**:
- ❌ **Less .NET native** - Needs NGettext or similar library
- ❌ **Additional complexity** - .po/.pot/.mo file management

**PO File Example**:
```po
msgid "about_title"
msgstr "About Greenshot"

msgid "contextmenu_settings"
msgstr "Preferences..."
```

### Hybrid Recommendation: JSON Primary, RESX for WPF Bindings

**Strategy**:
1. **JSON files** as source of truth - human-readable, platform-friendly
2. **Build-time generation** of RESX files from JSON for WPF XAML bindings
3. **Runtime JSON loading** for dynamic scenarios
4. **Migration tool** to convert existing XML to JSON

**Benefits**:
- ✅ Best of both worlds
- ✅ Human-readable source files
- ✅ Platform integration (Crowdin, etc.)
- ✅ Strong typing in code (via generated RESX)
- ✅ XAML binding support
- ✅ Machine translation friendly

**Implementation Approach**:
```
Languages/
  en-US.json          (source of truth)
  de-DE.json
  fr-FR.json
  
Build/
  GenerateResx.targets  (MSBuild task)
  
Generated/
  Resources.en-US.resx  (auto-generated)
  Resources.de-DE.resx
```

## Step-by-Step Migration Plan

### Phase 0: Infrastructure Setup (Week 1-2)

**Tasks**:
1. ✅ Create migration concept document (this document)
2. Create new project structure:
   ```
   src/Greenshot.UI.Modern/
     Greenshot.UI.Modern.csproj
     Core/
       ModernUIManager.cs       - Manages UI mode selection
       ViewModelBase.cs         - Base class for ViewModels
     Views/
       (Empty - will be populated per phase)
     Resources/
       Translations/
         en-US.json
     Converters/
       (WPF value converters)
   ```
3. Add configuration flag to `CoreConfiguration.cs`:
   ```csharp
   [IniProperty("UseModernUI", Description = "Enable modern WPF UI (experimental)", 
                DefaultValue = "false")]
   public bool UseModernUI { get; set; }
   ```
4. Implement translation system:
   - Create JSON translation files for all existing languages
   - Build migration tool: `Tools/XmlToJsonTranslation/`
   - Create MSBuild task to generate RESX from JSON
   - Create WPF markup extension for translations
5. Create UI factory pattern:
   ```csharp
   public interface IUIFactory
   {
       IAboutDialog CreateAboutDialog();
       ISettingsDialog CreateSettingsDialog();
       // ... etc
   }
   
   public class WinFormsUIFactory : IUIFactory { }
   public class ModernUIFactory : IUIFactory { }
   ```

**Deliverables**:
- `Greenshot.UI.Modern` project created
- Translation JSON files for all 25+ languages
- Build process for RESX generation
- Configuration flag in greenshot.ini
- UI factory infrastructure

### Phase 1: About Dialog (Week 3-4) - PILOT

**Why Start Here**:
- ✅ Simple, self-contained dialog
- ✅ Good visual impact (can showcase modern styling)
- ✅ Low risk - not critical to functionality
- ✅ Has custom graphics (animated G logo) - good test of WPF capabilities

**Implementation**:

1. Create `src/Greenshot.UI.Modern/Views/AboutView.xaml`:
```xaml
<Window x:Class="Greenshot.UI.Modern.Views.AboutView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:Greenshot.UI.Modern"
        Title="{local:Translate about.title}" 
        SizeToContent="WidthAndHeight"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen"
        Background="#FF3D3D3D">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Logo (can reuse existing animation logic in code-behind) -->
        <Image Grid.Row="0" x:Name="LogoImage" Width="90" Height="90" 
               Margin="0,0,0,20"/>
        
        <!-- Version Info -->
        <TextBlock Grid.Row="1" x:Name="VersionText" 
                   FontSize="16" FontWeight="Bold"
                   Foreground="White" TextAlignment="Center"
                   Margin="0,0,0,10"/>
        
        <!-- Links -->
        <StackPanel Grid.Row="2" Margin="0,10">
            <TextBlock Foreground="LightGray">
                <Hyperlink NavigateUri="https://getgreenshot.org/" 
                           RequestNavigate="Hyperlink_RequestNavigate"
                           Foreground="#FF8AFF00">
                    <Run Text="{local:Translate about.host}"/>
                </Hyperlink>
            </TextBlock>
            <!-- More links... -->
        </StackPanel>
        
        <!-- Close Button -->
        <Button Grid.Row="3" Content="{local:Translate OK}" 
                IsDefault="True" Click="CloseButton_Click"
                Padding="20,5" HorizontalAlignment="Center"
                Margin="0,20,0,0"/>
    </Grid>
</Window>
```

2. Create ViewModel `AboutViewModel.cs`
3. Update `MainForm.cs` to check configuration and instantiate correct dialog:
```csharp
private void ShowAboutDialog()
{
    var conf = IniConfig.GetIniSection<CoreConfiguration>();
    if (conf.UseModernUI)
    {
        var modernAbout = new Greenshot.UI.Modern.Views.AboutView();
        modernAbout.ShowDialog();
    }
    else
    {
        var classicAbout = new AboutForm();
        classicAbout.ShowDialog();
    }
}
```

**Testing**:
- Manual testing with `UseModernUI=false` (default, old UI)
- Manual testing with `UseModernUI=true` (new UI)
- Test with multiple languages
- Test DPI scaling at 100%, 125%, 150%, 200%
- Visual comparison screenshots

**Success Criteria**:
- ✅ Both UIs work correctly
- ✅ Switching via config flag works
- ✅ Modern UI looks better and scales properly
- ✅ Translations work in both UIs
- ✅ No impact on users who don't enable flag

### Phase 2: System Tray Context Menu (Week 5-7)

**Why Second**:
- ✅ High-visibility component (used constantly)
- ✅ Good impact on user experience
- ✅ Relatively straightforward
- ✅ Tests the pattern for menus

**Challenges**:
- WPF doesn't have native NotifyIcon support
- Need third-party library or custom implementation

**Solution**: Use **Hardcodet.NotifyIcon.Wpf** library
- Mature, well-maintained NuGet package
- Designed specifically for WPF system tray icons
- XAML-based context menu definition

**Implementation**:

1. Add NuGet package: `Hardcodet.NotifyIcon.Wpf`
2. Create `SystemTrayView.xaml`:
```xaml
<tb:TaskbarIcon x:Class="Greenshot.UI.Modern.Views.SystemTrayView"
                xmlns:tb="http://www.hardcodet.net/taskbar"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:local="clr-namespace:Greenshot.UI.Modern"
                IconSource="/Greenshot;component/Resources/icon.ico"
                ToolTipText="Greenshot">
    
    <tb:TaskbarIcon.ContextMenu>
        <ContextMenu>
            <MenuItem Header="{local:Translate contextmenu.capturearea}"
                      Command="{Binding CaptureAreaCommand}">
                <MenuItem.Icon>
                    <Image Source="/Resources/region.png"/>
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header="{local:Translate contextmenu.capturelastregion}"
                      Command="{Binding CaptureLastRegionCommand}"/>
            <!-- ... more menu items ... -->
            <Separator/>
            <MenuItem Header="{local:Translate contextmenu.settings}"
                      Command="{Binding ShowSettingsCommand}"/>
            <MenuItem Header="{local:Translate contextmenu.about}"
                      Command="{Binding ShowAboutCommand}"/>
            <Separator/>
            <MenuItem Header="{local:Translate contextmenu.exit}"
                      Command="{Binding ExitCommand}"/>
        </ContextMenu>
    </tb:TaskbarIcon.ContextMenu>
</tb:TaskbarIcon>
```

3. Create `SystemTrayViewModel.cs` with ICommand implementations
4. Update `MainForm.cs` to conditionally create WPF or WinForms tray icon

**Testing**:
- All menu items work correctly
- Icons display properly
- Keyboard shortcuts work
- Multi-monitor scenarios
- High DPI displays

### Phase 3: Settings Dialog (Week 8-12)

**Why Third**:
- ✅ Most complex dialog - good stress test
- ✅ High user impact
- ✅ Currently has many layout issues with translations

**Challenges**:
- Large, complex form with multiple tabs
- Many controls and validation logic
- Plugin settings integration

**Implementation Strategy**:

1. **Analyze current structure**:
   - `SettingsForm.cs` has ~861 lines
   - Uses tab control with 6 tabs:
     - General
     - Capture
     - Output
     - Printer
     - Plugins
     - Expert

2. **Create modular WPF structure**:
```
Views/
  Settings/
    SettingsWindow.xaml          (main window with tab control)
    GeneralTab.xaml               (general settings)
    CaptureTab.xaml               (capture settings)
    OutputTab.xaml                (output settings)
    PrinterTab.xaml               (printer settings)
    PluginsTab.xaml               (plugin settings)
    ExpertTab.xaml                (expert settings)
ViewModels/
  Settings/
    SettingsViewModel.cs          (main VM, coordinates tabs)
    GeneralTabViewModel.cs
    CaptureTabViewModel.cs
    OutputTabViewModel.cs
    PrinterTabViewModel.cs
    PluginsTabViewModel.cs
    ExpertTabViewModel.cs
```

3. **Use WPF data binding**:
```xaml
<!-- Example: General Tab -->
<UserControl x:Class="Greenshot.UI.Modern.Views.Settings.GeneralTab">
    <StackPanel Margin="10">
        <!-- Language Selection -->
        <Label Content="{local:Translate settings.language}"/>
        <ComboBox ItemsSource="{Binding AvailableLanguages}"
                  SelectedItem="{Binding SelectedLanguage}"
                  DisplayMemberPath="Description"/>
        
        <!-- Startup -->
        <CheckBox Content="{local:Translate settings.autostartshortcut}"
                  IsChecked="{Binding StartupEnabled}"/>
        
        <!-- ... more controls ... -->
    </StackPanel>
</UserControl>
```

4. **Validation with IDataErrorInfo**:
```csharp
public class CaptureTabViewModel : ViewModelBase, IDataErrorInfo
{
    private int _captureDelay;
    
    public int CaptureDelay
    {
        get => _captureDelay;
        set
        {
            if (_captureDelay != value)
            {
                _captureDelay = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string this[string columnName]
    {
        get
        {
            if (columnName == nameof(CaptureDelay))
            {
                if (CaptureDelay < 0 || CaptureDelay > 5000)
                    return "Delay must be between 0 and 5000ms";
            }
            return null;
        }
    }
}
```

**Testing**:
- All settings save/load correctly
- Validation works
- Plugin settings integration
- Multi-language testing
- DPI scaling
- Tab navigation and keyboard shortcuts

### Phase 4: Language Selection Dialog (Week 13)

**Why Fourth**:
- ✅ Simple dialog
- ✅ Quick win after complex settings dialog
- ✅ Tests combo box/list box patterns

**Implementation**:
- Single window with language list
- Filter/search capability
- Preview of selected language

### Phase 5: Capture-Related Dialogs (Week 14-15)

**Dialogs to Migrate**:
- `CaptureForm.cs` - Capture preview/selection
- `PrintOptionsDialog.cs` - Print settings

**Implementation**:
- Follow established patterns from previous phases
- Focus on maintaining existing functionality
- Improve layouts for better scaling

### Phase 6: Plugin Settings Forms (Week 16-18)

**Scope**:
- Migrate plugin settings dialogs to WPF
- Each plugin has 1-2 settings forms
- ~10 plugins to update

**Strategy**:
- Create base plugin settings view/viewmodel
- Each plugin extends base classes
- Maintain plugin API compatibility

**Note**: Some plugins like Confluence already use WPF - document as reference!

### Phase 7: Image Editor (Week 19-25) - COMPLEX

**Why Last**:
- ❌ Most complex component (2045 lines in `ImageEditorForm.cs`)
- ❌ Custom drawing code
- ❌ Many child dialogs (color picker, resize, effects)
- ❌ Performance-critical
- ❌ Highest risk

**Strategy**:
- Evaluate if full migration is necessary
- Consider hybrid approach (WPF shell, WinForms editor control)
- Potentially defer to future major version
- May use Windows Forms for editor, WPF for toolbars/chrome

**Sub-dialogs to Consider**:
- `ColorDialog.cs` - Custom color picker
- `ResizeSettingsForm.cs` - Resize options
- `DropShadowSettingsForm.cs` - Effect settings
- `TornEdgeSettingsForm.cs` - Effect settings

### Phase 8: Final Cutover (Week 26+)

**Tasks**:
1. Comprehensive testing of all migrated components
2. Update documentation
3. Create migration guide for contributors
4. **Change default**: `UseModernUI=true` by default
5. Announce in release notes
6. Keep old UI available for one more release (safety net)
7. Plan to remove old UI in subsequent release

**Testing Checklist**:
- [ ] All UI components work in modern mode
- [ ] All translations display correctly
- [ ] DPI scaling works at 100%, 125%, 150%, 200%, 300%
- [ ] Keyboard navigation and shortcuts work
- [ ] Screen reader accessibility tested
- [ ] Multi-monitor scenarios
- [ ] Theme consistency
- [ ] Performance benchmarks (startup time, memory usage)
- [ ] Plugin compatibility
- [ ] Upgrade path from old config to new config

## Configuration Strategy

### greenshot.ini Configuration

Add new configuration section and properties:

```ini
[Core]
; Enable modern WPF UI (false = classic Windows Forms UI)
; During migration phases 1-7, this defaults to false
; After Phase 8, this defaults to true
; Old UI will be removed in a future release
UseModernUI=false

; UI Theme for modern UI (light, dark, auto)
; Only applies when UseModernUI=true
UITheme=auto

; Translation format (xml, json)
; xml = legacy format (deprecated)
; json = new format (recommended)
; During migration, both are supported
TranslationFormat=json
```

### Implementation in CoreConfiguration.cs

```csharp
[IniSection("Core", Description = "Greenshot core configuration")]
public class CoreConfiguration : IniSection
{
    // ... existing properties ...
    
    [IniProperty("UseModernUI", 
        Description = "Enable modern WPF UI. false = Windows Forms (classic), true = WPF (modern)",
        DefaultValue = "false")]  // Will change to "true" in Phase 8
    public bool UseModernUI { get; set; }
    
    [IniProperty("UITheme",
        Description = "UI theme for modern UI: light, dark, auto (follows system)",
        DefaultValue = "auto")]
    public UITheme Theme { get; set; }
    
    [IniProperty("TranslationFormat",
        Description = "Translation file format: xml (legacy) or json (modern)",
        DefaultValue = "json")]
    public TranslationFormat TranslationFormat { get; set; }
}

public enum UITheme
{
    Light,
    Dark,
    Auto
}

public enum TranslationFormat
{
    Xml,
    Json
}
```

### UI Mode Selection Logic

**Location**: `src/Greenshot.UI.Modern/Core/ModernUIManager.cs`

```csharp
public static class ModernUIManager
{
    private static CoreConfiguration _config;
    
    public static void Initialize()
    {
        _config = IniConfig.GetIniSection<CoreConfiguration>();
    }
    
    public static bool IsModernUIEnabled => _config?.UseModernUI ?? false;
    
    public static void ShowAboutDialog()
    {
        if (IsModernUIEnabled)
        {
            var dialog = new Greenshot.UI.Modern.Views.AboutView();
            dialog.ShowDialog();
        }
        else
        {
            var dialog = new Greenshot.Forms.AboutForm();
            dialog.ShowDialog();
        }
    }
    
    public static void ShowSettingsDialog()
    {
        if (IsModernUIEnabled)
        {
            var dialog = new Greenshot.UI.Modern.Views.Settings.SettingsWindow();
            dialog.ShowDialog();
        }
        else
        {
            var dialog = new Greenshot.Forms.SettingsForm();
            dialog.ShowDialog();
        }
    }
    
    // ... other dialog factory methods ...
}
```

### Migration Path for Users

**Phase 1-7 (Development/Testing)**:
```ini
; Default for all users - old UI
UseModernUI=false

; Power users / testers can opt-in
UseModernUI=true
```

**Phase 8 (Soft Launch)**:
```ini
; New installs default to modern UI
UseModernUI=true

; Existing users keep their setting (false if upgrading)
; They can manually change to true when ready
```

**Future Release (Complete Migration)**:
```ini
; Remove UseModernUI flag entirely
; Remove all WinForms UI code
; WPF is the only UI
```

## Translation Platform Integration

### Recommended Platform: **Crowdin** (or Weblate)

**Why Crowdin**:
- ✅ Free for open source projects
- ✅ Excellent JSON support
- ✅ Machine translation integration (DeepL, Google Translate)
- ✅ Translation memory
- ✅ Context screenshots
- ✅ GitHub integration (automatic PR creation)
- ✅ Proofreading workflow
- ✅ Used by many open source projects

**Setup Process**:
1. Create Crowdin project for Greenshot
2. Upload `en-US.json` as source language
3. Configure 25+ target languages
4. Invite community translators
5. Set up GitHub integration for automated sync
6. Configure machine translation pre-fill (DeepL recommended)

**Workflow**:
```
Developer adds new string → en-US.json → Git commit → 
Crowdin auto-sync → Translators notified → 
Translations completed → Crowdin creates PR → 
Review and merge → Translations in next build
```

### File Structure for Translation Platform

```
src/Greenshot.UI.Modern/Resources/Translations/
  en-US.json          (source language)
  de-DE.json          (auto-synced from Crowdin)
  fr-FR.json
  es-ES.json
  ... (25+ languages)
  
  _metadata/
    context-screenshots/    (UI screenshots for translators)
    translation-notes.md    (context for translators)
```

### Migration from XML to JSON

**Tool**: `Tools/XmlToJsonTranslation/Program.cs`

```csharp
// Converts language-en-US.xml → en-US.json
// Preserves all translations
// Handles special characters
// Validates structure

public class XmlToJsonConverter
{
    public void ConvertAll(string sourcePath, string targetPath)
    {
        // For each language-*.xml file
        // Parse XML structure
        // Convert to JSON hierarchy
        // Preserve formatting
        // Write to output
    }
}
```

**Usage**:
```bash
cd Tools/XmlToJsonTranslation
dotnet run -- --source ../../src/Greenshot/Languages --target ../../src/Greenshot.UI.Modern/Resources/Translations
```

## Benefits of Migration

### For Users

1. **Better DPI Scaling**: Text and UI elements scale properly on high-DPI displays
2. **Modern Appearance**: Updated look and feel, more professional
3. **Better Translations**: More room for longer translations, no more cut-off text
4. **Accessibility**: Better screen reader support, keyboard navigation
5. **Theming**: Light and dark theme support
6. **Smoother Animations**: WPF's hardware-accelerated rendering

### For Developers

1. **Easier Maintenance**: XAML separation of UI from logic
2. **Better Tooling**: Visual Studio XAML designer, XAML Hot Reload
3. **Modern Patterns**: MVVM, data binding reduces boilerplate
4. **Type Safety**: Compile-time checking of bindings (with compiled bindings)
5. **Testability**: ViewModels easier to unit test than WinForms
6. **Reusability**: Styles, templates, custom controls

### For Translators

1. **Human-Readable Format**: JSON instead of XML
2. **Platform Support**: Crowdin/Weblate integration
3. **Machine Translation**: Pre-fill with DeepL/Google Translate
4. **Context**: Screenshots and notes for each string
5. **Translation Memory**: Reuse of similar translations
6. **Easier Contributions**: Lower barrier to entry

## Risks and Mitigations

### Risk 1: Breaking Existing Functionality

**Mitigation**:
- ✅ Gradual migration, one component at a time
- ✅ Both UIs run side-by-side during migration
- ✅ Comprehensive testing checklist for each phase
- ✅ Old UI remains available as fallback
- ✅ Config flag allows instant switch back

### Risk 2: Performance Regression

**Mitigation**:
- ✅ Benchmark before and after each phase
- ✅ WPF is hardware-accelerated (often faster than WinForms)
- ✅ Performance testing on low-end hardware
- ✅ Monitor memory usage

### Risk 3: Learning Curve for Team

**Mitigation**:
- ✅ Confluence plugin already uses WPF (reference code)
- ✅ Extensive documentation in this concept doc
- ✅ Simple pilot (About dialog) to learn pattern
- ✅ Code reviews to share knowledge

### Risk 4: Translation Quality During Migration

**Mitigation**:
- ✅ Automated conversion from XML to JSON preserves all translations
- ✅ Both formats supported during migration
- ✅ Validate conversion with diff tools
- ✅ Community review before cutover

### Risk 5: Plugin Compatibility

**Mitigation**:
- ✅ Plugin API remains unchanged
- ✅ Plugins can use either WinForms or WPF
- ✅ Confluence plugin proves WPF plugins work
- ✅ Gradual plugin migration, not forced

### Risk 6: Cross-Platform Expectations

**Mitigation**:
- ✅ Clear communication: This is WPF, Windows-only
- ✅ Future migration to Avalonia is possible (similar XAML)
- ✅ Focus on solving current problems first
- ✅ Document cross-platform as future consideration

## Timeline Summary

| Phase | Duration | Component | Complexity |
|-------|----------|-----------|------------|
| 0 | 2 weeks | Infrastructure Setup | Medium |
| 1 | 2 weeks | About Dialog | Low |
| 2 | 3 weeks | System Tray Context Menu | Medium |
| 3 | 5 weeks | Settings Dialog | High |
| 4 | 1 week | Language Selection Dialog | Low |
| 5 | 2 weeks | Capture-Related Dialogs | Medium |
| 6 | 3 weeks | Plugin Settings Forms | Medium |
| 7 | 7 weeks | Image Editor | Very High |
| 8 | 2+ weeks | Final Cutover & Testing | High |
| **Total** | **27+ weeks** | **Complete Migration** | **~6-7 months** |

**Note**: Timeline assumes one developer working part-time. Can be accelerated with multiple contributors.

## Success Metrics

### Quantitative Metrics

1. **DPI Scaling**: UI renders correctly at 100%, 125%, 150%, 200%, 300% without clipping
2. **Translation Coverage**: All 25+ languages migrated successfully
3. **Performance**: Startup time < 2 seconds (same or better than WinForms)
4. **Memory**: Memory usage < 100MB baseline (same or better)
5. **Bug Reports**: < 10 critical bugs reported in first month after launch

### Qualitative Metrics

1. **User Feedback**: Positive sentiment on modern look and feel
2. **Contributor Feedback**: Developers find WPF easier to work with
3. **Translator Feedback**: JSON format easier to work with than XML
4. **Accessibility**: Screen reader users report improved experience

## Conclusion

This migration plan provides a **low-risk, incremental path** to modernize Greenshot's UI while maintaining full backward compatibility. By leveraging WPF (already proven in the Confluence plugin), we can deliver significant improvements to users without requiring a .NET version upgrade or massive architectural changes.

The gradual, opt-in approach ensures that:
- ✅ Users are not disrupted during migration
- ✅ Each component can be thoroughly tested before release
- ✅ Team can learn WPF incrementally
- ✅ Risk is minimized at each step
- ✅ Value is delivered early (About dialog, context menu)

**Recommendation**: Approve and begin with Phase 0 (Infrastructure Setup) and Phase 1 (About Dialog) as a pilot to validate the approach.

---

**Document Version**: 1.0  
**Date**: January 2026  
**Author**: GitHub Copilot  
**Status**: Proposal - Awaiting Review
