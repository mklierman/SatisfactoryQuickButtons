using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

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
		private ObservableCollection<ModItem> modItems = new ObservableCollection<ModItem>();
		private string enabledMods = string.Empty; // Semicolon-separated list of enabled mod names
		private string satisfactorySteamInstallLocation = string.Empty;
		private string satisfactoryEpicInstallLocation = string.Empty;
		private string satisfactoryServerWindowsInstallLocation = string.Empty;

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

		[Category("Mods")]
		[DisplayName("Satisfactory Steam Install Location")]
		[Description("Path to the Satisfactory Steam game installation directory")]
		public string SatisfactorySteamInstallLocation
		{
			get => satisfactorySteamInstallLocation;
			set => satisfactorySteamInstallLocation = value ?? string.Empty;
		}

		[Category("Mods")]
		[DisplayName("Satisfactory Epic Install Location")]
		[Description("Path to the Satisfactory Epic game installation directory")]
		public string SatisfactoryEpicInstallLocation
		{
			get => satisfactoryEpicInstallLocation;
			set => satisfactoryEpicInstallLocation = value ?? string.Empty;
		}

		[Category("Mods")]
		[DisplayName("Satisfactory Server Windows Install Location")]
		[Description("Path to the Satisfactory Server Windows installation directory")]
		public string SatisfactoryServerWindowsInstallLocation
		{
			get => satisfactoryServerWindowsInstallLocation;
			set => satisfactoryServerWindowsInstallLocation = value ?? string.Empty;
		}

		public ObservableCollection<ModItem> ModItems
		{
			get => modItems;
		}

		[Category("Mods")]
		[DisplayName("Enabled Mods")]
		[Description("Semicolon-separated list of enabled mod names")]
		[Browsable(false)]
		public string EnabledMods
		{
			get
			{
				return string.Join(";", modItems.Where(m => m.IsEnabled).Select(m => m.Name));
			}
			set
			{
				enabledMods = value ?? string.Empty;
			}
		}

		protected override UIElement Child
		{
			get
			{
				return new OptionsPageControl(this);
			}
		}

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			
			enabledMods = string.Join(";", modItems.Where(m => m.IsEnabled).Select(m => m.Name));
		}

		public async Task DiscoverModsAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			modItems.Clear();

			try
			{
				var dte = await package.GetServiceAsync(typeof(DTE)) as DTE;
				if (dte?.Solution == null)
				{
					System.Diagnostics.Debug.WriteLine("DiscoverModsAsync: No solution loaded");
					return;
				}

				Project factoryGameProject = FindProjectRecursive(dte.Solution.Projects, "FactoryGame");
				if (factoryGameProject == null)
				{
					System.Diagnostics.Debug.WriteLine("DiscoverModsAsync: FactoryGame project not found");
					return;
				}

				ProjectItem modsFolder = FindProjectItemRecursive(factoryGameProject.ProjectItems, "Mods");
				if (modsFolder == null)
				{
					System.Diagnostics.Debug.WriteLine("DiscoverModsAsync: Mods folder not found");
					return;
				}

				if (modsFolder.ProjectItems != null)
				{
					var enabledModNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					if (!string.IsNullOrEmpty(enabledMods))
					{
						enabledModNames = new HashSet<string>(
							enabledMods.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
								.Select(n => n.Trim()),
							StringComparer.OrdinalIgnoreCase);
					}

					int modCount = 0;
					foreach (ProjectItem item in modsFolder.ProjectItems)
					{
						if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
							item.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
						{
							var existingMod = modItems.FirstOrDefault(m => m.Name == item.Name);
							if (existingMod == null)
							{
								bool isEnabled = enabledModNames.Contains(item.Name);
								modItems.Add(new ModItem { Name = item.Name, IsEnabled = isEnabled });
								modCount++;
							}
						}
					}
					System.Diagnostics.Debug.WriteLine($"DiscoverModsAsync: Found {modCount} mods");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("DiscoverModsAsync: Mods folder has no ProjectItems");
				}
			}
			catch (Exception ex)
			{
				// Log the exception for debugging
				System.Diagnostics.Debug.WriteLine($"DiscoverModsAsync exception: {ex.Message}\n{ex.StackTrace}");
			}
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
						foreach (ProjectItem item in proj.ProjectItems)
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
					foreach (ProjectItem item in project.ProjectItems)
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

		private ProjectItem FindProjectItemRecursive(ProjectItems items, string itemName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (items == null)
				return null;

			foreach (ProjectItem item in items)
			{
				if (item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}

				if (item.ProjectItems != null && item.ProjectItems.Count > 0)
				{
					var found = FindProjectItemRecursive(item.ProjectItems, itemName);
					if (found != null)
						return found;
				}
			}

			return null;
		}
	}
}

