using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace MCCLocalizationEditor.Dialogs
{
	/// <summary>
	/// Interaction logic for SourceDragDrop.xaml
	/// </summary>
	public partial class SourceDragDrop : Window
	{
		UnicodeBank _unicBank;
		public List<LocalizationPair> Strings = new List<LocalizationPair>();

		public List<string> Files = new List<string>();

		static readonly List<string> HmtKeys = new List<string>()
		{
			"%a-button",
			"%b-button",
			"%x-button",
			"%y-button",
			"%black-button",
			"%white-button",
			"%left-trigger",
			"%right-trigger",
			"%dpad-up",
			"%dpad-down",
			"%dpad-left",
			"%dpad-right",
			"%start-button",
			"%back-button",
			"%left-thumb",
			"%right-thumb",
			"%left-stick",
			"%right-stick",
			"%action",
			"%throw-grenade",
			"%primary-trigger",
			"%integrated-light",
			"%jump",
			"%use-equipment",
			"%rotate-weapons",
			"%rotate-grenades",
			"%zoom",
			"%crouch",
			"%accept",
			"%back",
			"%move",
			"%look",
			"%custom-1",
			"%custom-2",
			"%custom-3",
			"%custom-4",
			"%custom-5",
			"%custom-6",
			"%custom-7",
			"%custom-8"
		};

		public SourceDragDrop(UnicodeBank bank)
		{
			InitializeComponent();
			_unicBank = bank;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (cmbGame.SelectedItem == null)
				return;

			UnicodeGame game = (UnicodeGame)((ComboBoxItem)cmbGame.SelectedItem).Tag;

			if (game == UnicodeGame.None)
				return;
			else if (game == UnicodeGame.H1)
			{
				foreach (string file in Files)
				{
					string ext = Path.GetExtension(file);
					if (ext == ".hmt")
						Strings.AddRange(ReadH1HUDMessages(file));
					else if (ext == ".txt")
						Strings.AddRange(ReadH1StringList(file));
					else
						continue;
				}
			}
			else
			{
				foreach (string file in Files)
				{
					string ext = Path.GetExtension(file);
					if (ext == ".txt")
						Strings.AddRange(ReadUnicodeList(file, game));
					else
						continue;
				}
			}

			fileList.ItemsSource = null;
			Files.Clear();
			Close();
		}

		private void Grid_Drop(object sender, DragEventArgs e)
		{
			string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, true);
			Focus();

			fileList.ItemsSource = null;

			foreach (string file in draggedFiles)
			{
				string ext = Path.GetExtension(file);
				if (ext != ".txt" && ext != ".hmt")
					continue;

				Files.Add(file);
			}

			fileList.ItemsSource = Files;
		}


		public List<LocalizationPair> ReadH1StringList(string path)
		{
			string tagpath = GetTagPath(path);

			string tagname = Path.GetFileNameWithoutExtension(path).Replace(" ", "_");

			List<LocalizationPair> result = new List<LocalizationPair>();

			//these might be fixed keys rather than using tagname
			string key = "";
			if (tagpath.ToLower().StartsWith("levels\\"))
			{
				var dir = new DirectoryInfo(path);
				key = dir.Parent + "_" + tagname + "_";
			}
			else if (tagpath.ToLower().StartsWith("ui\\hud\\"))
			{
				key = "UH_" + tagname + "_";
			}
			else if (tagpath.ToLower().StartsWith("ui\\shell\\solo_game\\pause_game\\"))
			{
				//dont think these are supported?
				return result;
			}
			else if (tagpath.ToLower().StartsWith("ui\\shell\\solo_game\\player_help\\"))
			{
				key = "USSP_" + tagname + "_";
			}
			else if (tagpath.ToLower().StartsWith("ui\\"))
			{
				key = "U_" + tagname + "_";
			}

			string stringlist = File.ReadAllText(path);

			var matches = Regex.Matches(stringlist, @"(?<string>.+?)(?:\r\n|[\r\n])(?:###END-STRING###)(?:\s*)", RegexOptions.Singleline);

			for (int i = 0; i < matches.Count; i++)
			{
				string text = matches[i].Groups["string"].Value;
				text = CleanString(text, true);
				result.Add(new LocalizationPair(key + i, text));
			}

			return result;
		}

		public List<LocalizationPair> ReadH1HUDMessages(string path)
		{
			string tagpath = GetTagPath(path);

			List<LocalizationPair> result = new List<LocalizationPair>();

			string key = "";
			if (tagpath.ToLower().StartsWith("levels\\"))
			{
				var dir = new DirectoryInfo(path);
				key = dir.Parent + "_HUD_MESSAGES_";
			}
			else if (tagpath.ToLower().StartsWith("ui\\hud\\"))
			{
				key = "HUD_GLOBALS_";
			}

			string[] messages = File.ReadAllLines(path).Where(x=>!string.IsNullOrEmpty(x)).ToArray();

			for (int i = 0; i < messages.Length; i++)
			{
				string msg = messages[i];

				Match m = Regex.Match(msg, @"(?<name>.+)\s*=\s*(?<string>.+)");

				if (!m.Success)
					continue;

				string name = m.Groups["name"].Value;
				string text = m.Groups["string"].Value;

				text = CleanString(text, true);

				if (name == "enter_vehicle")
				{
					//vehicle is a special case where the full string is written to element 0, then the split versions are written to 10, 20, and 30 with extra padding
					string seatTarget = "%custom-1";
					int index = text.IndexOf(seatTarget, StringComparison.OrdinalIgnoreCase);

					string before = text.Substring(0, index);
					string after = text.Substring(index + seatTarget.Length);

					string driver = before + "driver" + after;
					string gunner = before + "gunner" + after;
					string side = before + "side" + after;

					result.Add(new LocalizationPair(key + i + "_0_driver", FormatSpecialHUDVehicleMessage(driver)));
					result.Add(new LocalizationPair(key + i + "_0_gunner", FormatSpecialHUDVehicleMessage(gunner)));
					result.Add(new LocalizationPair(key + i + "_0_side", FormatSpecialHUDVehicleMessage(side)));

					var driverList = HandleHUDMessage(key + i, driver, true, 10);
					var gunnerList = HandleHUDMessage(key + i, gunner, true, 20);
					var sideList = HandleHUDMessage(key + i, gunner, true, 30);

					if (driverList.Count > 10 ||
						gunnerList.Count > 10 ||
						sideList.Count > 10)
					{
						MessageBox.Show($"File at\r\n{path}\r\nHas more than 10 segments for enter_vehicle. It will not be imported.", "HUD Messages");
							continue; 
					}

					result.AddRange(driverList);
					result.AddRange(gunnerList);
					result.AddRange(sideList);

				}
				else
				{
					result.AddRange(HandleHUDMessage(key + i, text, false));

					if (name.StartsWith("obj_") || name.StartsWith("dia_"))
						result.AddRange(HandleHUDMessage("h1obj_" + key + i, text, false));
				}
			}

			return result;
		}

		private List<LocalizationPair> ReadUnicodeList(string path, UnicodeGame game)
		{
			string tagpath = GetTagPath(path);

			List<LocalizationPair> result = new List<LocalizationPair>();

			string[] strings = File.ReadAllLines(path).Where(x => !string.IsNullOrEmpty(x)).ToArray();

			for (int i = 0; i < strings.Length; i++)
			{
				string str = strings[i];

				Match m = Regex.Match(str, @"(?<name>[\S]+)\s*=\s""(?<string>.*)""");

				if (!m.Success)
					continue;

				string name = m.Groups["name"].Value;
				string text = m.Groups["string"].Value;
				text = CleanString(text, false);

				text = _unicBank.StringToUnicode(text, game);

				result.Add(new LocalizationPair(tagpath + "_" + name, text));

			}

			return result;
		}

		private string GetTagPath(string path)
		{
			string[] dirs = path.Replace(".hmt", "").Replace(".txt", "").Split('\\');
			string result = "";
			bool found = false;

			for (int i = 0; i < dirs.Length; i++)
			{
				if (!found && dirs[i].ToLower().StartsWith("data"))
					found = true;
				else if (found)
				{
					result = string.Join("\\", dirs, i, dirs.Length - i);
					break;
				}

			}

			if (!found)
				return null;

			return result;
		}

		private string CleanString(string text, bool h1)
		{
			string result = text;
			if (h1)
			{
				result = text.Replace("»", ">>");
				result = Regex.Replace(result, @"\|n\s+", " ");
			}

			return result.Replace("|n", "\n").Replace("’", "'").Replace("\\?", "?");
		}

		private string FormatSpecialHUDVehicleMessage(string hmt)
		{
			//special case where the variables are just {PARAM} and not split up
			string runningstring = hmt;
			while (true)
			{
				int index = runningstring.IndexOf("%");
				if (index == -1)
					break;

				string startstring = runningstring.Substring(0, index);
				runningstring = runningstring.Substring(index);

				foreach (string k in HmtKeys)
				{
					if (runningstring.StartsWith(k, StringComparison.OrdinalIgnoreCase))
					{
						runningstring = startstring + "{PARAM}" + runningstring.Substring(k.Length);
						break;
					}
				}
			}

			return runningstring;
		}
		private List<LocalizationPair> HandleHUDMessage(string key, string hmt, bool vehicle, int elementoffset = 0)
		{
			List<LocalizationPair> result = new List<LocalizationPair>();

			int elementindex = elementoffset;
			string runningstring = hmt;
			while (true)
			{
				int index = runningstring.IndexOf("%");
				if (index == -1)
				{
					result.Add(new LocalizationPair(key + "_" + elementindex, runningstring));
					elementindex++;
					break;
				}

				string startstring = runningstring.Substring(0, index);
				result.Add(new LocalizationPair(key + "_" + elementindex, startstring));
				elementindex++;

				if (vehicle && startstring.Contains("driver seat") || startstring.Contains("gunner seat") || startstring.Contains("side seat"))
				{
					//extra padding possibly due to how they originally handled the extra seat strings
					result.Add(new LocalizationPair(key + "_" + elementindex, ""));
					elementindex++;
					result.Add(new LocalizationPair(key + "_" + elementindex, ""));
					elementindex++;
				}

				runningstring = runningstring.Substring(index);

				foreach (string k in HmtKeys)
				{
					if (runningstring.StartsWith(k, StringComparison.OrdinalIgnoreCase))
					{
						runningstring = runningstring.Substring(k.Length);

						//skip an element for the symbol
						elementindex++;
					}
				}
			}

			return result;
		}
	}
}
