using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneMediaStore
{
	private readonly string folder;

	private readonly int limit;

	private readonly Dictionary<ulong, string[]> capturedCache = new Dictionary<ulong, string[]>();

	public PhoneMediaStore(string folder, int limit = 1000)
	{
		this.folder = folder;
		this.limit = limit;
		Directory.CreateDirectory(folder);
	}

	public string PathFor(string id)
	{
		if (!Guid.TryParseExact(id, "N", out var _))
		{
			throw new InvalidOperationException("Identifiant photo invalide.");
		}
		return Path.Combine(folder, id + ".jpg");
	}

	public bool Owns(ulong owner, string id)
	{
		string text = PathFor(id);
		return File.Exists(text) && File.Exists(text + ".owner") && File.ReadAllText(text + ".owner") == owner.ToString(CultureInfo.InvariantCulture);
	}

	public void Save(string id, ulong owner, byte[] data)
	{
		if (!PhoneMediaRules.Jpeg(data))
		{
			throw new InvalidOperationException("JPEG invalide ou dimensions excessives.");
		}
		string text = PathFor(id);
		if (File.Exists(text))
		{
			if (!Owns(owner, id) || !Read(id).SequenceEqual(data))
			{
				throw new InvalidOperationException("Identifiant photo déjà utilisé.");
			}
			return;
		}
		if (Directory.EnumerateFiles(folder, "*.jpg").Take(limit).Count() >= limit)
		{
			throw new InvalidOperationException("Stockage photo plein.");
		}
		string text2 = text + ".tmp";
		File.WriteAllBytes(text2, data);
		try
		{
			File.WriteAllText(text + ".owner", owner.ToString(CultureInfo.InvariantCulture));
			File.Move(text2, text);
			capturedCache.Clear();
		}
		catch
		{
			if (!File.Exists(text) && File.Exists(text + ".owner"))
			{
				File.Delete(text + ".owner");
			}
			throw;
		}
		finally
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
		}
	}

	public byte[] Read(string id)
	{
		string text = PathFor(id);
		if (!File.Exists(text) || new FileInfo(text).Length > 65536)
		{
			throw new InvalidOperationException("Photo indisponible.");
		}
		return File.ReadAllBytes(text);
	}

	public string[] List()
	{
		Guid result;
		return (from id in Directory.EnumerateFiles(folder, "*.jpg").Select(Path.GetFileNameWithoutExtension)
			where Guid.TryParseExact(id, "N", out result)
			orderby File.GetLastWriteTimeUtc(PathFor(id)) descending
			select id).Take(limit).ToArray();
	}

	public void Prune(Func<string, bool> referenced, DateTime olderThanUtc)
	{
		string[] array = List();
		foreach (string text in array)
		{
			if (File.GetLastWriteTimeUtc(PathFor(text)) < olderThanUtc && !referenced(text))
			{
				Delete(text);
			}
		}
	}

	public void MarkCaptured(string id, ulong owner)
	{
		if (!Owns(owner, id))
		{
			throw new InvalidOperationException("Photo source absente.");
		}
		File.WriteAllText(PathFor(id) + ".captured", owner.ToString(CultureInfo.InvariantCulture));
		capturedCache.Clear();
	}

	public string[] Captured(ulong owner)
	{
		if (!capturedCache.TryGetValue(owner, out var value))
		{
			value = (from id in List()
				where Owns(owner, id) && File.Exists(PathFor(id) + ".captured") && File.ReadAllText(PathFor(id) + ".captured") == owner.ToString(CultureInfo.InvariantCulture)
				select id).ToArray();
			capturedCache[owner] = value;
		}
		return (string[])value.Clone();
	}

	public void UnmarkCaptured(string id)
	{
		File.Delete(PathFor(id) + ".captured");
		capturedCache.Clear();
	}

	public int OwnedCount(ulong owner)
	{
		return List().Count((string id) => Owns(owner, id));
	}

	public DateTime Date(string id)
	{
		return File.GetLastWriteTime(PathFor(id));
	}

	public void Delete(string id)
	{
		string text = PathFor(id);
		File.Delete(text);
		File.Delete(text + ".owner");
		File.Delete(text + ".captured");
		capturedCache.Clear();
	}
}
