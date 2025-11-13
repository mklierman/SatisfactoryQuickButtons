using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace SatisfactoryQuickButtons
{
	/// <summary>
	/// Options page for Satisfactory Quick Buttons extension
	/// </summary>
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[ComVisible(true)]
	[Guid("A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D")]
	public class OptionsPage : UIElementDialogPage
	{
		private bool enableToastNotifications = true;
		private int notificationDurationSeconds = 5;

		/// <summary>
		/// Gets or sets whether toast notifications are enabled
		/// </summary>
		[Category("Notifications")]
		[DisplayName("Enable Toast Notifications")]
		[Description("Show Windows toast notifications when builds complete")]
		public bool EnableToastNotifications
		{
			get => enableToastNotifications;
			set => enableToastNotifications = value;
		}

		/// <summary>
		/// Gets or sets the duration in seconds for notifications to be displayed
		/// </summary>
		[Category("Notifications")]
		[DisplayName("Notification Duration (seconds)")]
		[Description("How long notifications should be displayed (in seconds)")]
		public int NotificationDurationSeconds
		{
			get => notificationDurationSeconds;
			set => notificationDurationSeconds = Math.Max(1, Math.Min(60, value)); // Clamp between 1 and 60
		}

		protected override UIElement Child
		{
			get
			{
				return new OptionsPageControl(this);
			}
		}
	}
}

