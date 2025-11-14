using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace SatisfactoryQuickButtons
{
	public partial class OptionsPageControl : UserControl
	{
		private readonly OptionsPage optionsPage;

		public OptionsPageControl(OptionsPage optionsPage)
		{
			InitializeComponent();
			this.optionsPage = optionsPage;
			this.DataContext = optionsPage;
			
			this.Loaded += OptionsPageControl_Loaded;
		}

		private void OptionsPageControl_Loaded(object sender, RoutedEventArgs e)
		{
			_ = DiscoverModsAsync();
		}

		private async Task DiscoverModsAsync()
		{
			try
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				
				if (optionsPage.Site != null)
				{
					var package = optionsPage.Site.GetService(typeof(AsyncPackage)) as AsyncPackage;
					if (package != null)
					{
						await optionsPage.DiscoverModsAsync(package);
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error discovering mods: {ex.Message}\n{ex.StackTrace}");
			}
		}
	}
}

