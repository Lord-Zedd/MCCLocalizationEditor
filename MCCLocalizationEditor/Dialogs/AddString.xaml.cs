using System.Windows;

namespace MCCLocalizationEditor.Dialogs
{
	/// <summary>
	/// Interaction logic for AddString.xaml
	/// </summary>
	public partial class AddString : Window
	{
		UnicodeCollection _collection;
		public LocalizationPair ResultEntry { get; protected set; }

		public AddString(UnicodeCollection collection)
		{
			InitializeComponent();
			TextBoxValueString.Focus();
			_collection = collection;
		}

		private void Import_Click(object sender, RoutedEventArgs e)
		{
			string cleanString = _collection.StringsToUnicode(TextBoxValueString.Text);

			if ((bool)RadioKeyString.IsChecked)
			{
				if (string.IsNullOrEmpty(TextBoxKeyString.Text))
				{
					MessageBox.Show("Please enter a valid Key String value.");
					return;
				}

				ResultEntry = new LocalizationPair(TextBoxKeyString.Text, cleanString);
			}
			else
			{
				bool parsed = uint.TryParse(TextBoxKeyInt.Text, out uint hash);

				if (!parsed)
				{
					MessageBox.Show("Please enter a valid Key UInt value. Decimal is expected, not hex.");
					return;
				}

				ResultEntry = new LocalizationPair(hash, cleanString);
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
