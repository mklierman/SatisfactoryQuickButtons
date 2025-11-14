using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;

namespace SatisfactoryQuickButtons
{
	internal sealed class LaunchSteamCommand
	{
		public const int CommandId = 0x0106;

		public static readonly Guid CommandSet = new Guid("19bd05a3-bd28-438f-a123-fbcdc6afd845");

		private readonly AsyncPackage package;

		private LaunchSteamCommand(AsyncPackage package, IMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		public static LaunchSteamCommand Instance
		{
			get;
			private set;
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			IMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
			Instance = new LaunchSteamCommand(package, commandService);
		}

		private void Execute(object sender, EventArgs e)
		{
			this.package.JoinableTaskFactory.RunAsync(async () =>
			{
				await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
				await LaunchSteamAsync();
			});
		}

		private async Task LaunchSteamAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			try
			{
				var optionsPage = GetOptionsPage();
				if (optionsPage == null)
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						"Unable to access options page.",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				string installLocation = optionsPage.SatisfactorySteamInstallLocation;
				if (string.IsNullOrEmpty(installLocation))
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						"Satisfactory Steam Install Directory is not configured.",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				string exePath = Path.Combine(installLocation, "FactoryGameSteam.exe");
				if (!File.Exists(exePath))
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						$"FactoryGameSteam.exe not found at:\n{exePath}",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = exePath,
					UseShellExecute = true,
					WorkingDirectory = installLocation
				};

				Process.Start(startInfo);
			}
			catch (Exception ex)
			{
				VsShellUtilities.ShowMessageBox(
					this.package,
					$"Error launching Satisfactory Steam: {ex.Message}",
					"Launch Error",
					OLEMSGICON.OLEMSGICON_CRITICAL,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
		}

		private OptionsPage GetOptionsPage()
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
	}
}

