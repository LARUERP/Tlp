using System;

namespace BonelabRP.Telephone;

public static class PhoneStreamPcm
{
	public const int Rate = 48000;

	public const int FrameSamples = 480;

	public static byte[] Pack(short[] samples, int rate = 48000)
	{
		if ((rate != 16000 && rate != 32000 && rate != 48000) || samples == null || samples.Length != rate / 100)
		{
			throw new ArgumentException("Trame PCM invalide.");
		}
		byte[] array = new byte[8 + samples.Length * 2];
		array[0] = 87;
		array[1] = 80;
		array[2] = 67;
		array[3] = 51;
		BitConverter.GetBytes(rate).CopyTo(array, 4);
		Buffer.BlockCopy(samples, 0, array, 8, samples.Length * 2);
		return array;
	}

	public static bool Valid(byte[] b)
	{
		if (b == null || b.Length < 8 || b[0] != 87 || b[1] != 80 || b[2] != 67 || b[3] != 51)
		{
			return false;
		}
		int num = BitConverter.ToInt32(b, 4);
		return (num == 16000 || num == 32000 || num == 48000) && b.Length == 8 + num / 100 * 2;
	}

	public static short[] Decode(byte[] b, int outputRate)
	{
		if (!Valid(b) || outputRate < 8000 || outputRate > 192000)
		{
			throw new ArgumentException("Trame PCM invalide.");
		}
		int num = BitConverter.ToInt32(b, 4);
		short[] array = new short[num / 100];
		Buffer.BlockCopy(b, 8, array, 0, array.Length * 2);
		int num2 = outputRate / 100;
		short[] array2 = new short[num2];
		for (int i = 0; i < num2; i++)
		{
			double num3 = (double)i * (double)num / (double)outputRate;
			int num4 = (int)num3;
			int num5 = Math.Min(num4 + 1, array.Length - 1);
			array2[i] = (short)((double)array[num4] + (double)(array[num5] - array[num4]) * (num3 - (double)num4));
		}
		return array2;
	}
}
