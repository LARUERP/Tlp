using System.Linq;

namespace BonelabRP.Telephone;

public static class PhoneBankRules
{
	public static bool Amount(string text, out long value)
	{
		value = 0L;
		return text != null && text.Length > 0 && text.Length <= 12 && text.All((char c) => c >= '0' && c <= '9') && long.TryParse(text, out value) && value > 0 && value <= 999999999999L;
	}
}
