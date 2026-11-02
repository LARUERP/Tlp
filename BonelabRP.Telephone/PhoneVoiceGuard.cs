using System.IO;
using System.IO.Compression;

namespace BonelabRP.Telephone;

public static class PhoneVoiceGuard
{
	public const int MaxFrame = 49152;

	public const int MaxSamples = 96000;

	public static bool Valid(byte[] bytes)
	{
		if (bytes == null || bytes.Length == 0 || bytes.Length > 49152)
		{
			return false;
		}
		try
		{
			using MemoryStream stream = new MemoryStream(bytes);
			using DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
			byte[] array = new byte[4096];
			int num = 0;
			int num2;
			while ((num2 = deflateStream.Read(array, 0, array.Length)) > 0)
			{
				num += num2;
				if (num > 96000)
				{
					return false;
				}
			}
			return num > 0;
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
}
