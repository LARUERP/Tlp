using System;
using System.Collections.Generic;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneAudioBuffer
{
	private readonly SortedDictionary<uint, (byte[] Bytes, double At)> pending = new SortedDictionary<uint, (byte[], double)>();

	private uint played;

	public int Count => pending.Count;

	public bool Push(uint sequence, byte[] bytes, double now)
	{
		if (sequence == 0 || sequence <= played || pending.ContainsKey(sequence) || bytes == null || bytes.Length == 0 || pending.Count >= 32)
		{
			return false;
		}
		pending.Add(sequence, (bytes, now));
		return true;
	}

	public void Drain(double now, Action<byte[]> play, Action<int> missing = null)
	{
		while (pending.Count > 0)
		{
			KeyValuePair<uint, (byte[], double)> keyValuePair = pending.First();
			if (now - keyValuePair.Value.Item2 < 0.08)
			{
				break;
			}
			pending.Remove(keyValuePair.Key);
			if (played != 0 && keyValuePair.Key > played + 1)
			{
				missing?.Invoke((int)Math.Min(20u, keyValuePair.Key - played - 1));
			}
			played = keyValuePair.Key;
			play(keyValuePair.Value.Item1);
		}
	}

	public void Clear()
	{
		pending.Clear();
		played = 0u;
	}

	public static float Limit(float sample)
	{
		return (float.IsNaN(sample) || float.IsInfinity(sample)) ? 0f : Math.Max(-0.96f, Math.Min(0.96f, sample));
	}
}
