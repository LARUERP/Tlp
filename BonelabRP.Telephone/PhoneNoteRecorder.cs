using System;
using System.Collections.Generic;
using System.Linq;

namespace BonelabRP.Telephone;

public sealed class PhoneNoteRecorder
{
	private readonly List<short> samples = new List<short>();

	private readonly int outputRate;

	private readonly PhonePcmResampler resampler;

	public PhoneNoteRecorder(int rate = 8000)
	{
		if (rate != 8000 && rate != 16000 && rate != 32000 && rate != 48000)
		{
			throw new ArgumentOutOfRangeException("rate");
		}
		outputRate = rate;
		resampler = new PhonePcmResampler(rate);
	}

	public void Capture(short[] frame, int rate, double elapsed)
	{
		if (!double.IsFinite(elapsed) || elapsed < 0.0)
		{
			return;
		}
		resampler.Push(frame, rate, delegate(short sample)
		{
			if (samples.Count < outputRate * 15)
			{
				samples.Add(sample);
			}
		});
	}

	public byte[] Finish(double elapsed)
	{
		if (!double.IsFinite(elapsed) || elapsed < 0.5)
		{
			throw new InvalidOperationException("Vocal : 0,5 à 15 secondes.");
		}
		int num = (int)Math.Min(outputRate * 15, elapsed * (double)outputRate);
		while (samples.Count < num)
		{
			samples.Add(0);
		}
		return PhoneVoiceNoteRules.Pack(samples.Take(num).ToArray(), outputRate);
	}
}
