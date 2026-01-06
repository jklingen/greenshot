# Greenshot UI Migration - Technical Reference

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Translation System Implementation](#translation-system-implementation)
3. [MVVM Pattern Reference](#mvvm-pattern-reference)
4. [WPF Conversion Examples](#wpf-conversion-examples)
5. [Build System Integration](#build-system-integration)
6. [Testing Strategy](#testing-strategy)

## Architecture Overview

### Current Architecture (WinForms)

```
┌─────────────────────────────────────────┐
│           MainForm (WinForms)           │
│  - System tray icon (NotifyIcon)        │
│  - Context menu (ContextMenuStrip)      │
│  - Plugin management                     │
│  - Capture coordination                  │
└──────────────┬──────────────────────────┘
               │
               ├──> AboutForm (WinForms)
               ├──> SettingsForm (WinForms)
               ├──> LanguageDialog (WinForms)
               ├──> BugReportForm (WinForms)
               │
               ├──> ImageEditorForm (WinForms)
               │    └──> ColorDialog, ResizeForm, etc.
               │
               └──> Plugin Forms (WinForms)
                    └──> Confluence Plugin (WPF!) ✓
```

### Target Architecture (Hybrid During Migration)

```
┌─────────────────────────────────────────┐
│       ModernUIManager (Selector)        │
│  if (UseModernUI)                       │
│      return WPF view                    │
│  else                                   │
│      return WinForms view               │
└──────────────┬──────────────────────────┘
               │
      ┌────────┴────────┐
      │                 │
      ▼                 ▼
  WPF Views      WinForms Views
  (Modern)         (Legacy)
      │                 │
      └────────┬────────┘
               │
               ▼
       Shared Business Logic
       - Capture
       - Destinations
       - Plugins
       - Configuration
```

### Final Architecture (Post-Migration)

```
┌─────────────────────────────────────────┐
│         Application (WPF)               │
│  - TaskbarIcon (WPF)                    │
│  - Navigation Service                   │
│  - Theme Manager                        │
└──────────────┬──────────────────────────┘
               │
               ├──> Modern Views (WPF)
               │    ├── AboutView
               │    ├── SettingsView
               │    ├── LanguageView
               │    └── ...
               │
               ├──> ViewModels (MVVM)
               │    ├── AboutViewModel
               │    ├── SettingsViewModel
               │    └── ...
               │
               ├──> Services
               │    ├── TranslationService
               │    ├── ThemeService
               │    ├── DialogService
               │    └── ...
               │
               └──> Business Logic (Unchanged)
                    ├── Capture
                    ├── Destinations
                    ├── Plugins
                    └── Configuration
```

## Translation System Implementation

### JSON Structure

**File**: `src/Greenshot.UI.Modern/Resources/Translations/en-US.json`

```json
{
  "$schema": "./translation-schema.json",
  "language": {
    "code": "en-US",
    "description": "English (United States)",
    "version": "2.0.0"
  },
  "about": {
    "title": "About Greenshot",
    "bugs": "Please report bugs to",
    "donations": "If you like Greenshot, you are welcome to support us:",
    "host": "Greenshot is hosted by GitHub at",
    "icons": "Icons from Yusuke Kamiyamane's Fugue icon set (Creative Commons Attribution 3.0 license)",
    "license": "Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom\nGreenshot comes with ABSOLUTELY NO WARRANTY...",
    "translation": ""
  },
  "contextmenu": {
    "about": "About Greenshot",
    "capturearea": "Capture region",
    "captureclipboard": "Open image from clipboard",
    "capturefullscreen": "Capture full screen",
    "capturefullscreen_all": "all",
    "capturefullscreen_bottom": "bottom",
    "capturefullscreen_left": "left",
    "capturefullscreen_right": "right",
    "capturefullscreen_top": "top",
    "captureie": "Capture Internet Explorer",
    "captureiefromlist": "Capture Internet Explorer from list",
    "capturelastregion": "Capture last region",
    "capturewindow": "Capture window",
    "capturewindowfromlist": "Capture window from list",
    "donate": "Support Greenshot",
    "exit": "Exit",
    "help": "Help",
    "openfile": "Open image from file",
    "openrecentcapture": "Open last capture location",
    "quicksettings": "Quick preferences",
    "settings": "Preferences..."
  },
  "settings": {
    "title": "Greenshot Settings",
    "general": "General",
    "capture": "Capture",
    "output": "Output",
    "printer": "Printer",
    "plugins": "Plugins",
    "expert": "Expert",
    "language": "Language:",
    "autostartshortcut": "Start Greenshot on Windows startup",
    "registerhotkeys": "Register hotkeys",
    "captureMousepointer": "Capture mouse pointer",
    "captureWindowsInteractive": "Use interactive window capture mode",
    "captureDelay": "Capture delay (ms):"
  },
  "common": {
    "ok": "OK",
    "cancel": "Cancel",
    "yes": "Yes",
    "no": "No",
    "apply": "Apply",
    "close": "Close",
    "save": "Save",
    "browse": "Browse..."
  },
  "errors": {
    "clipboard_error": "An unexpected error occurred while writing to the clipboard.",
    "clipboard_inuse": "Greenshot wasn't able to write to the clipboard as the process {0} blocked the access.",
    "clipboard_noimage": "Couldn't find a clipboard image.",
    "config_unauthorizedaccess_write": "Could not save Greenshot's configuration file. Please check access permissions for '{0}'.",
    "destination_exportfailed": "Error while exporting to {0}. Please try again.",
    "error_openlink": "Could not open link: {0}"
  }
}
```

### JSON Schema for Validation

**File**: `src/Greenshot.UI.Modern/Resources/Translations/translation-schema.json`

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Greenshot Translation File",
  "description": "Schema for Greenshot translation JSON files",
  "type": "object",
  "required": ["language"],
  "properties": {
    "$schema": {
      "type": "string",
      "description": "Schema reference"
    },
    "language": {
      "type": "object",
      "required": ["code", "description", "version"],
      "properties": {
        "code": {
          "type": "string",
          "pattern": "^[a-z]{2,3}-[A-Z]{2}$",
          "description": "IETF language code (e.g., en-US, de-DE)"
        },
        "description": {
          "type": "string",
          "description": "Human-readable language name"
        },
        "version": {
          "type": "string",
          "pattern": "^\\d+\\.\\d+\\.\\d+$",
          "description": "Translation version (semver)"
        }
      }
    }
  },
  "additionalProperties": {
    "type": "object",
    "additionalProperties": {
      "type": "string"
    }
  }
}
```

### Translation Service Implementation

**File**: `src/Greenshot.UI.Modern/Services/TranslationService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Greenshot.UI.Modern.Services
{
    public interface ITranslationService
    {
        string Translate(string key);
        string Translate(string key, params object[] args);
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
        IEnumerable<LanguageInfo> AvailableLanguages { get; }
    }

    public class TranslationService : ITranslationService
    {
        private Dictionary<string, string> _translations = new Dictionary<string, string>();
        private string _currentLanguage = "en-US";
        private readonly string _translationsPath;
        private static readonly Regex KeyPathRegex = new Regex(@"^([a-z]+)\.([a-z_]+)$", RegexOptions.Compiled);

        public TranslationService()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var assemblyPath = Path.GetDirectoryName(assembly.Location);
            _translationsPath = Path.Combine(assemblyPath, "Resources", "Translations");
            
            LoadLanguage(_currentLanguage);
        }

        public string CurrentLanguage => _currentLanguage;

        public IEnumerable<LanguageInfo> AvailableLanguages
        {
            get
            {
                if (!Directory.Exists(_translationsPath))
                    yield break;

                foreach (var file in Directory.GetFiles(_translationsPath, "*.json"))
                {
                    var langCode = Path.GetFileNameWithoutExtension(file);
                    var content = File.ReadAllText(file);
                    
                    using (var doc = JsonDocument.Parse(content))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("language", out var langObj))
                        {
                            var code = langObj.GetProperty("code").GetString();
                            var description = langObj.GetProperty("description").GetString();
                            yield return new LanguageInfo(code, description);
                        }
                    }
                }
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (_currentLanguage == languageCode)
                return;

            if (LoadLanguage(languageCode))
            {
                _currentLanguage = languageCode;
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler LanguageChanged;

        private bool LoadLanguage(string languageCode)
        {
            var filePath = Path.Combine(_translationsPath, $"{languageCode}.json");
            
            if (!File.Exists(filePath))
            {
                // Fallback to en-US
                if (languageCode != "en-US")
                    return LoadLanguage("en-US");
                return false;
            }

            try
            {
                var content = File.ReadAllText(filePath);
                var translations = new Dictionary<string, string>();
                
                using (var doc = JsonDocument.Parse(content))
                {
                    var root = doc.RootElement;
                    
                    // Flatten the nested structure
                    foreach (var section in root.EnumerateObject())
                    {
                        if (section.Name == "$schema" || section.Name == "language")
                            continue;

                        foreach (var item in section.Value.EnumerateObject())
                        {
                            var key = $"{section.Name}.{item.Name}";
                            translations[key] = item.Value.GetString();
                        }
                    }
                }

                _translations = translations;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load language {languageCode}: {ex.Message}");
                return false;
            }
        }

        public string Translate(string key)
        {
            if (_translations.TryGetValue(key, out var value))
                return value;

            // Return the key itself as fallback
            return $"[{key}]";
        }

        public string Translate(string key, params object[] args)
        {
            var template = Translate(key);
            
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
    }

    public record LanguageInfo(string Code, string Description);
}
```

### WPF Markup Extension for Translations

**File**: `src/Greenshot.UI.Modern/MarkupExtensions/TranslateExtension.cs`

```csharp
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Greenshot.UI.Modern.Services;

namespace Greenshot.UI.Modern.MarkupExtensions
{
    /// <summary>
    /// XAML markup extension for translations
    /// Usage: {local:Translate about.title}
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class TranslateExtension : MarkupExtension
    {
        private static ITranslationService _translationService;

        public static void Initialize(ITranslationService translationService)
        {
            _translationService = translationService;
            _translationService.LanguageChanged += (s, e) =>
            {
                // Trigger UI refresh when language changes
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    // This will cause all bindings to re-evaluate
                    LanguageChangedNotifier.Instance.OnLanguageChanged();
                });
            };
        }

        public string Key { get; set; }

        public TranslateExtension()
        {
        }

        public TranslateExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return "[No Key]";

            if (_translationService == null)
                return $"[{Key}]";

            // For design-time support
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(
                new DependencyObject()))
            {
                return $"[{Key}]";
            }

            // Create a binding that updates when language changes
            var binding = new Binding
            {
                Source = LanguageChangedNotifier.Instance,
                Path = new PropertyPath(nameof(LanguageChangedNotifier.Trigger)),
                Converter = new TranslationConverter(Key, _translationService)
            };

            var targetProvider = serviceProvider.GetService(typeof(IProvideValueTarget)) 
                as IProvideValueTarget;

            if (targetProvider?.TargetObject is DependencyObject)
            {
                return binding.ProvideValue(serviceProvider);
            }

            // Fallback for non-dependency properties
            return _translationService.Translate(Key);
        }
    }

    /// <summary>
    /// Notifies UI when language changes
    /// </summary>
    public class LanguageChangedNotifier : DependencyObject
    {
        private static readonly Lazy<LanguageChangedNotifier> _instance =
            new Lazy<LanguageChangedNotifier>(() => new LanguageChangedNotifier());

        public static LanguageChangedNotifier Instance => _instance.Value;

        public static readonly DependencyProperty TriggerProperty =
            DependencyProperty.Register(
                nameof(Trigger),
                typeof(int),
                typeof(LanguageChangedNotifier),
                new PropertyMetadata(0));

        public int Trigger
        {
            get => (int)GetValue(TriggerProperty);
            set => SetValue(TriggerProperty, value);
        }

        public void OnLanguageChanged()
        {
            Trigger++;
        }
    }

    /// <summary>
    /// Converter that translates the key
    /// </summary>
    public class TranslationConverter : IValueConverter
    {
        private readonly string _key;
        private readonly ITranslationService _translationService;

        public TranslationConverter(string key, ITranslationService translationService)
        {
            _key = key;
            _translationService = translationService;
        }

        public object Convert(object value, Type targetType, object parameter, 
            System.Globalization.CultureInfo culture)
        {
            return _translationService?.Translate(_key) ?? $"[{_key}]";
        }

        public object ConvertBack(object value, Type targetType, object parameter, 
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
```

### Usage in XAML

```xaml
<Window x:Class="Greenshot.UI.Modern.Views.AboutView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:Greenshot.UI.Modern.MarkupExtensions"
        Title="{local:Translate about.title}">
    
    <StackPanel>
        <TextBlock Text="{local:Translate about.bugs}"/>
        <TextBlock Text="{local:Translate about.donations}"/>
        <Button Content="{local:Translate common.close}" Click="Close_Click"/>
    </StackPanel>
</Window>
```

## MVVM Pattern Reference

### Base ViewModel

**File**: `src/Greenshot.UI.Modern/ViewModels/ViewModelBase.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Greenshot.UI.Modern.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, 
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
```

### Relay Command

**File**: `src/Greenshot.UI.Modern/Commands/RelayCommand.cs`

```csharp
using System;
using System.Windows.Input;

namespace Greenshot.UI.Modern.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}
```

### Example ViewModel: AboutViewModel

**File**: `src/Greenshot.UI.Modern/ViewModels/AboutViewModel.cs`

```csharp
using System;
using System.Diagnostics;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.UI.Modern.Commands;

namespace Greenshot.UI.Modern.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        private string _versionText;

        public AboutViewModel()
        {
            VersionText = $"Greenshot {EnvironmentInfo.GetGreenshotVersion()} " +
                         $"{(IniConfig.IsPortable ? "Portable" : "")} ({OsInfo.Bits} bit)";

            OpenUrlCommand = new RelayCommand<string>(OpenUrl, CanOpenUrl);
            CloseCommand = new RelayCommand(Close);
        }

        public string VersionText
        {
            get => _versionText;
            set => SetProperty(ref _versionText, value);
        }

        public ICommand OpenUrlCommand { get; }
        public ICommand CloseCommand { get; }

        public event EventHandler CloseRequested;

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Failed to open URL {url}: {ex.Message}");
            }
        }

        private bool CanOpenUrl(string url)
        {
            return !string.IsNullOrEmpty(url) && 
                   (url.StartsWith("http://") || url.StartsWith("https://"));
        }

        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
```

### Example View: AboutView

**File**: `src/Greenshot.UI.Modern/Views/AboutView.xaml`

```xaml
<Window x:Class="Greenshot.UI.Modern.Views.AboutView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:Greenshot.UI.Modern.MarkupExtensions"
        xmlns:vm="clr-namespace:Greenshot.UI.Modern.ViewModels"
        Title="{local:Translate about.title}" 
        SizeToContent="WidthAndHeight"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen"
        Background="#FF3D3D3D"
        WindowStyle="SingleBorderWindow">
    
    <Window.DataContext>
        <vm:AboutViewModel/>
    </Window.DataContext>
    
    <Window.Resources>
        <!-- Styles -->
        <Style TargetType="TextBlock">
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="Margin" Value="0,5"/>
        </Style>
        
        <Style TargetType="Hyperlink">
            <Setter Property="Foreground" Value="#FF8AFF00"/>
            <Setter Property="TextDecorations" Value="None"/>
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="TextDecorations" Value="Underline"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
    
    <Grid Margin="30,20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Logo -->
        <Viewbox Grid.Row="0" Width="90" Height="90" Margin="0,0,0,20">
            <Canvas Width="90" Height="90">
                <!-- Animated G logo will be drawn in code-behind -->
                <Image x:Name="LogoImage" Width="90" Height="90"/>
            </Canvas>
        </Viewbox>
        
        <!-- Version -->
        <TextBlock Grid.Row="1" 
                   Text="{Binding VersionText}"
                   FontSize="18" 
                   FontWeight="SemiBold"
                   TextAlignment="Center"
                   Margin="0,0,0,20"/>
        
        <!-- Links -->
        <StackPanel Grid.Row="2" Margin="0,0,0,10">
            <TextBlock>
                <Run Text="{local:Translate about.host}"/>
            </TextBlock>
            <TextBlock>
                <Hyperlink NavigateUri="https://getgreenshot.org/" 
                          Command="{Binding OpenUrlCommand}"
                          CommandParameter="https://getgreenshot.org/">
                    https://getgreenshot.org/
                </Hyperlink>
            </TextBlock>
            
            <TextBlock Margin="0,10,0,0">
                <Run Text="{local:Translate about.bugs}"/>
            </TextBlock>
            <TextBlock>
                <Hyperlink NavigateUri="https://github.com/greenshot/greenshot/issues" 
                          Command="{Binding OpenUrlCommand}"
                          CommandParameter="https://github.com/greenshot/greenshot/issues">
                    https://github.com/greenshot/greenshot/issues
                </Hyperlink>
            </TextBlock>
            
            <TextBlock Margin="0,10,0,0">
                <Run Text="{local:Translate about.donations}"/>
            </TextBlock>
            <TextBlock>
                <Hyperlink NavigateUri="https://getgreenshot.org/support/" 
                          Command="{Binding OpenUrlCommand}"
                          CommandParameter="https://getgreenshot.org/support/">
                    https://getgreenshot.org/support/
                </Hyperlink>
            </TextBlock>
        </StackPanel>
        
        <!-- License -->
        <TextBlock Grid.Row="3" 
                   Text="{local:Translate about.license}"
                   TextWrapping="Wrap"
                   Foreground="LightGray"
                   FontSize="10"
                   MaxWidth="400"
                   Margin="0,10,0,20"/>
        
        <!-- Close Button -->
        <Button Grid.Row="4" 
                Content="{local:Translate common.close}"
                Command="{Binding CloseCommand}"
                IsDefault="True"
                Padding="30,8"
                HorizontalAlignment="Center"
                Style="{StaticResource ModernButtonStyle}"/>
    </Grid>
</Window>
```

**File**: `src/Greenshot.UI.Modern/Views/AboutView.xaml.cs`

```csharp
using System;
using System.Windows;
using Greenshot.UI.Modern.ViewModels;

namespace Greenshot.UI.Modern.Views
{
    public partial class AboutView : Window
    {
        public AboutView()
        {
            InitializeComponent();
            
            var viewModel = DataContext as AboutViewModel;
            if (viewModel != null)
            {
                viewModel.CloseRequested += (s, e) => Close();
            }
            
            // Initialize logo animation
            InitializeLogo();
        }

        private void InitializeLogo()
        {
            // Port the animated G logo from AboutForm.cs
            // This can use WPF animations or continue to use GDI+ drawing
            // For simplicity, can initially use a static image
        }
    }
}
```

## WPF Conversion Examples

### Example 1: Simple Dialog - Language Selector

**Before (WinForms)**: `LanguageDialog.cs`

```csharp
public partial class LanguageDialog : BaseForm
{
    private void InitializeComponent()
    {
        this.listBox_languages = new System.Windows.Forms.ListBox();
        this.button_ok = new System.Windows.Forms.Button();
        this.button_cancel = new System.Windows.Forms.Button();
        // ... WinForms setup code ...
    }
    
    private void PopulateLanguages()
    {
        foreach (var lang in Language.SupportedLanguages)
        {
            listBox_languages.Items.Add(lang);
        }
    }
}
```

**After (WPF)**: `LanguageView.xaml`

```xaml
<Window x:Class="Greenshot.UI.Modern.Views.LanguageView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:Greenshot.UI.Modern.MarkupExtensions"
        Title="{local:Translate settings.language}" 
        Width="400" Height="500"
        WindowStartupLocation="CenterScreen">
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Search Box -->
        <TextBox Grid.Row="0" 
                 Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,10">
            <TextBox.Style>
                <Style TargetType="TextBox">
                    <Setter Property="Foreground" Value="Gray"/>
                    <Style.Triggers>
                        <Trigger Property="IsFocused" Value="True">
                            <Setter Property="Foreground" Value="Black"/>
                        </Trigger>
                    </Style.Triggers>
                </Style>
            </TextBox.Style>
        </TextBox>
        
        <!-- Languages List -->
        <ListBox Grid.Row="1" 
                 ItemsSource="{Binding FilteredLanguages}"
                 SelectedItem="{Binding SelectedLanguage}"
                 DisplayMemberPath="Description"
                 IsSynchronizedWithCurrentItem="True"/>
        
        <!-- Buttons -->
        <StackPanel Grid.Row="2" 
                    Orientation="Horizontal" 
                    HorizontalAlignment="Right"
                    Margin="0,10,0,0">
            <Button Content="{local:Translate common.ok}"
                    Command="{Binding OkCommand}"
                    IsDefault="True"
                    Padding="20,5"
                    Margin="0,0,10,0"/>
            <Button Content="{local:Translate common.cancel}"
                    Command="{Binding CancelCommand}"
                    IsCancel="True"
                    Padding="20,5"/>
        </StackPanel>
    </Grid>
</Window>
```

### Example 2: Settings with Tabs

**Before (WinForms)**: `SettingsForm.cs` with TabControl

```csharp
private void InitializeComponent()
{
    this.tabControl = new System.Windows.Forms.TabControl();
    this.tabPageGeneral = new System.Windows.Forms.TabPage();
    this.tabPageCapture = new System.Windows.Forms.TabPage();
    // ... hundreds of control initializations ...
}
```

**After (WPF)**: `SettingsWindow.xaml`

```xaml
<Window x:Class="Greenshot.UI.Modern.Views.Settings.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:Greenshot.UI.Modern.MarkupExtensions"
        xmlns:views="clr-namespace:Greenshot.UI.Modern.Views.Settings"
        Title="{local:Translate settings.title}" 
        Width="700" Height="600">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Tab Control -->
        <TabControl Grid.Row="0" Margin="10">
            <TabItem Header="{local:Translate settings.general}">
                <views:GeneralTab DataContext="{Binding GeneralViewModel}"/>
            </TabItem>
            <TabItem Header="{local:Translate settings.capture}">
                <views:CaptureTab DataContext="{Binding CaptureViewModel}"/>
            </TabItem>
            <TabItem Header="{local:Translate settings.output}">
                <views:OutputTab DataContext="{Binding OutputViewModel}"/>
            </TabItem>
            <TabItem Header="{local:Translate settings.printer}">
                <views:PrinterTab DataContext="{Binding PrinterViewModel}"/>
            </TabItem>
            <TabItem Header="{local:Translate settings.plugins}">
                <views:PluginsTab DataContext="{Binding PluginsViewModel}"/>
            </TabItem>
            <TabItem Header="{local:Translate settings.expert}">
                <views:ExpertTab DataContext="{Binding ExpertViewModel}"/>
            </TabItem>
        </TabControl>
        
        <!-- Buttons -->
        <StackPanel Grid.Row="1" 
                    Orientation="Horizontal" 
                    HorizontalAlignment="Right"
                    Margin="10">
            <Button Content="{local:Translate common.ok}"
                    Command="{Binding SaveCommand}"
                    IsDefault="True"
                    Padding="20,5"
                    Margin="0,0,10,0"/>
            <Button Content="{local:Translate common.cancel}"
                    Command="{Binding CancelCommand}"
                    IsCancel="True"
                    Padding="20,5"/>
        </StackPanel>
    </Grid>
</Window>
```

**Individual Tab**: `GeneralTab.xaml`

```xaml
<UserControl x:Class="Greenshot.UI.Modern.Views.Settings.GeneralTab"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:Greenshot.UI.Modern.MarkupExtensions">
    
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="10">
            <!-- Language -->
            <Label Content="{local:Translate settings.language}"/>
            <ComboBox ItemsSource="{Binding AvailableLanguages}"
                      SelectedItem="{Binding SelectedLanguage}"
                      DisplayMemberPath="Description"
                      Margin="0,0,0,10"/>
            
            <!-- Startup -->
            <CheckBox Content="{local:Translate settings.autostartshortcut}"
                      IsChecked="{Binding AutoStartEnabled}"
                      Margin="0,5"/>
            
            <!-- Register Hotkeys -->
            <CheckBox Content="{local:Translate settings.registerhotkeys}"
                      IsChecked="{Binding RegisterHotkeys}"
                      Margin="0,5"/>
            
            <!-- More settings... -->
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

## Build System Integration

### MSBuild Task for JSON to RESX Conversion

**File**: `src/Greenshot.UI.Modern/Build/GenerateResxFromJson.targets`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  
  <UsingTask TaskName="JsonToResxTask" 
             TaskFactory="RoslynCodeTaskFactory" 
             AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll">
    <ParameterGroup>
      <JsonFile ParameterType="System.String" Required="true"/>
      <OutputFile ParameterType="System.String" Required="true"/>
    </ParameterGroup>
    <Task>
      <Using Namespace="System"/>
      <Using Namespace="System.IO"/>
      <Using Namespace="System.Text.Json"/>
      <Using Namespace="System.Resources"/>
      <Using Namespace="System.Collections"/>
      <Code Type="Fragment" Language="cs">
        <![CDATA[
          try
          {
              var jsonContent = File.ReadAllText(JsonFile);
              using var doc = JsonDocument.Parse(jsonContent);
              var root = doc.RootElement;
              
              using var writer = new ResXResourceWriter(OutputFile);
              
              foreach (var section in root.EnumerateObject())
              {
                  if (section.Name == "$schema" || section.Name == "language")
                      continue;
                  
                  foreach (var item in section.Value.EnumerateObject())
                  {
                      var key = $"{section.Name}.{item.Name}";
                      var value = item.Value.GetString();
                      writer.AddResource(key, value);
                  }
              }
              
              writer.Generate();
              Log.LogMessage($"Generated {OutputFile} from {JsonFile}");
              return true;
          }
          catch (Exception ex)
          {
              Log.LogError($"Failed to convert {JsonFile}: {ex.Message}");
              return false;
          }
        ]]>
      </Code>
    </Task>
  </UsingTask>
  
  <Target Name="GenerateResxFiles" BeforeTargets="CoreCompile">
    <ItemGroup>
      <JsonTranslations Include="$(ProjectDir)Resources\Translations\*.json"/>
    </ItemGroup>
    
    <JsonToResxTask JsonFile="%(JsonTranslations.FullPath)" 
                    OutputFile="$(IntermediateOutputPath)Resources.%(JsonTranslations.Filename).resx"/>
    
    <ItemGroup>
      <EmbeddedResource Include="$(IntermediateOutputPath)Resources.*.resx"/>
    </ItemGroup>
  </Target>
  
</Project>
```

### Project File Integration

**File**: `src/Greenshot.UI.Modern/Greenshot.UI.Modern.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <UseWPF>true</UseWPF>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  
  <!-- Import the custom build task -->
  <Import Project="Build\GenerateResxFromJson.targets"/>
  
  <ItemGroup>
    <!-- JSON translation files -->
    <None Include="Resources\Translations\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0"/>
    <PackageReference Include="System.Text.Json" Version="6.0.0"/>
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\Greenshot.Base\Greenshot.Base.csproj"/>
  </ItemGroup>
  
</Project>
```

## Testing Strategy

### Unit Tests for ViewModels

**File**: `src/Greenshot.UI.Modern.Tests/ViewModels/AboutViewModelTests.cs`

```csharp
using Xunit;
using Greenshot.UI.Modern.ViewModels;

namespace Greenshot.UI.Modern.Tests.ViewModels
{
    public class AboutViewModelTests
    {
        [Fact]
        public void VersionText_IsNotNullOrEmpty()
        {
            // Arrange
            var viewModel = new AboutViewModel();
            
            // Act & Assert
            Assert.False(string.IsNullOrEmpty(viewModel.VersionText));
            Assert.Contains("Greenshot", viewModel.VersionText);
        }
        
        [Fact]
        public void CloseCommand_RaisesCloseRequested()
        {
            // Arrange
            var viewModel = new AboutViewModel();
            bool eventRaised = false;
            viewModel.CloseRequested += (s, e) => eventRaised = true;
            
            // Act
            viewModel.CloseCommand.Execute(null);
            
            // Assert
            Assert.True(eventRaised);
        }
    }
}
```

### Integration Tests for Translation Service

**File**: `src/Greenshot.UI.Modern.Tests/Services/TranslationServiceTests.cs`

```csharp
using Xunit;
using Greenshot.UI.Modern.Services;
using System.IO;
using System.Text.Json;

namespace Greenshot.UI.Modern.Tests.Services
{
    public class TranslationServiceTests
    {
        [Fact]
        public void Translate_ReturnsCorrectString_ForEnglish()
        {
            // Arrange
            var service = CreateServiceWithTestData();
            
            // Act
            var result = service.Translate("about.title");
            
            // Assert
            Assert.Equal("About Greenshot", result);
        }
        
        [Fact]
        public void Translate_ReturnsFallback_ForMissingKey()
        {
            // Arrange
            var service = CreateServiceWithTestData();
            
            // Act
            var result = service.Translate("nonexistent.key");
            
            // Assert
            Assert.Equal("[nonexistent.key]", result);
        }
        
        [Fact]
        public void SetLanguage_ChangesCurrentLanguage()
        {
            // Arrange
            var service = CreateServiceWithTestData();
            
            // Act
            service.SetLanguage("de-DE");
            
            // Assert
            Assert.Equal("de-DE", service.CurrentLanguage);
        }
        
        private ITranslationService CreateServiceWithTestData()
        {
            // Create test translation files
            // Return configured service
            return new TranslationService();
        }
    }
}
```

### UI Automation Tests

**File**: `src/Greenshot.UI.Modern.Tests/UI/AboutViewUITests.cs`

```csharp
using Xunit;
using System.Windows;
using System.Windows.Automation;
using Greenshot.UI.Modern.Views;

namespace Greenshot.UI.Modern.Tests.UI
{
    public class AboutViewUITests
    {
        [WpfFact]
        public void AboutView_OpensSuccessfully()
        {
            // Arrange
            var view = new AboutView();
            
            // Act
            view.Show();
            
            // Assert
            Assert.True(view.IsVisible);
            
            // Cleanup
            view.Close();
        }
        
        [WpfFact]
        public void AboutView_CloseButton_ClosesWindow()
        {
            // Arrange
            var view = new AboutView();
            view.Show();
            
            // Act
            // Find and click close button via automation
            var closeButton = FindButton(view, "common.close");
            closeButton?.Invoke();
            
            // Assert
            Assert.False(view.IsVisible);
        }
        
        private AutomationElement FindButton(Window window, string translationKey)
        {
            // Implementation using UI Automation
            return null;
        }
    }
    
    // Custom fact attribute for WPF tests
    public class WpfFactAttribute : FactAttribute
    {
        public WpfFactAttribute()
        {
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
        }
    }
}
```

## Appendix: File Structure

```
src/
├── Greenshot/                          (Existing - WinForms)
│   ├── Forms/
│   ├── Languages/
│   └── ...
│
├── Greenshot.UI.Modern/                (New - WPF)
│   ├── Greenshot.UI.Modern.csproj
│   │
│   ├── Build/
│   │   └── GenerateResxFromJson.targets
│   │
│   ├── Commands/
│   │   ├── RelayCommand.cs
│   │   └── AsyncRelayCommand.cs
│   │
│   ├── Converters/
│   │   ├── BoolToVisibilityConverter.cs
│   │   └── InvertBooleanConverter.cs
│   │
│   ├── MarkupExtensions/
│   │   └── TranslateExtension.cs
│   │
│   ├── Resources/
│   │   ├── Styles/
│   │   │   ├── ButtonStyles.xaml
│   │   │   ├── TextBoxStyles.xaml
│   │   │   └── Colors.xaml
│   │   │
│   │   └── Translations/
│   │       ├── translation-schema.json
│   │       ├── en-US.json
│   │       ├── de-DE.json
│   │       └── ... (25+ languages)
│   │
│   ├── Services/
│   │   ├── ITranslationService.cs
│   │   ├── TranslationService.cs
│   │   ├── IDialogService.cs
│   │   ├── DialogService.cs
│   │   └── ThemeService.cs
│   │
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs
│   │   ├── AboutViewModel.cs
│   │   ├── LanguageViewModel.cs
│   │   │
│   │   └── Settings/
│   │       ├── SettingsViewModel.cs
│   │       ├── GeneralTabViewModel.cs
│   │       ├── CaptureTabViewModel.cs
│   │       └── ... (one per tab)
│   │
│   ├── Views/
│   │   ├── AboutView.xaml
│   │   ├── AboutView.xaml.cs
│   │   ├── LanguageView.xaml
│   │   ├── LanguageView.xaml.cs
│   │   ├── SystemTrayView.xaml
│   │   │
│   │   └── Settings/
│   │       ├── SettingsWindow.xaml
│   │       ├── SettingsWindow.xaml.cs
│   │       ├── GeneralTab.xaml
│   │       ├── CaptureTab.xaml
│   │       └── ... (one per tab)
│   │
│   └── Core/
│       └── ModernUIManager.cs
│
├── Greenshot.UI.Modern.Tests/          (New - Tests)
│   ├── ViewModels/
│   ├── Services/
│   └── UI/
│
└── Tools/
    └── XmlToJsonTranslation/           (New - Migration Tool)
        ├── Program.cs
        └── XmlToJsonConverter.cs
```

---

**Document Version**: 1.0  
**Companion to**: ui-migration-concept.md  
**Date**: January 2026
