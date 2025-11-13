using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Toolkit.Uwp.Notifications;

namespace SatisfactoryQuickButtons
{
	internal static class BuildNotificationHelper
	{
		public static async Task ShowBuildNotificationAsync(AsyncPackage package, string buildName, bool succeeded)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			try
			{
				var optionsPage = GetOptionsPage(package);
				bool enableToastNotifications = optionsPage?.EnableToastNotifications ?? true;
				int notificationDurationSeconds = optionsPage?.NotificationDurationSeconds ?? 5;

				var statusBar = await package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
				if (statusBar == null)
					return;

				string message = succeeded
					? $"{buildName} completed successfully!"
					: $"{buildName} failed. Check the Output window for details.";

				statusBar.SetText(message);

				if (enableToastNotifications)
				{
					_ = Task.Run(() =>
					{
						try
						{
							ShowWindowsToastNotification(message, succeeded, notificationDurationSeconds);
						}
						catch { }
					});
				}
				
				_ = Task.Run(async () =>
				{
					await Task.Delay(notificationDurationSeconds * 1000);
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					statusBar.Clear();
				});
			}
			catch { }
		}

		private static OptionsPage GetOptionsPage(AsyncPackage package)
		{
			try
			{
				if (package is SatisfactoryQuickButtonsPackage sqbPackage)
				{
					return sqbPackage.GetOptionsPage();
				}
			}
			catch { }
			return null;
		}

		private static void ShowWindowsToastNotification(string message, bool succeeded, int durationSeconds)
		{
			try
			{
				var toastBuilder = new ToastContentBuilder()
					.AddText(message);

				toastBuilder.SetToastScenario(succeeded ? ToastScenario.Default : ToastScenario.Alarm);

				toastBuilder.Show(toast =>
				{
					toast.ExpirationTime = DateTime.Now.AddSeconds(durationSeconds);
				});
			}
			catch { }
		}
	}
}

