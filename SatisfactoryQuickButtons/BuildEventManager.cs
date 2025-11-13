using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace SatisfactoryQuickButtons
{
	internal class BuildEventManager
	{
		private readonly AsyncPackage package;
		private readonly Dictionary<string, string> registeredBuilds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private DTE2 dte;

		public BuildEventManager(AsyncPackage package)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
		}

		public async Task SubscribeToBuildEventsAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			try
			{
				dte = await this.package.GetServiceAsync(typeof(DTE)) as DTE2;
				if (dte != null && dte.Events != null)
				{
					var buildEvents = dte.Events.BuildEvents;
					if (buildEvents != null)
					{
						buildEvents.OnBuildDone += OnBuildDone;
					}
				}
			}
			catch { }
		}

		public void RegisterBuild(string projectName, string buildName)
		{
			if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(buildName))
				return;

			lock (registeredBuilds)
			{
				registeredBuilds[projectName] = buildName;
			}
		}

		private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				if (dte == null || dte.Solution == null)
					return;

				lock (registeredBuilds)
				{
					if (registeredBuilds.Count == 0)
						return;

					var solutionBuild = dte.Solution.SolutionBuild;
					if (solutionBuild == null)
						return;

					bool buildSucceeded = solutionBuild.LastBuildInfo == 0;

					if (registeredBuilds.Count > 0)
					{
						var firstBuild = new List<KeyValuePair<string, string>>(registeredBuilds);
						var buildToNotify = firstBuild[0];
						
						string projectName = buildToNotify.Key;
						string buildName = buildToNotify.Value;

						_ = Task.Run(async () =>
						{
							await BuildNotificationHelper.ShowBuildNotificationAsync(package, buildName, buildSucceeded);
						});

						registeredBuilds.Remove(projectName);
					}
				}
			}
			catch { }
		}

		private Project FindProjectRecursive(Projects projects, string projectName)
		{
			if (projects == null)
				return null;

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
			if (project == null)
				return null;

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
	}
}

