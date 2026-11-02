using System;
using System.IO;

namespace BonelabRP.Telephone;

public static class PhoneMicrophoneRules
{
	public static int Available(int cursor, int position, int capacity)
	{
		if (capacity <= 0 || cursor < 0 || position < 0 || cursor >= capacity || position >= capacity)
		{
			return 0;
		}
		return (position >= cursor) ? (position - cursor) : (capacity - cursor + position);
	}

	public static short[] Mono(float[] raw, int channels)
	{
		if (raw == null || channels < 1 || channels > 8 || raw.Length % channels != 0)
		{
			throw new InvalidDataException("Micro invalide.");
		}
		short[] array = new short[raw.Length / channels];
		for (int i = 0; i < array.Length; i++)
		{
			double num = 0.0;
			for (int j = 0; j < channels; j++)
			{
				float num2 = raw[i * channels + j];
				if (float.IsFinite(num2))
				{
					num += (double)Math.Clamp(num2, -1f, 1f);
				}
			}
			array[i] = (short)Math.Clamp(num / (double)channels * 32767.0, -32767.0, 32767.0);
		}
		return array;
	}

	public static float Peak(short[] pcm)
	{
		int num = 0;
		foreach (short value in pcm)
		{
			num = Math.Max(num, Math.Abs((int)value));
		}
		return (float)num / 32768f;
	}

	public static byte[] Audible(byte[] bytes)
	{
		float[] array = PhoneVoiceNoteRules.Decode(bytes);
		float num = 0f;
		float[] array2 = array;
		foreach (float value in array2)
		{
			num = Math.Max(num, Math.Abs(value));
		}
		if (num < 0.001f)
		{
			throw new InvalidOperationException("Aucune voix capturée. Vérifier le micro Fusion puis réenregistrer.");
		}
		float num2 = Math.Min(4f, 0.85f / num);
		short[] array3 = new short[array.Length];
		for (int j = 0; j < array3.Length; j++)
		{
			array3[j] = (short)Math.Clamp(array[j] * num2 * 32767f, -32767f, 32767f);
		}
		int num3 = PhoneVoiceNoteRules.SampleRate(bytes);
		int num4 = num3 / 100;
		for (int k = 0; k < Math.Min(num4, array3.Length / 2); k++)
		{
			array3[k] = (short)((float)(array3[k] * k) / (float)num4);
			array3[array3.Length - 1 - k] = (short)((float)(array3[array3.Length - 1 - k] * k) / (float)num4);
		}
		return PhoneVoiceNoteRules.Pack(array3, num3);
	}
}
