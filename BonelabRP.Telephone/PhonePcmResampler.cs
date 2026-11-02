using System;

namespace BonelabRP.Telephone;

public sealed class PhonePcmResampler
{
	private readonly int outputRate;

	private PhoneVoiceFilter filter;

	private int inputRate;

	private long inputIndex;

	private long outputIndex;

	private double previous;

	public PhonePcmResampler(int rate)
	{
		outputRate = rate;
	}

	public void Push(short[] pcm, int rate, Action<short> emit)
	{
		if (pcm == null || rate < outputRate || rate > 192000)
		{
			return;
		}
		if (inputRate != rate)
		{
			inputRate = rate;
			filter = new PhoneVoiceFilter(rate, (double)outputRate * 0.4);
			inputIndex = (outputIndex = 0L);
			previous = 0.0;
		}
		foreach (short num in pcm)
		{
			double num2 = filter.Process((float)num / 32768f);
			for (double num3 = (double)outputIndex * (double)inputRate / (double)outputRate; num3 <= (double)inputIndex; num3 = (double)outputIndex * (double)inputRate / (double)outputRate)
			{
				double num4 = ((inputIndex == 0L) ? 1.0 : (num3 - (double)(inputIndex - 1)));
				double num5 = previous + (num2 - previous) * num4;
				emit((short)Math.Clamp(num5 * 32767.0, -32767.0, 32767.0));
				outputIndex++;
			}
			previous = num2;
			inputIndex++;
		}
	}
}
