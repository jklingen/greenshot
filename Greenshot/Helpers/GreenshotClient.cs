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

using System.Collections.Generic;
using System.ServiceModel;
using Greenshot.Addon.Interfaces;
using Greenshot.Core.Interfaces;
using Greenshot.Services;

#endregion

namespace Greenshot.Helpers
{
	/// <summary>
	///     A simple helper to talk to an already running Greenshot 1.3+ instance
	/// </summary>
	public class GreenshotClient
	{
		private static ChannelFactory<IGreenshotContract> ChannelFactory
		{
			get { return new ChannelFactory<IGreenshotContract>(new NetNamedPipeBinding(), new EndpointAddress(GreenshotServer.EndPoint)); }
		}

		public static void Exit()
		{
			using (var factory = ChannelFactory)
			{
				var client = factory.CreateChannel();
				client.Exit();
			}
		}

		public static void OpenFiles(IList<string> filesToOpen)
		{
			if ((filesToOpen == null) || (filesToOpen.Count == 0))
			{
				return;
			}
			using (var factory = ChannelFactory)
			{
				var client = factory.CreateChannel();
				foreach (string filename in filesToOpen)
				{
					client.OpenFile(filename);
				}
			}
		}
	}
}