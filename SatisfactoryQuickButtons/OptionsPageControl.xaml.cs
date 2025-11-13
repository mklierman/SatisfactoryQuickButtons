using System.Windows;
using System.Windows.Controls;

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
		}
	}
}

