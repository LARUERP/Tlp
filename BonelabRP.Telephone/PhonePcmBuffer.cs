using System;

namespace BonelabRP.Telephone;

public sealed class PhonePcmBuffer
{
	private readonly object gate = new object();

	private readonly float[] samples;

	private readonly int warmup;

	private readonly int fade;

	private readonly int maximum;

	private int read;

	private int count;

	private bool started;

	private bool disposed;

	private float previous;

	private float gain;

	public int Count
	{
		get
		{
			lock (gate)
			{
				return count;
			}
		}
	}

	public PhonePcmBuffer(int rate)
	{
		if (rate < 8000 || rate > 192000)
		{
			throw new ArgumentOutOfRangeException("rate");
		}
		samples = new float[rate];
		warmup = rate * 120 / 1000;
		fade = Math.Max(16, rate / 200);
		maximum = rate / 2;
	}

	public void Push(short[] pcm, float volume)
	{
		if (pcm == null || pcm.Length == 0)
		{
			return;
		}
		volume = ((float.IsNaN(volume) || float.IsInfinity(volume)) ? 0f : Math.Max(0f, Math.Min(1f, volume)));
		lock (gate)
		{
			if (!disposed)
			{
				int num = Math.Min(pcm.Length, maximum);
				if (count + num > maximum)
				{
					int num2 = count + num - maximum;
					read = (read + num2) % samples.Length;
					count -= num2;
					started = false;
					gain = 0f;
				}
				for (int i = pcm.Length - num; i < pcm.Length; i++)
				{
					samples[(read + count) % samples.Length] = PhoneAudioBuffer.Limit((float)pcm[i] / 32768f * volume);
					count++;
				}
			}
		}
	}

	public void Read(float[] output)
	{
		lock (gate)
		{
			for (int i = 0; i < output.Length; i++)
			{
				if (!started && count >= warmup)
				{
					started = true;
					gain = 0f;
				}
				if (disposed || !started || count == 0)
				{
					previous *= 0.96f;
					if (Math.Abs(previous) < 1E-05f)
					{
						previous = 0f;
					}
					output[i] = previous;
					if (started && count == 0)
					{
						started = false;
						gain = 0f;
					}
				}
				else
				{
					float num = samples[read];
					read = (read + 1) % samples.Length;
					count--;
					gain = Math.Min(1f, gain + 1f / (float)fade);
					previous = num * gain;
					output[i] = previous;
				}
			}
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			disposed = true;
			count = 0;
			started = false;
			previous = (gain = 0f);
		}
	}
}
