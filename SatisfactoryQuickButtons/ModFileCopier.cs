using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace SatisfactoryQuickButtons
{
	internal static class ModFileCopier
	{
		public static async Task CopyModFilesAfterBuildAsync(AsyncPackage package, string buildName, bool buildSucceeded)
		{
			if (!buildSucceeded)
				return;

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			try
			{
				var optionsPage = GetOptionsPage(package);
				if (optionsPage == null)
					return;

				string installLocation = null;
				string filePrefix = null;

				string buildNameLower = buildName.ToLowerInvariant();
				if (buildNameLower.Contains("steam"))
				{
					installLocation = optionsPage.SatisfactorySteamInstallLocation;
					filePrefix = "FactoryGameSteam-";
				}
				else if (buildNameLower.Contains("epic"))
				{
					installLocation = optionsPage.SatisfactoryEpicInstallLocation;
					filePrefix = "FactoryGameEGS-";
				}
				else if (buildNameLower.Contains("win server") || buildNameLower.Contains("server"))
				{
					installLocation = optionsPage.SatisfactoryServerWindowsInstallLocation;
					filePrefix = "FactoryServer-";
				}

				if (string.IsNullOrEmpty(installLocation))
					return;

				var dte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
				if (dte?.Solution == null || string.IsNullOrEmpty(dte.Solution.FullName))
					return;

				string solutionDirectory = Path.GetDirectoryName(dte.Solution.FullName);
				if (string.IsNullOrEmpty(solutionDirectory))
					return;

				var enabledMods = optionsPage.ModItems.Where(m => m.IsEnabled).ToList();
				if (enabledMods.Count == 0)
					return;

				foreach (var mod in enabledMods)
				{
					await CopyModFilesAsync(solutionDirectory, mod.Name, installLocation, filePrefix);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error copying mod files: {ex.Message}\n{ex.StackTrace}");
			}
		}

		private static async Task CopyModFilesAsync(string solutionDirectory, string modName, string installLocation, string filePrefix)
		{
			await Task.Run(() =>
			{
				try
				{
					string sourceDir = Path.Combine(solutionDirectory, "Mods", modName, "Binaries", "Win64");
					if (!Directory.Exists(sourceDir))
					{
						System.Diagnostics.Debug.WriteLine($"Source directory not found: {sourceDir}");
						return;
					}

					string destDir = Path.Combine(installLocation, "FactoryGame", "Mods", modName, "Binaries", "Win64");
					
					if (!Directory.Exists(destDir))
					{
						Directory.CreateDirectory(destDir);
					}

					string dllFileName = $"{filePrefix}{modName}-Win64-Shipping.dll";
					string pdbFileName = $"{filePrefix}{modName}-Win64-Shipping.pdb";

					string sourceDll = Path.Combine(sourceDir, dllFileName);
					string sourcePdb = Path.Combine(sourceDir, pdbFileName);
					string destDll = Path.Combine(destDir, dllFileName);
					string destPdb = Path.Combine(destDir, pdbFileName);

					if (File.Exists(sourceDll))
					{
						File.Copy(sourceDll, destDll, overwrite: true);
						System.Diagnostics.Debug.WriteLine($"Copied: {sourceDll} -> {destDll}");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"Source DLL not found: {sourceDll}");
					}

					if (File.Exists(sourcePdb))
					{
						File.Copy(sourcePdb, destPdb, overwrite: true);
						System.Diagnostics.Debug.WriteLine($"Copied: {sourcePdb} -> {destPdb}");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"Source PDB not found: {sourcePdb}");
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error copying files for mod {modName}: {ex.Message}\n{ex.StackTrace}");
				}
			});
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
	}
}

