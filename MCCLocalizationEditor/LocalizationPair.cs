using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MCCLocalizationEditor
{
	public class LocalizationPair : INotifyPropertyChanged
	{
		private uint _keyHash;
		private string _string;
		public uint KeyHash
		{
			get { return _keyHash; }
			set
			{
				_keyHash = value;
				OnPropertyChanged();
			}
		}
		public string String
		{
			get { return _string; }
			set
			{
				_string = value;
				OnPropertyChanged();
			}
		}

		public LocalizationPair(string key, string value)
		{
			KeyHash = CRC32MPEG.CountCRC(Encoding.ASCII.GetBytes(key.ToUpper()));
			String = value;
		}

		public LocalizationPair(uint key, string value)
		{
			KeyHash = key;
			String = value;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
