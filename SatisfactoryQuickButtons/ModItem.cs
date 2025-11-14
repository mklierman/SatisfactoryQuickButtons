using System.ComponentModel;

namespace SatisfactoryQuickButtons
{
	public class ModItem : INotifyPropertyChanged
	{
		private bool isEnabled;

		public string Name { get; set; }

		public bool IsEnabled
		{
			get => isEnabled;
			set
			{
				if (isEnabled != value)
				{
					isEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

