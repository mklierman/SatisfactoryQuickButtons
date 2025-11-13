using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio;

namespace SatisfactoryQuickButtons
{
	[Guid("A1B2C3D4-E5F6-4789-A012-B3456789CDEF")]
	public sealed class SatisfactorySolutionContext
	{
		public static readonly Guid Guid = new Guid("A1B2C3D4-E5F6-4789-A012-B3456789CDEF");
		
		private const string TargetSolutionName = "FactoryGame";

		public static async Task<bool> IsSatisfactorySolutionLoadedAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			try
			{
				var dte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
				if (dte?.Solution == null || string.IsNullOrEmpty(dte.Solution.FullName))
				{
					return false;
				}

				string solutionPath = dte.Solution.FullName;
				string solutionName = System.IO.Path.GetFileNameWithoutExtension(solutionPath);
				
				return solutionName.Equals(TargetSolutionName, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		public static bool IsSatisfactorySolutionLoaded(AsyncPackage package)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				var serviceProvider = ServiceProvider.GlobalProvider;
				if (serviceProvider == null)
				{
					return false;
				}

				var dte = serviceProvider.GetService<DTE, DTE>();
				if (dte == null)
				{
					return false;
				}
				
				if (dte.Solution == null)
				{
					return false;
				}
				
				if (string.IsNullOrEmpty(dte.Solution.FullName))
				{
					return false;
				}

				string solutionPath = dte.Solution.FullName;
				string solutionName = System.IO.Path.GetFileNameWithoutExtension(solutionPath);
				
				return solutionName.Equals(TargetSolutionName, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}
	}
}

