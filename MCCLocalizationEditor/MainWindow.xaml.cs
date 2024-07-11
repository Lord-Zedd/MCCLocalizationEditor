using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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

		SourceDragDrop srcDD = null;
		AddString addStr = null;
		LocaleHelp locHelp = null;

		UnicodeBank _unicBank;

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

		private void MergeStringCollection(List<LocalizationPair> newStrings)
		{
			if (newStrings == null)
				return;

			List<LocalizationPair> currentStrings = Strings.ToList();

			//remove previous instances of the new strings
			foreach (LocalizationPair loc in newStrings)
				currentStrings.RemoveAll(x => x.KeyHash == loc.KeyHash);

			currentStrings.AddRange(newStrings);
			SetStringCollection(currentStrings);
		}

		private UnicodeCollection GetUnicodeCollection()
		{
			if (_unicBank == null)
				return new UnicodeCollection(null);

			UnicodeGame game = (UnicodeGame)((ComboBoxItem)cmbGame.SelectedItem).Tag;

			return _unicBank.WithdrawCollection(game);
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

			var bin = LocalizationData.ReadBinFile(ofd.FileName);
			if (bin == null)
			{
				MessageBox.Show($"Bin file\r\n{ofd.SafeFileName}\r\nFailed to open.", "Bin File");
				return;
			}
			SetStringCollection(bin);

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

			SetStringCollection(LocalizationData.ReadXMLFile(ofd.FileName, GetUnicodeCollection()));
		}

		private void MenuItemImportXML_Click(object sender, RoutedEventArgs e)
		{
			if (Strings.Count > 0)
			{
				MessageBoxResult result = MessageBox.Show("There are currently strings loaded. Importing an XML will overwrite any existing strings with matching hashes. Continue?", "Confirm Action", MessageBoxButton.OKCancel);
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


			var xmlStrings = LocalizationData.ReadXMLFile(ofd.FileName, GetUnicodeCollection());

			MergeStringCollection(xmlStrings);
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

			LocalizationData.SaveXMLFile(sfd.FileName, Strings.ToList(), GetUnicodeCollection());
		}

		private void MenuItemSaveXMLEmpty_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
			{
				RestoreDirectory = true,
				Title = "Save XML",
				Filter = "Localization XML (*.xml)|*.xml",
			};
			if (!(bool)sfd.ShowDialog())
				return;

			LocalizationData.SaveXMLFile(sfd.FileName, null, GetUnicodeCollection());
		}

		#endregion

		private void MenuItemAdd_Click(object sender, RoutedEventArgs e)
		{
			addStr = new AddString(GetUnicodeCollection());
			addStr.ShowDialog();

			if (addStr.DialogResult.Value == false)
				return;

			List<LocalizationPair> strings = Strings.ToList();
			strings.Add(addStr.ResultEntry);
			SetStringCollection(strings);

			EditorGrid.Focus();
			EditorGrid.SelectedItem = addStr.ResultEntry;
			EditorGrid.ScrollIntoView(addStr.ResultEntry);
			addStr = null;
		}

		private void MenuItemImport_Click(object sender, RoutedEventArgs e)
		{
			if (srcDD != null)
			{
				srcDD.Focus();
				return;
			}

			srcDD = new SourceDragDrop(_unicBank);
			srcDD.Closing += SourceDD_Closing;
			srcDD.Owner = this;
			srcDD.Show();
		}

		private void SourceDD_Closing(object sender, CancelEventArgs e)
		{
			MergeStringCollection(srcDD.Strings);

			srcDD.Closing -= SourceDD_Closing;
			srcDD = null;
		}

		private void MenuItemHelp_Click(object sender, RoutedEventArgs e)
		{
			if (locHelp != null)
			{
				locHelp.Focus();
				return;
			}

			locHelp = new LocaleHelp();
			locHelp.Closing += Help_Closing;
			locHelp.Owner = this;
			locHelp.Show();
		}

		private void Help_Closing(object sender, CancelEventArgs e)
		{
			locHelp.Closing -= Help_Closing;
			locHelp = null;
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

				uint hash = CRC32MPEG.CountCRC(Encoding.ASCII.GetBytes(TextBoxKeyString.Text.ToUpper()));

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

		private void window_Loaded(object sender, RoutedEventArgs e)
		{
			string mccLEPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			_unicBank = new UnicodeBank(Path.Combine(mccLEPath, @"unicode\"));
			if (!_unicBank.Initialized)
			{
				MessageBox.Show($"An error occurred while loading XML files from the \\unicode directory\r\n\r\n{_unicBank.Error}\r\n\r\nMCC Localization Editor will now close.", "Unicode XMLs");
				Close();
			}
		}
	}
}
