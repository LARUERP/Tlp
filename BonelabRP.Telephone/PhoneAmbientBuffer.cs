using System;
using System.Collections.Generic;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneAmbientBuffer
{
	private sealed class Track
	{
		public readonly Queue<short> Samples = new Queue<short>();

		public double Last;

		public int Rate;

		public float Gain;
	}

	private readonly Dictionary<ulong, Track> tracks = new Dictionary<ulong, Track>();

	public void Clear()
	{
		tracks.Clear();
	}

	public void Add(ulong actor, short[] pcm, int rate, float gain, double now)
	{
		if (pcm == null || rate < 8000 || rate > 192000 || !float.IsFinite(gain) || gain <= 0f)
		{
			return;
		}
		if (!tracks.TryGetValue(actor, out var value))
		{
			if (tracks.Count >= 8)
			{
				return;
			}
			value = (tracks[actor] = new Track());
		}
		if (value.Rate != rate)
		{
			value.Samples.Clear();
		}
		value.Rate = rate;
		value.Gain = Math.Clamp(gain, 0f, 0.7f);
		value.Last = now;
		int num = Math.Min(pcm.Length, rate / 4);
		while (value.Samples.Count + num > rate / 4)
		{
			value.Samples.Dequeue();
		}
		for (int i = pcm.Length - num; i < pcm.Length; i++)
		{
			value.Samples.Enqueue(pcm[i]);
		}
	}

	public short[] Mix(short[] own, int rate, double now, Func<ulong, bool> allowed)
	{
		ulong[] array = tracks.Keys.ToArray();
		foreach (ulong num in array)
		{
			if (now - tracks[num].Last > 0.3 || !allowed(num) || tracks[num].Rate != rate)
			{
				tracks.Remove(num);
			}
		}
		if (tracks.Count == 0)
		{
			return own;
		}
		short[] array2 = new short[own.Length];
		for (int j = 0; j < own.Length; j++)
		{
			double num2 = (double)own[j] / 32768.0;
			foreach (Track value in tracks.Values)
			{
				if (value.Samples.Count > 0)
				{
					num2 += (double)value.Samples.Dequeue() / 32768.0 * (double)value.Gain;
				}
			}
			double num3 = Math.Abs(num2);
			if (num3 > 0.85)
			{
				num2 = (double)Math.Sign(num2) * (0.85 + 0.15 * Math.Tanh((num3 - 0.85) / 0.15));
			}
			array2[j] = (short)Math.Clamp(num2 * 32767.0, -32767.0, 32767.0);
		}
		return array2;
	}
}
