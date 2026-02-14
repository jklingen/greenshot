---
layout: post
status: publish
published: true
release_version: 1.4.107
title: "Greenshot 1.4 Development Builds: New Features and Improvements"
tags:
- windows
- 1.4
- development
- release
- continuous-builds
---

We're excited to share the latest developments in Greenshot 1.4! Since November 2025, we've been hard at work on the next major version of Greenshot, and we're now making continuous development builds available for testing.

**⚠️ Important Note**: Greenshot 1.4 is currently in active development. All builds are **continuous/development builds** and are **UNSTABLE-UNSIGNED**. These are not production-ready releases and should be considered beta quality. For production use, please continue to use the [latest stable 1.3 release](https://github.com/greenshot/greenshot/releases).

## What's New in Greenshot 1.4?

The 1.4 development cycle has brought some exciting new features and significant improvements to Greenshot. Here are the highlights:

### 🎨 Editor Enhancements

**Emoji Support** – You can now add emojis directly to your screenshots! Thanks to contributor [@jairbubbles](https://github.com/jairbubbles), Greenshot's editor now includes a new emoji object type, making it easier to add expressive reactions and annotations to your captures.

**OCR-Based Text Redaction (Beta)** – Privacy just got easier! This new experimental feature uses OCR technology to automatically detect and redact sensitive text in your screenshots. Whether you're sharing screenshots with colleagues or posting online, you can now quickly obscure passwords, email addresses, or other sensitive information. This beta feature is a game-changer for privacy-conscious users.

**Remove Transparency** – Need to export images without transparent backgrounds? The new "Remove Transparency" feature makes it simple to convert transparent areas to solid backgrounds, perfect for certain export formats and use cases.

### 📸 Capture Improvements

**New Capture Technology** – We've implemented a completely new capture engine that provides better screenshot quality and improved compatibility with modern Windows applications. This means more reliable captures across a wider range of apps.

**Enhanced Cursor Capture** – Cursor capture now reflects the actual cursor size, giving you more accurate representations in your screenshots.

**Better Windows 10/11 Integration** – The Windows 10 plugin has been integrated directly into the main codebase, providing better native integration with modern Windows versions. We've also improved compatibility with Windows 11's capture behavior and enhanced the Windows Share functionality.

### 🔌 Plugin Modernization

We've modernized several plugins to use current APIs and authentication methods:

- **Confluence Plugin**: Migrated from the deprecated SOAP API to the modern REST API
- **Dropbox Plugin**: Updated OAuth2 flow from the deprecated implicit flow to the more secure code flow
- **JIRA Plugin**: Various fixes and improvements
- **OneNote Plugin**: Fixed COM instantiation issues on modern Windows
- **Outlook Plugin**: Better compatibility with New Outlook

### 🛠️ Quality Improvements

One of our major focuses in 1.4 has been stability and memory management. Thanks to extensive contributions from [@danrhodes](https://github.com/danrhodes) and others, we've fixed numerous memory leaks and GDI handle leaks that could cause performance degradation over time. We've also resolved several drawing issues, including artifacts that appeared at certain zoom levels.

### 🌍 Internationalization

Greenshot 1.4 includes updated translations for Turkish, Swedish, Polish, Japanese, and Persian/Farsi. We've also introduced a custom translation agent to improve our localization workflow, making it easier to keep Greenshot accessible in many languages.

## How to Get 1.4 Development Builds

Continuous builds are automatically created for every commit to the `main` branch:

1. Visit the [GitHub Releases page](https://github.com/greenshot/greenshot/releases)
2. Look for releases tagged with version numbers like `v1.4.X-gXXXXXXXXXX (continuous build)`
3. Download the installer or portable ZIP marked as **"UNSTABLE-UNSIGNED"**

**Important**: Since these builds are unsigned, you may see Windows SmartScreen warnings. This is expected for continuous development builds. Always download Greenshot only from our official sources: [getgreenshot.org](https://getgreenshot.org) or the official [GitHub repository](https://github.com/greenshot/greenshot/releases).

## We Need Your Help!

As these are development builds, we need beta testers to help us identify issues and improve Greenshot. If you're interested in testing:

1. **Backup your configuration**: Before installing, back up your settings in `%APPDATA%\Greenshot`
2. **Uninstall 1.3**: Completely uninstall any stable 1.3 release first
3. **Install and test**: Download the latest 1.4 build and test your usual workflows
4. **Report issues**: Found a bug? [Report it on GitHub](https://github.com/greenshot/greenshot/issues)
5. **Share feedback**: Let us know what you think!

## Thank You to Our Contributors!

Greenshot 1.4 wouldn't be possible without the amazing contributions from our community. Special thanks to:

- [@Lakritzator](https://github.com/Lakritzator) – Major features and core improvements
- [@danrhodes](https://github.com/danrhodes) – Extensive memory leak fixes and quality improvements
- [@jairbubbles](https://github.com/jairbubbles) – Emoji support
- [@Christian-Schulz](https://github.com/Christian-Schulz) – Bug fixes and improvements
- [@Copilot](https://github.com/features/copilot) – AI-assisted improvements
- And many more contributors who have helped with translations, bug fixes, and testing!

We also welcome several new contributors who made their first contributions during the 1.4 development cycle. Thank you all for making Greenshot better!

## What's Next?

Development on 1.4 is ongoing, and we'll continue to add features, fix bugs, and improve stability. Once we're confident in the quality and stability, we'll work toward a stable 1.4 release. In the meantime, continuous builds will keep coming with improvements and fixes.

For a detailed technical changelog including all changes and commit history, check out the [full CHANGELOG-1.4.md](https://github.com/greenshot/greenshot/blob/main/docs/changelogs/CHANGELOG-1.4.md) in our repository.

## Stay Safe!

As always, remember to download Greenshot only from official sources:
- Official website: [getgreenshot.org](https://getgreenshot.org)
- GitHub releases: [github.com/greenshot/greenshot/releases](https://github.com/greenshot/greenshot/releases)

Beware of fake websites and modified installers that may contain malware or unwanted software.

Happy screenshotting (or should we say, happy Greenshotting)! 📸

*Your Greenshot Team*

---

*Latest continuous build as of this post: v1.4.107 (February 14, 2026)*
