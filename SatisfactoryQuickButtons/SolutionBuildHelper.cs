using System;
using EnvDTE;
using EnvDTE80;

namespace SatisfactoryQuickButtons
{
	internal static class SolutionBuildHelper
	{
		internal static SolutionConfiguration2 FindSolutionConfiguration(
			Solution solution,
			string configurationName,
			string targetPlatform,
			string targetProjectFileName)
		{
			if (solution?.SolutionBuild == null)
			{
				return null;
			}

			foreach (SolutionConfiguration2 solConfig in solution.SolutionBuild.SolutionConfigurations)
			{
				if (!ConfigurationNameMatches(solConfig, configurationName))
				{
					continue;
				}

				if (!SolutionPlatformMatches(solConfig, targetPlatform))
				{
					continue;
				}

				if (!ProjectParticipatesInConfiguration(solConfig, targetProjectFileName, targetPlatform))
				{
					continue;
				}

				return solConfig;
			}

			return null;
		}

		private static bool ConfigurationNameMatches(SolutionConfiguration2 solConfig, string configurationName)
		{
			if (solConfig.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (solConfig.Name.StartsWith(configurationName + "|", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			int pipeIndex = solConfig.Name.IndexOf('|');
			if (pipeIndex >= 0)
			{
				string configPart = solConfig.Name.Substring(0, pipeIndex);
				return configPart.Equals(configurationName, StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		private static bool SolutionPlatformMatches(SolutionConfiguration2 solConfig, string targetPlatform)
		{
			string solutionPlatform = GetSolutionPlatformName(solConfig);
			return PlatformMatches(solutionPlatform, targetPlatform);
		}

		private static string GetSolutionPlatformName(SolutionConfiguration2 solConfig)
		{
			if (!string.IsNullOrEmpty(solConfig.PlatformName))
			{
				return solConfig.PlatformName;
			}

			int pipeIndex = solConfig.Name.IndexOf('|');
			if (pipeIndex >= 0 && pipeIndex < solConfig.Name.Length - 1)
			{
				return solConfig.Name.Substring(pipeIndex + 1);
			}

			return string.Empty;
		}

		private static bool ProjectParticipatesInConfiguration(
			SolutionConfiguration2 solConfig,
			string targetProjectFileName,
			string targetPlatform)
		{
			string solutionPlatform = GetSolutionPlatformName(solConfig);

			foreach (SolutionContext context in solConfig.SolutionContexts)
			{
				string contextProjectFileName = System.IO.Path.GetFileName(context.ProjectName);
				if (!contextProjectFileName.Equals(targetProjectFileName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (ContextPlatformMatches(context.PlatformName, targetPlatform, solutionPlatform))
				{
					return true;
				}
			}

			return false;
		}

		private static bool PlatformMatches(string platform, string targetPlatform)
		{
			if (string.IsNullOrEmpty(platform))
			{
				return false;
			}

			return platform.Equals(targetPlatform, StringComparison.OrdinalIgnoreCase);
		}

		private static bool ContextPlatformMatches(string contextPlatform, string targetPlatform, string solutionPlatform)
		{
			if (PlatformMatches(contextPlatform, targetPlatform))
			{
				return true;
			}

			// UE 5.6+ may report project platform as "x64" for both Win64 and Linux solution
			// configurations. Only treat "x64" as Win64 when the solution platform is not Linux.
			if (targetPlatform.Equals("Win64", StringComparison.OrdinalIgnoreCase) &&
				contextPlatform.Equals("x64", StringComparison.OrdinalIgnoreCase) &&
				!solutionPlatform.Equals("Linux", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			return false;
		}
	}
}
