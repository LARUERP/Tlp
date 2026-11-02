using System;
using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class PhoneStreamEncoder
{
	private readonly List<short> frame = new List<short>();

	private PhonePcmResampler resampler;

	private int sourceRate;

	private int wireRate;

	public void Push(short[] pcm, int inputRate, Action<byte[]> send)
	{
		if (pcm == null || inputRate < 16000)
		{
			return;
		}
		if (sourceRate != inputRate)
		{
			sourceRate = inputRate;
			wireRate = ((inputRate >= 48000) ? 48000 : ((inputRate >= 32000) ? 32000 : 16000));
			resampler = new PhonePcmResampler(wireRate);
			frame.Clear();
		}
		resampler.Push(pcm, inputRate, delegate(short sample)
		{
			frame.Add(sample);
			if (frame.Count == wireRate / 100)
			{
				send(PhoneStreamPcm.Pack(frame.ToArray(), wireRate));
				frame.Clear();
			}
		});
	}
}
