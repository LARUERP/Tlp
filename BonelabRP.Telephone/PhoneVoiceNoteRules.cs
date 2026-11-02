using System;
using System.IO;

namespace BonelabRP.Telephone;

public static class PhoneVoiceNoteRules
{
	public const int Rate = 8000;

	public const int Seconds = 15;

	public const int HighRate = 48000;

	public const int MaxBytes = 1440016;

	public static bool Valid(byte[] b)
	{
		if (b == null || b.Length < 8016 || b.Length > 1440016 || b[0] != 87 || b[1] != 78 || b[2] != 86 || b[3] != 49)
		{
			return false;
		}
		int num = BitConverter.ToInt32(b, 4);
		int num2 = BitConverter.ToInt32(b, 8);
		return (num == 8000 || num == 16000 || num == 32000 || num == 48000) && BitConverter.ToInt32(b, 12) == 0 && num2 >= num / 2 && num2 <= num * 15 && b.Length == 16 + num2 * 2;
	}

	public static byte[] Pack(short[] samples, int rate = 8000)
	{
		if ((rate != 8000 && rate != 16000 && rate != 32000 && rate != 48000) || samples == null || samples.Length < rate / 2 || samples.Length > rate * 15)
		{
			throw new InvalidOperationException("Vocal : 0,5 à 15 secondes.");
		}
		byte[] array = new byte[16 + samples.Length * 2];
		array[0] = 87;
		array[1] = 78;
		array[2] = 86;
		array[3] = 49;
		BitConverter.GetBytes(rate).CopyTo(array, 4);
		BitConverter.GetBytes(samples.Length).CopyTo(array, 8);
		Buffer.BlockCopy(samples, 0, array, 16, samples.Length * 2);
		return array;
	}

	public static float Duration(byte[] b)
	{
		return Valid(b) ? ((float)(b.Length - 16) / (2f * (float)SampleRate(b))) : 0f;
	}

	public static int SampleRate(byte[] b)
	{
		if (!Valid(b))
		{
			throw new InvalidDataException("Vocal invalide.");
		}
		return BitConverter.ToInt32(b, 4);
	}

	public static float[] Decode(byte[] b)
	{
		if (!Valid(b))
		{
			throw new InvalidDataException("Vocal invalide.");
		}
		float[] array = new float[(b.Length - 16) / 2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (float)BitConverter.ToInt16(b, 16 + i * 2) / 32768f;
		}
		return array;
	}
}
