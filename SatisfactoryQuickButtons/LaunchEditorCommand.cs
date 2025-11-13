using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Diagnostics;

namespace SatisfactoryQuickButtons
{
	internal sealed class LaunchEditorCommand
	{
		public const int CommandId = 0x0103;

		public static readonly Guid CommandSet = new Guid("19bd05a3-bd28-438f-a123-fbcdc6afd845");

		private readonly AsyncPackage package;

		private LaunchEditorCommand(AsyncPackage package, IMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		public static LaunchEditorCommand Instance
		{
			get;
			private set;
		}

		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			IMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
			Instance = new LaunchEditorCommand(package, commandService);
		}

		private void Execute(object sender, EventArgs e)
		{
			this.package.JoinableTaskFactory.RunAsync(async () =>
			{
				await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
				await LaunchEditorAsync();
			});
		}

		private async Task LaunchEditorAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			try
			{
				var dte = await this.package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
				if (dte == null || dte.Solution == null)
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						"No solution is currently open.",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				string solutionPath = dte.Solution.FullName;
				if (string.IsNullOrEmpty(solutionPath))
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						"Unable to get solution path.",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				string solutionDirectory = Path.GetDirectoryName(solutionPath);
				if (string.IsNullOrEmpty(solutionDirectory))
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						"Unable to get solution directory.",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				string uprojectPath = Path.Combine(solutionDirectory, "FactoryGame.uproject");
				if (!File.Exists(uprojectPath))
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						$"FactoryGame.uproject not found in:\n{solutionDirectory}",
						"Launch Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = uprojectPath,
					UseShellExecute = true
				};

				System.Diagnostics.Process.Start(startInfo);
			}
			catch (Exception ex)
			{
				VsShellUtilities.ShowMessageBox(
					this.package,
					$"Error launching editor: {ex.Message}",
					"Launch Error",
					OLEMSGICON.OLEMSGICON_CRITICAL,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
		}
	}
}
