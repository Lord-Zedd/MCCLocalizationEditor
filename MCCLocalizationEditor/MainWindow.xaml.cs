using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Navigation;
using MCCLocalizationEditor.Dialogs;

namespace MCCLocalizationEditor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ObservableCollection<LocalizationPair> Strings { get; set; }
		private string _lastOpenedFile = "";

		public MainWindow()
		{
			InitializeComponent();
			Strings = new ObservableCollection<LocalizationPair>();
			EditorGrid.DataContext = EditorGrid.ItemsSource = Strings;
			TxtLastFile.Text = _lastOpenedFile;
		}

		private void SetStringCollection(List<LocalizationPair> strings)
		{
			strings = strings.OrderBy(p => p.KeyHash).ToList();
			EditorGrid.ItemsSource = null;
			Strings.Clear();
			strings.ForEach(Strings.Add);
			EditorGrid.ItemsSource = Strings;
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
		}

		#region File Menu
		private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
		{
			if (Strings.Count > 0)
			{
				MessageBoxResult result = MessageBox.Show("There are currently strings loaded. Opening a new file will erase them. Continue?", "Confirm Action", MessageBoxButton.OKCancel);
				if (result != MessageBoxResult.OK)
					return;
			}

			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
			{
				RestoreDirectory = true,
				Title = "Open Localization",
				Filter = "Localization (*.bin)|*.bin"
			};
			if (!(bool)ofd.ShowDialog())
				return;

			SetStringCollection(LocalizationData.ReadBinFile(ofd.FileName));

			_lastOpenedFile = ofd.SafeFileName;
			TxtLastFile.Text = _lastOpenedFile;
		}

		private void MenuItemSave_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
			{
				RestoreDirectory = true,
				Title = "Save Localization",
				Filter = "Localization (*.bin)|*.bin",
				FileName = _lastOpenedFile,
			};
			if (!(bool)sfd.ShowDialog())
				return;

			LocalizationData.SaveBinFile(sfd.FileName, Strings.ToList());
		}

		private void MenuItemOpenXML_Click(object sender, RoutedEventArgs e)
		{
			if (Strings.Count > 0)
			{
				MessageBoxResult result = MessageBox.Show("There are currently strings loaded. Opening a new file will erase them. Continue?", "Confirm Action", MessageBoxButton.OKCancel);
				if (result != MessageBoxResult.OK)
					return;
			}

			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
			{
				RestoreDirectory = true,
				Title = "Open XML",
				Filter = "Localization XML (*.xml)|*.xml"
			};
			if (!(bool)ofd.ShowDialog())
				return;

			SetStringCollection(LocalizationData.ReadXMLFile(ofd.FileName));
		}

		private void MenuItemSaveXML_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
			{
				RestoreDirectory = true,
				Title = "Save XML",
				Filter = "Localization XML (*.xml)|*.xml",
			};
			if (!(bool)sfd.ShowDialog())
				return;

			LocalizationData.SaveXMLFile(sfd.FileName, Strings.ToList());
		}

		#endregion

		private void MenuItemAdd_Click(object sender, RoutedEventArgs e)
		{
			AddString astring = new AddString();
			astring.ShowDialog();

			if (astring.DialogResult.Value == false)
				return;

			List<LocalizationPair> strings = Strings.ToList();
			strings.Add(astring.ResultEntry);
			SetStringCollection(strings);

			EditorGrid.Focus();
			EditorGrid.SelectedItem = astring.ResultEntry;
			EditorGrid.ScrollIntoView(astring.ResultEntry);
		}

		private void MenuItemHelp_Click(object sender, RoutedEventArgs e)
		{
			LocaleHelp help = new LocaleHelp();
			help.ShowDialog();
		}

		private void ButtonFind_Click(object sender, RoutedEventArgs e)
		{
			LocalizationPair result;

			if ((bool)RadioKeyString.IsChecked)
			{
				if (string.IsNullOrEmpty(TextBoxKeyString.Text))
				{
					MessageBox.Show("Please enter a valid Key String value.");
					return;
				}

				uint hash = CRC32MPEG.CountCRC(Encoding.ASCII.GetBytes(TextBoxKeyString.Text));

				result = Strings.FirstOrDefault(p => p.KeyHash == hash);

			}
			else if ((bool)RadioKeyInt.IsChecked)
			{
				bool parsed = uint.TryParse(TextBoxKeyInt.Text, out uint hash);

				if (!parsed)
				{
					MessageBox.Show("Please enter a valid Key UInt value. Decimal is expected, not hex.");
					return;
				}

				result = Strings.FirstOrDefault(p => p.KeyHash == hash);

			}
			else
			{
				string val = TextBoxValueString.Text;

				if ((bool)CheckStringCase.IsChecked)
					result = Strings.FirstOrDefault(p => p.String.Contains(val));
				else
					result = Strings.FirstOrDefault(p => p.String.ToLower().Contains(val.ToLower()));

			}

			if (result != null)
			{
				EditorGrid.Focus();
				EditorGrid.SelectedItem = result;
				EditorGrid.ScrollIntoView(result);
			}
			else
			{
				MessageBox.Show("No result found. If using a Key String try different casing.");
				return;
			}
		}

		private void ButtonClear_Click(object sender, RoutedEventArgs e)
		{
			Strings.Clear();
		}
	}
}
