using System.IO;

namespace MCCLocalizationEditor
{
	public static class ReaderExtender
	{
		public static string ReadStringToNull(this BinaryReader br, int limit = -1)
		{
			string output = "";
			char c;

			if (limit == -1)
				limit = (int)br.BaseStream.Length - (int)br.BaseStream.Position;

			for (int j = 0; j < limit; j++)
			{
				c = br.ReadChar();
				if (c == 0)
					break;

				output += c.ToString();
			}

			return output;
		}

	}
}
