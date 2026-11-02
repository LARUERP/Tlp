using System;

namespace BonelabRP.Telephone;

public static class PhoneMediaRules
{
	public const int ChunkBytes = 600;

	public const int PhotoBytes = 65536;

	public const int VideoBytes = 16384;

	public static bool Voice(string kind)
	{
		return kind == "voiceUpload" || kind == "voiceDownload";
	}

	public static bool Stream(string kind)
	{
		return kind == "video" || kind == "live";
	}

	public static bool ValidChunk(PhoneMediaChunk c)
	{
		if (c == null || c.Protocol != 16 || !Guid.TryParseExact(c.Session, "N", out var result) || !Guid.TryParseExact(c.Id, "N", out result) || c.Bytes == null)
		{
			return false;
		}
		if (c.Kind != "upload" && c.Kind != "download" && c.Kind != "video" && c.Kind != "live" && !Voice(c.Kind))
		{
			return false;
		}
		int num = (Voice(c.Kind) ? 1440016 : (Stream(c.Kind) ? 16384 : 65536));
		if (Stream(c.Kind) && (!Guid.TryParseExact(c.Call, "N", out result) || c.Frame == 0))
		{
			return false;
		}
		if (c.Total < 16 || c.Total > num || c.Count != (c.Total + 600 - 1) / 600 || c.Index < 0 || c.Index >= c.Count)
		{
			return false;
		}
		return c.Bytes.Length == Math.Min(600, c.Total - c.Index * 600);
	}

	public static PhoneMediaChunk[] Split(string session, string id, string kind, byte[] bytes, string call = "", uint frame = 0u)
	{
		if (bytes == null || bytes.Length < 16 || bytes.Length > (Voice(kind) ? 1440016 : (Stream(kind) ? 16384 : 65536)))
		{
			throw new InvalidOperationException("Image absente ou trop volumineuse.");
		}
		int num = (bytes.Length + 600 - 1) / 600;
		PhoneMediaChunk[] array = new PhoneMediaChunk[num];
		for (int i = 0; i < num; i++)
		{
			PhoneMediaChunk phoneMediaChunk = new PhoneMediaChunk
			{
				Session = session,
				Id = id,
				Kind = kind,
				Call = call,
				Frame = frame,
				Index = i,
				Count = num,
				Total = bytes.Length,
				Bytes = bytes.AsSpan(i * 600, Math.Min(600, bytes.Length - i * 600)).ToArray()
			};
			if (!ValidChunk(phoneMediaChunk))
			{
				throw new InvalidOperationException("Image trop volumineuse ou enveloppe invalide.");
			}
			array[i] = phoneMediaChunk;
		}
		return array;
	}

	public static bool Jpeg(byte[] bytes, int maxWidth = 640, int maxHeight = 480)
	{
		if (bytes == null || bytes.Length < 16 || bytes.Length > 65536 || bytes[0] != byte.MaxValue || bytes[1] != 216 || bytes[^2] != byte.MaxValue || bytes[^1] != 217)
		{
			return false;
		}
		int i = 2;
		bool result = false;
		while (i + 1 < bytes.Length)
		{
			if (bytes[i++] != byte.MaxValue)
			{
				return false;
			}
			for (; i < bytes.Length && bytes[i] == byte.MaxValue; i++)
			{
			}
			if (i >= bytes.Length)
			{
				return false;
			}
			int num = bytes[i++];
			switch (num)
			{
			case 218:
				return result;
			default:
				if (num != 0)
				{
					if (num >= 208 && num <= 215)
					{
						break;
					}
					if (i + 2 > bytes.Length)
					{
						return false;
					}
					int num2 = (bytes[i] << 8) | bytes[i + 1];
					if (num2 < 2 || i + num2 > bytes.Length)
					{
						return false;
					}
					if (num >= 192 && num <= 207 && num != 192 && num != 194 && num != 196 && num != 200 && num != 204)
					{
						return false;
					}
					if (num == 192 || num == 194)
					{
						if (num2 < 8 || bytes[i + 2] != 8)
						{
							return false;
						}
						int num3 = (bytes[i + 3] << 8) | bytes[i + 4];
						int num4 = (bytes[i + 5] << 8) | bytes[i + 6];
						if (num4 < 1 || num3 < 1 || num4 > maxWidth || num3 > maxHeight)
						{
							return false;
						}
						result = true;
					}
					i += num2;
					break;
				}
				goto case 216;
			case 216:
			case 217:
				return false;
			}
		}
		return false;
	}
}
