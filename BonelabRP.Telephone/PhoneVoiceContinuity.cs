using System;

namespace BonelabRP.Telephone;

public sealed class PhoneVoiceContinuity
{
	private float last;

	private bool recovery;

	public short[] Prepare(short[] pcm)
	{
		if (pcm == null || pcm.Length == 0)
		{
			return pcm;
		}
		if (recovery)
		{
			short[] array = (short[])pcm.Clone();
			int num = Math.Min(array.Length, 240);
			for (int i = 0; i < num; i++)
			{
				array[i] = (short)((float)(array[i] * i) / (float)num);
			}
			pcm = array;
			recovery = false;
		}
		last = pcm[^1];
		return pcm;
	}

	public short[] Fill(int count)
	{
		count = Math.Clamp(count, 0, 38400);
		short[] array = new short[count];
		for (int i = 0; i < count; i++)
		{
			last *= 0.98f;
			array[i] = (short)last;
		}
		if (count > 0)
		{
			recovery = true;
		}
		return array;
	}

	public static float Limit(float sample)
	{
		if (!float.IsFinite(sample))
		{
			return 0f;
		}
		float num = Math.Abs(sample);
		return (num <= 0.8f) ? sample : ((float)Math.Sign(sample) * (0.8f + 0.16f * (float)Math.Tanh((num - 0.8f) / 0.16f)));
	}
}
