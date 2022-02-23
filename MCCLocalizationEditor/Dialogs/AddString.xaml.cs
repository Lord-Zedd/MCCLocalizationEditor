using System.Windows;

namespace MCCLocalizationEditor.Dialogs
{
	/// <summary>
	/// Interaction logic for AddString.xaml
	/// </summary>
	public partial class AddString : Window
	{
		public LocalizationPair ResultEntry { get; protected set; }

		public AddString()
		{
			InitializeComponent();
			TextBoxValueString.Focus();
		}

		private void Import_Click(object sender, RoutedEventArgs e)
		{

			if ((bool)RadioKeyString.IsChecked)
			{
				if (string.IsNullOrEmpty(TextBoxKeyString.Text))
				{
					MessageBox.Show("Please enter a valid Key String value.");
					return;
				}

				ResultEntry = new LocalizationPair(TextBoxKeyString.Text, TextBoxValueString.Text);
			}
			else
			{
				bool parsed = uint.TryParse(TextBoxKeyInt.Text, out uint hash);

				if (!parsed)
				{
					MessageBox.Show("Please enter a valid Key UInt value. Decimal is expected, not hex.");
					return;
				}

				ResultEntry = new LocalizationPair(hash, TextBoxValueString.Text);
			}

			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
