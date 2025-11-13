using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace SatisfactoryQuickButtons
{
	internal sealed class BuildEditorCommand
	{
		public const int CommandId = 0x0100;

		public static readonly Guid CommandSet = new Guid("19bd05a3-bd28-438f-a123-fbcdc6afd845");

		private readonly AsyncPackage package;

		private BuildEditorCommand(AsyncPackage package, IMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		public static BuildEditorCommand Instance
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
			Instance = new BuildEditorCommand(package, commandService);
		}

		private void Execute(object sender, EventArgs e)
		{
			this.package.JoinableTaskFactory.RunAsync(async () =>
			{
				await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
				await BuildProjectWithConfigurationAsync("Development Editor", "FactoryGame");
			});
		}

		private Project FindProjectRecursive(Projects projects, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			
			foreach (Project proj in projects)
			{
				if (proj.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
				{
					if (proj.ProjectItems != null)
					{
						foreach (EnvDTE.ProjectItem item in proj.ProjectItems)
						{
							if (item.SubProject != null)
							{
								var found = FindProjectRecursive(item.SubProject, projectName);
								if (found != null)
									return found;
							}
						}
					}
				}
				else
				{
					if (proj.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase) ||
						proj.UniqueName.EndsWith(projectName + ".vcxproj", StringComparison.OrdinalIgnoreCase) ||
						proj.UniqueName.EndsWith(projectName + ".csproj", StringComparison.OrdinalIgnoreCase))
					{
						return proj;
					}
				}
			}
			
			return null;
		}

		private Project FindProjectRecursive(Project project, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			
			if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
			{
				if (project.ProjectItems != null)
				{
					foreach (EnvDTE.ProjectItem item in project.ProjectItems)
					{
						if (item.SubProject != null)
						{
							var found = FindProjectRecursive(item.SubProject, projectName);
							if (found != null)
								return found;
						}
					}
				}
			}
			else
			{
				if (project.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase) ||
					project.UniqueName.EndsWith(projectName + ".vcxproj", StringComparison.OrdinalIgnoreCase) ||
					project.UniqueName.EndsWith(projectName + ".csproj", StringComparison.OrdinalIgnoreCase))
				{
					return project;
				}
			}
			
			return null;
		}

		private bool SelectProjectRecursive(UIHierarchyItem item, Project targetProject)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			
			try
			{
				if (item.Object is Project proj)
				{
					if (proj.UniqueName == targetProject.UniqueName)
					{
						item.Select(vsUISelectionType.vsUISelectionTypeSelect);
						return true;
					}
				}
				
				foreach (UIHierarchyItem child in item.UIHierarchyItems)
				{
					if (SelectProjectRecursive(child, targetProject))
						return true;
				}
			}
			catch { }
			
			return false;
		}

		private async Task BuildProjectWithConfigurationAsync(string configurationName, string projectName)
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
						"Build Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				Solution solution = dte.Solution;
				if (solution.SolutionBuild == null)
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						"Unable to access solution build manager.",
						"Build Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				SolutionConfiguration2 config = null;
				string targetPlatform = "Win64";
				
				Project project = FindProjectRecursive(solution.Projects, projectName);
				
				if (project == null)
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						$"Project '{projectName}' not found in the solution.",
						"Build Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}
				
				var buildEventManager = SatisfactoryQuickButtonsPackage.BuildEventManager;
				if (buildEventManager != null)
				{
					buildEventManager.RegisterBuild(projectName, "Build Editor");
				}
				
				string targetProjectFileName = System.IO.Path.GetFileName(project.UniqueName);
				
				string fullConfigName = $"{configurationName}|{targetPlatform}";
				foreach (SolutionConfiguration2 solConfig in solution.SolutionBuild.SolutionConfigurations)
				{
					if (solConfig.Name.Equals(fullConfigName, StringComparison.OrdinalIgnoreCase))
					{
						config = solConfig;
						break;
					}
				}
				
				if (config == null)
				{
					foreach (SolutionConfiguration2 solConfig in solution.SolutionBuild.SolutionConfigurations)
					{
						if (solConfig.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase) ||
							solConfig.Name.StartsWith(configurationName + "|", StringComparison.OrdinalIgnoreCase))
						{
							foreach (SolutionContext context in solConfig.SolutionContexts)
							{
								string contextProjectFileName = System.IO.Path.GetFileName(context.ProjectName);
								bool projectMatches = contextProjectFileName.Equals(targetProjectFileName, StringComparison.OrdinalIgnoreCase);
								
								if (projectMatches &&
									(context.PlatformName.Equals(targetPlatform, StringComparison.OrdinalIgnoreCase) ||
									 (context.PlatformName.Equals("x64", StringComparison.OrdinalIgnoreCase) && targetPlatform.Equals("Win64", StringComparison.OrdinalIgnoreCase))))
								{
									config = solConfig;
									break;
								}
							}
							if (config != null) break;
						}
					}
				}

				if (config == null)
				{
					VsShellUtilities.ShowMessageBox(
						this.package,
						$"Configuration '{configurationName}' with platform '{targetPlatform}' not found in the solution. Please ensure this configuration exists.",
						"Build Error",
						OLEMSGICON.OLEMSGICON_WARNING,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
					return;
				}

				var buildManager2 = await this.package.GetServiceAsync(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
				var solutionService = await this.package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
				
				if (buildManager2 != null && solutionService != null)
				{
					IVsHierarchy projectHierarchy = null;
					Guid projectGuid = Guid.Empty;
					
					try
					{
						if (project.Object != null)
						{
							projectHierarchy = project.Object as IVsHierarchy;
						}
					}
					catch { }
					
					if (projectHierarchy == null)
					{
						try
						{
							if (project.Properties != null)
							{
								var prop = project.Properties.Item("ProjectGuid");
								if (prop != null)
								{
									projectGuid = new Guid(prop.Value.ToString());
								}
							}
						}
						catch { }
						
						if (projectGuid != Guid.Empty)
						{
							solutionService.GetProjectOfGuid(ref projectGuid, out projectHierarchy);
						}
					}
					
					if (projectHierarchy == null)
					{
						try
						{
							solutionService.GetProjectOfUniqueName(project.UniqueName, out projectHierarchy);
						}
						catch { }
					}
					
					if (projectHierarchy == null)
					{
						try
						{
							VSUPDATEPROJREFREASON[] reasons = new VSUPDATEPROJREFREASON[1];
							solutionService.GetProjectOfProjref(project.UniqueName, out projectHierarchy, out string updatedProjRef, reasons);
						}
						catch { }
					}
					
					if (projectHierarchy != null)
					{
						config.Activate();
						await Task.Delay(500);
						await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
						
						IVsHierarchy[] hierarchies = new IVsHierarchy[] { projectHierarchy };
						uint buildFlags = (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD;
						
						int hr = buildManager2.StartUpdateProjectConfigurations(
							(uint)hierarchies.Length,
							hierarchies,
							buildFlags,
							0);
						
						if (hr == VSConstants.S_OK)
						{
							return;
						}
					}
				}
				
				config.Activate();
				await Task.Delay(500);
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				
				var window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer);
				if (window != null)
				{
					window.Activate();
					var solExp = window.Object as EnvDTE.UIHierarchy;
					if (solExp != null)
					{
						foreach (UIHierarchyItem item in solExp.UIHierarchyItems)
						{
							SelectProjectRecursive(item, project);
						}
					}
				}
				
				dte.ExecuteCommand("Build.BuildOnlyProject", "");
			}
			catch (Exception ex)
			{
				VsShellUtilities.ShowMessageBox(
					this.package,
					$"Error building solution: {ex.Message}",
					"Build Error",
					OLEMSGICON.OLEMSGICON_CRITICAL,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
		}
	}
}
