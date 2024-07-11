﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace MCCLocalizationEditor
{
	public static class LocalizationData
	{
		static readonly uint _magic = 0x90ce6b7a;

		public static List<LocalizationPair> ReadBinFile(string path)
		{
			byte[] loc = File.ReadAllBytes(path);

			List<LocalizationPair> contents = new List<LocalizationPair>();

			using (MemoryStream ms = new MemoryStream(loc))
			{
				using (BinaryReader br = new BinaryReader(ms))
				{
					uint fileMagic = br.ReadUInt32();
					if (fileMagic != _magic)
						return null;

					ms.Seek(0x28, SeekOrigin.Begin);
					uint fileNameHash = br.ReadUInt32();

					ms.Seek(0x44, SeekOrigin.Begin);
					string intName = br.ReadStringToNull(0x20);

					ms.Seek(0x78, SeekOrigin.Begin);

					uint indexLength = br.ReadUInt32();
					uint stringLength = br.ReadUInt32();

					uint dataStart = (uint)ms.Position;

					uint indexEnd = dataStart + indexLength;

					List<uint> keys = new List<uint>();

					while (ms.Position < indexEnd)
					{
						keys.Add(br.ReadUInt32());
					}

					ms.Position = indexEnd;

					for (int i = 0; i < keys.Count; i++)
					{
						contents.Add(new LocalizationPair(keys[i], br.ReadStringToNull()));
					}
				}
			}

			return contents;
		}

		public static void SaveBinFile(string path, List<LocalizationPair> strings)
		{
			string filename = Path.GetFileNameWithoutExtension(path);

			MemoryStream indexStream = new MemoryStream();
			MemoryStream stringStream = new MemoryStream();
			FileStream fs = new FileStream(path, FileMode.Create);

			BinaryWriter indexWriter = new BinaryWriter(indexStream);
			BinaryWriter stringWriter = new BinaryWriter(stringStream);
			BinaryWriter bw = new BinaryWriter(fs);

			foreach (LocalizationPair entry in strings)
			{
				indexWriter.Write(entry.KeyHash);
				byte[] text = Encoding.UTF8.GetBytes(entry.String);
				stringWriter.Write(text);
				stringWriter.Write((byte)0);
			}

			uint datasize = (uint)indexStream.Length + (uint)stringStream.Length;

			byte[] capsName = Encoding.UTF8.GetBytes(filename.ToUpper());
			uint hash = CRC32MPEG.CountCRC(capsName);

			bw.Write(_magic);

			fs.Seek(0x28, SeekOrigin.Begin);
			bw.Write(hash);

			fs.Seek(0x40, SeekOrigin.Begin);
			bw.Write(_magic);

			byte[] name = Encoding.UTF8.GetBytes(filename);
			bw.Write(name);

			fs.Seek(0x68, SeekOrigin.Begin);
			bw.Write(datasize + 0x8);

			fs.Seek(0x70, SeekOrigin.Begin);
			bw.Write(8);

			fs.Seek(0x78, SeekOrigin.Begin);
			bw.Write((uint)indexStream.Length);
			bw.Write((uint)stringStream.Length);

			bw.Write(indexStream.ToArray());
			bw.Write(stringStream.ToArray());

			int rem = (int)fs.Length % 0x100;
			if (rem != 0)
			{
				fs.Seek(0x100 - rem - 1, SeekOrigin.Current);
				bw.Write((byte)0);
			}

			fs.Seek(0x4, SeekOrigin.Begin);
			bw.Write((int)fs.Length);
			bw.Write((int)fs.Length);

			indexWriter.Close();
			stringWriter.Close();
			bw.Close();
		}

		public static List<LocalizationPair> ReadXMLFile(string path, UnicodeCollection unicodes)
		{
			XmlDocument input = new XmlDocument();
			input.Load(path);

			XmlNodeList entries = input["localization"].ChildNodes;

			List<LocalizationPair> contents = new List<LocalizationPair>();

			foreach (XmlNode entry in entries)
			{
				string content = entry.InnerText;

				//check for known weird characters and unescape them
				string refilt = content.Replace("\\a", "\a")
					.Replace("\\b", "\b")
					.Replace("\\f", "\f");

				refilt = unicodes.StringsToUnicode(refilt);

				if (entry.Attributes["key"] != null)
				{
					string keyVal = entry.Attributes["key"].Value;
					contents.Add(new LocalizationPair(keyVal, refilt));
				}
				else
				{
					string idVal = entry.Attributes["keyHash"].Value;
					uint id = uint.Parse(idVal);
					contents.Add(new LocalizationPair(id, refilt));
				}

			}

			return contents;
		}

		public static void SaveXMLFile(string path, List<LocalizationPair> strings, UnicodeCollection unicodes)
		{
			XmlWriterSettings sett = new XmlWriterSettings()
			{
				Indent = true,
				NewLineChars = "\n",
				NewLineHandling = NewLineHandling.None,
				IndentChars = "\t"
			};

			using (FileStream fs = new FileStream(path, FileMode.Create))
			{
				using (XmlWriter writer = XmlWriter.Create(fs, sett))
				{
					writer.WriteStartDocument();
					writer.WriteComment("XML file generated by MCC Localization Editor created by Zeddikins.\r\n" +
						"When adding strings, you can use a \"key\" attribute to input a key string that will automatically get hashed for you when importing back to MCC Localization Editor.\r\n" +
						"Example: <entry xml:space=\"preserve\" key=\"my_cool_string\">Look ma, no hash!</entry>");

					writer.WriteStartElement("localization");

					if (strings == null)
					{
						writer.WriteStartElement("entry");
						writer.WriteAttributeString("xml", "space", null, "preserve");
						writer.WriteAttributeString("key", "my_cool_string");

						writer.WriteValue("Look ma, no hash!");
						writer.WriteEndElement();
					}
					else
					{
						foreach (LocalizationPair entry in strings)
						{
							writer.WriteStartElement("entry");
							writer.WriteAttributeString("xml", "space", null, "preserve");
							writer.WriteAttributeString("keyHash", entry.KeyHash.ToString());
							//check for known weird characters and escape them
							string filt = entry.String.Replace("\a", "\\a")
								.Replace("\b", "\\b")
								.Replace("\f", "\\f");
							filt = unicodes.UnicodeToStrings(filt);
							writer.WriteValue(filt);
							writer.WriteEndElement();
						}
					}

					writer.WriteEndElement();
					writer.WriteEndDocument();
				}
			}
		}

	}

}
