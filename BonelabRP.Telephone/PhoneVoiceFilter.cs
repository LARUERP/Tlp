using System;

namespace BonelabRP.Telephone;

public sealed class PhoneVoiceFilter
{
	private sealed class Section
	{
		private readonly double b0;

		private readonly double b1;

		private readonly double b2;

		private readonly double a1;

		private readonly double a2;

		private double z1;

		private double z2;

		public Section(int rate, double cutoff, bool high)
		{
			double num = Math.PI * 2.0 * cutoff / (double)rate;
			double num2 = Math.Cos(num);
			double num3 = Math.Sin(num) / Math.Sqrt(2.0);
			double num4 = 1.0 + num3;
			double num5 = (high ? ((1.0 + num2) / 2.0) : ((1.0 - num2) / 2.0));
			b0 = num5 / num4;
			b1 = (double)(high ? (-2) : 2) * num5 / num4;
			b2 = num5 / num4;
			a1 = -2.0 * num2 / num4;
			a2 = (1.0 - num3) / num4;
		}

		public double Process(double x)
		{
			double num = b0 * x + z1;
			z1 = b1 * x - a1 * num + z2;
			z2 = b2 * x - a2 * num;
			return num;
		}
	}

	private readonly Section high;

	private readonly Section low1;

	private readonly Section low2;

	public PhoneVoiceFilter(int rate, double cutoff)
	{
		if (rate < 8000 || rate > 192000)
		{
			throw new ArgumentOutOfRangeException("rate");
		}
		cutoff = Math.Clamp(cutoff, 100.0, (double)rate * 0.45);
		high = new Section(rate, 75.0, high: true);
		low1 = new Section(rate, cutoff, high: false);
		low2 = new Section(rate, cutoff, high: false);
	}

	public float Process(float x)
	{
		if (!float.IsFinite(x))
		{
			x = 0f;
		}
		return (float)low2.Process(low1.Process(high.Process(Math.Clamp(x, -1f, 1f))));
	}
}
