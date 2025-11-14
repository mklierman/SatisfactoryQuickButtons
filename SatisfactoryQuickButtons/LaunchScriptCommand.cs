using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using EnvDTE;

namespace SatisfactoryQuickButtons
{
	internal sealed class LaunchScriptCommand
	{
		public const int CommandId = 0x0109;

		public static readonly Guid CommandSet = new Guid("19bd05a3-bd28-438f-a123-fbcdc6afd845");

		private readonly AsyncPackage package;

		private LaunchScriptCommand(AsyncPackage package, IMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		public static LaunchScriptCommand Instance
		{
			get;
			private set;
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			IMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
			Instance = new LaunchScriptCommand(package, commandService);
		}

		private void Execute(object sender, EventArgs e)
		{
			this.package.JoinableTaskFactory.RunAsync(async () =>
			{
				await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
				await LaunchScriptAsync();
			});
		}

		private async Task LaunchScriptAsync()
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

				string scriptPath = optionsPage.LaunchScriptPath;
				if (string.IsNullOrEmpty(scriptPath))
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						"Launch Script Path is not configured.",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				if (!File.Exists(scriptPath))
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						$"Script file not found at:\n{scriptPath}",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				string workingDirectory = Path.GetDirectoryName(scriptPath);
				if (string.IsNullOrEmpty(workingDirectory))
				{
					workingDirectory = Environment.CurrentDirectory;
				}

				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = scriptPath,
					UseShellExecute = true,
					WorkingDirectory = workingDirectory
				};

				System.Diagnostics.Process.Start(startInfo);
			}
			catch (Exception ex)
			{
				VsShellUtilities.ShowMessageBox(
					this.package,
					$"Error launching script: {ex.Message}",
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

