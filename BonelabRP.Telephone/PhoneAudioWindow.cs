namespace BonelabRP.Telephone;

public sealed class PhoneAudioWindow
{
	private uint highest;

	private ulong seen;

	public bool Accept(uint seq)
	{
		if (seq == 0)
		{
			return false;
		}
		if (seq > highest)
		{
			uint num = seq - highest;
			seen = ((num >= 64) ? 1 : ((seen << (int)num) | 1));
			highest = seq;
			return true;
		}
		uint num2 = highest - seq;
		if (num2 >= 64)
		{
			return false;
		}
		ulong num3 = (ulong)(1L << (int)num2);
		if ((seen & num3) != 0)
		{
			return false;
		}
		seen |= num3;
		return true;
	}
}
