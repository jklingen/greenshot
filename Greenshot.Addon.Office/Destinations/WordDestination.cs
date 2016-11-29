﻿//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Extensions;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;
using MahApps.Metro.IconPacks;

#endregion

namespace Greenshot.Addon.Office.Destinations
{
	/// <summary>
	///     Description of WordDestination.
	/// </summary>
	[Destination(WordDesignation)]
	[PartNotDiscoverable]
	public sealed class WordDestination : AbstractDestination
	{
		public const string WordDesignation = "Word";
		private static readonly LogSource Log = new LogSource();
		private static readonly BitmapSource DocumentIcon;
		private static readonly BitmapSource ApplicationIcon;

		static WordDestination()
		{
			var exePath = PathHelper.GetExePath("WINWORD.EXE");
			if ((exePath != null) && File.Exists(exePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("WINWORD");
				DocumentIcon = IconHelper.GetCachedExeIcon(exePath, 1).ToBitmapSource();
				ApplicationIcon = IconHelper.GetCachedExeIcon(exePath, 0).ToBitmapSource();
				IsActive = true;
			}
		}

		[Import]
		private IGreenshotLanguage GreenshotLanguage { get; set; }

		/// <summary>
		///     Tells if the destination can be used
		/// </summary>
		public static bool IsActive { get; private set; }

		[Import]
		private IOfficeConfiguration OfficeConfiguration { get; set; }

		private Task<INotification> ExportCaptureAsync(ICapture capture, string documentCaption)
		{
			INotification returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = WordDesignation,
				NotificationSourceType = NotificationSourceTypes.Destination,
				Text = $"Exported to {WordDesignation}"
			};
			string tmpFile = capture.CaptureDetails.Filename;
			if ((tmpFile == null) || capture.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = capture.SaveNamedTmpFile(capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			try
			{
				if (documentCaption != null)
				{
					try
					{
						WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
					}
					catch (Exception)
					{
						// Retry once, just in case
						WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
					}
				}
				else
				{
					try
					{
						WordExporter.InsertIntoNewDocument(tmpFile, null, null);
					}
					catch (Exception)
					{
						// Retry once, just in case
						WordExporter.InsertIntoNewDocument(tmpFile, null, null);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Error exporting image to Word");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = ex.Message;
				returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, WordDesignation);
				return Task.FromResult(returnValue);
			}
			return Task.FromResult(returnValue);
		}

		protected override void Initialize()
		{
			base.Initialize();
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null);
			Text = Text = $"Export to {WordDesignation}";
			Designation = WordDesignation;
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.OfficeWord
			};
		}

		/// <summary>
		///     Load the current documents to export to
		/// </summary>
		/// <param name="caller1"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override async Task RefreshAsync(IExportContext caller1, CancellationToken token = default(CancellationToken))
		{
			Children.Clear();
			await Task.Run(() =>
			{
				return WordExporter.GetWordDocuments().OrderBy(x => x).Select(caption => new WordDestination
				{
					Icon = new PackIconModern
					{
						Kind = PackIconModernKind.PageWord
					},
					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, caption),
					Text = caption,
					OfficeConfiguration = OfficeConfiguration,
					GreenshotLanguage = GreenshotLanguage
				}).ToList();
			}, token).ContinueWith(async destinations =>
			{
				foreach (var caption in await destinations)
				{
					Children.Add(caption);
				}
			}, token, TaskContinuationOptions.None, UiContext.UiTaskScheduler).ConfigureAwait(false);
		}
	}
}