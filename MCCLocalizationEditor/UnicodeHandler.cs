using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MCCLocalizationEditor
{
	public enum UnicodeGame
	{
		None,
		H1,
		H2,
		H3,
		H3ODST,
		HReach,
		H4,
		H2AMP,
	}

	public class UnicodeBank
	{
		Dictionary<UnicodeGame, UnicodeCollection> _games;
		static Dictionary<string, UnicodeGame> _gameNames = new Dictionary<string, UnicodeGame>()
		{
			{ "H2MCC", UnicodeGame.H2 },
			{ "H3MCC", UnicodeGame.H3 },
			{ "H3ODSTMCC", UnicodeGame.H3ODST },
			{ "HReachMCC", UnicodeGame.HReach },
			{ "H4MCC", UnicodeGame.H4 },
			{ "H2AMPMCC", UnicodeGame.H2AMP },
		};

		public bool Initialized { get; private set; }
		public string Error { get; private set; }

		public UnicodeBank(string definitionPath)
		{
			Initialized = false;
			_games = new Dictionary<UnicodeGame, UnicodeCollection>();
			if (!Directory.Exists(definitionPath))
			{
				Error = "No \\unicode subdirectory found! Please check your download as it should have been included.";
				return;
			}
			string[] files = Directory.GetFiles(definitionPath);

			_games[UnicodeGame.None] = new UnicodeCollection(null);

			if (files.Length == 0)
			{
				Error = "No unicode XMLs found! Please check your download as it should have been included.";
				return;
			}

			foreach (string file in files)
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(file);

				if (doc["symbols"] == null)
				{
					Error = $"XML file {Path.GetFileName(file)} does not contain a <symbols> node!";
					return;
				}

				string game = doc["symbols"].GetAttribute("game");
				if (string.IsNullOrEmpty(game))
				{
					Error = $"XML file {Path.GetFileName(file)} does not contain a game name in its <symbols> node!";
					return;
				}

				if (!_gameNames.ContainsKey(game))
				{
					Error = $"XML file {Path.GetFileName(file)} does not contain a valid game name in its <symbols> node!";
					return;
				}

				UnicodeGame ugame = _gameNames[game];

				if (_games.ContainsKey(ugame))
				{
					Error = $"Game name {game} is defined twice in XMLs!";
					return;
				}

				_games[ugame] = new UnicodeCollection(doc);
			}

			if (!_games.ContainsKey(UnicodeGame.H2))
			{
				Error = "No unicode symbol XML for Halo 2 available!";
				return;
			}
			if (!_games.ContainsKey(UnicodeGame.H3))
			{
				Error = "No unicode symbol XML for Halo 3 available!";
				return;
			}
			if (!_games.ContainsKey(UnicodeGame.H3ODST))
			{
				Error = "No unicode symbol XML for Halo 3 ODST available!";
				return;
			}
			if (!_games.ContainsKey(UnicodeGame.HReach))
			{
				Error = "No unicode symbol XML for Halo Halo Reach available!";
				return;
			}
			if (!_games.ContainsKey(UnicodeGame.H4))
			{
				Error = "No unicode symbol XML for Halo 4 available!";
				return;
			}
			if (!_games.ContainsKey(UnicodeGame.H2AMP))
			{
				Error = "No unicode symbol XML for Halo 2A MP available!";
				return;
			}

			Initialized = true;
		}

		public string StringToUnicode(string locString, UnicodeGame game)
		{
			if (game < UnicodeGame.H2)
				return locString;
			else
				return _games[game].StringsToUnicode(locString);
		}

		public UnicodeCollection WithdrawCollection(UnicodeGame game)
		{
			if (!_games.ContainsKey(game))
				return null;

			return _games[game];
		}
	}

	public class UnicodeCollection
	{
		private Dictionary<ushort, string> _symbolsByCode;
		private Dictionary<string, ushort> _symbolsByString;

		public UnicodeCollection(XmlDocument doc)
		{
			_symbolsByCode = new Dictionary<ushort, string>();
			_symbolsByString = new Dictionary<string, ushort>();

			if (doc == null)
				return;

			XmlNodeList symbols = doc["symbols"].ChildNodes;

			foreach (XmlNode symbol in symbols)
			{
				if (symbol.Name != "symbol" ||
					symbol.Attributes["unicode"] == null ||
					symbol.Attributes["display"] == null)
					continue;

				string unic = symbol.Attributes["unicode"].Value;
				string display = "&" + symbol.Attributes["display"].Value;

				if (unic.StartsWith("0x"))
					unic = unic.Substring(2);

				if (!ushort.TryParse(unic, System.Globalization.NumberStyles.HexNumber, null, out ushort code))
					continue;

				_symbolsByCode[code] = display;
				_symbolsByString[display] = code;
			}
		}

		public string StringsToUnicode(string locString)
		{
			string result = locString;
			foreach (string s in _symbolsByString.Keys)
			{
				result = result.Replace(s, Convert.ToChar(_symbolsByString[s]).ToString());
			}

			return result;
		}

		public string UnicodeToStrings(string locString)
		{
			string result = locString;
			foreach (ushort c in _symbolsByCode.Keys)
			{
				result = result.Replace(Convert.ToChar(c).ToString(), _symbolsByCode[c]);
			}

			return result;
		}
	}
}
