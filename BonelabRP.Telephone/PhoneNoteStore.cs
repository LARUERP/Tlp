using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneNoteStore
{
	private readonly string folder;

	private readonly int ownerLimit;

	private readonly int totalLimit;

	public PhoneNoteStore(string folder, int ownerLimit = 100, int totalLimit = 1000)
	{
		this.ownerLimit = ownerLimit;
		this.totalLimit = totalLimit;
		this.folder = folder;
		Directory.CreateDirectory(folder);
	}

	private string PathFor(string id, string suffix)
	{
		if (!Guid.TryParseExact(id, "N", out var _))
		{
			throw new InvalidOperationException("Vocal invalide.");
		}
		return Path.Combine(folder, id + suffix);
	}

	public bool Owns(ulong owner, string id)
	{
		string path = PathFor(id, ".owner");
		return File.Exists(path) && File.ReadAllText(path) == owner.ToString(CultureInfo.InvariantCulture);
	}

	public void Save(ulong owner, string id, byte[] bytes)
	{
		if (!PhoneVoiceNoteRules.Valid(bytes))
		{
			throw new InvalidDataException("Vocal invalide.");
		}
		string text = PathFor(id, ".note");
		string text2 = PathFor(id, ".owner");
		if (File.Exists(text))
		{
			if (!Owns(owner, id) || !Read(id).SequenceEqual(bytes))
			{
				throw new InvalidOperationException("Vocal déjà attribué.");
			}
			return;
		}
		string[] files = Directory.GetFiles(folder, "*.owner");
		if (files.Length >= totalLimit || files.Count((string f) => File.ReadAllText(f) == owner.ToString(CultureInfo.InvariantCulture)) >= ownerLimit)
		{
			throw new InvalidOperationException("Stockage des vocaux plein.");
		}
		try
		{
			File.WriteAllBytes(text + ".tmp", bytes);
			File.WriteAllText(text2 + ".tmp", owner.ToString(CultureInfo.InvariantCulture));
			File.Move(text + ".tmp", text);
			File.Move(text2 + ".tmp", text2);
		}
		catch
		{
			string[] array = new string[4]
			{
				text + ".tmp",
				text2 + ".tmp",
				text,
				text2
			};
			foreach (string path in array)
			{
				try
				{
					if (File.Exists(path))
					{
						File.Delete(path);
					}
				}
				catch
				{
				}
			}
			throw;
		}
	}

	public void Forget(string id)
	{
		File.Delete(PathFor(id, ".note"));
		File.Delete(PathFor(id, ".owner"));
	}

	public bool TryRead(string id, out byte[] bytes)
	{
		bytes = null;
		try
		{
			bytes = Read(id);
			return true;
		}
		catch (InvalidDataException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	public byte[] Read(string id)
	{
		string text = PathFor(id, ".note");
		if (!File.Exists(text) || new FileInfo(text).Length > 1440016)
		{
			throw new InvalidDataException("Vocal indisponible.");
		}
		byte[] array = File.ReadAllBytes(text);
		if (!PhoneVoiceNoteRules.Valid(array))
		{
			throw new InvalidDataException("Vocal invalide.");
		}
		return array;
	}
}
