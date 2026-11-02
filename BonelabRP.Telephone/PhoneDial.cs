using System;

namespace BonelabRP.Telephone;

public static class PhoneDial
{
	public static string Parse(string input, out bool masked)
	{
		if (input == null || input.Length > 41)
		{
			throw new InvalidOperationException("Numéro invalide.");
		}
		input = input.Trim();
		masked = input.StartsWith("#", StringComparison.Ordinal);
		return PhoneService.Normalize(masked ? input.Substring(1) : input);
	}

	public static string Display(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}
		bool flag = input.StartsWith("#");
		return (flag ? "# " : "") + PhoneService.Display(flag ? input.Substring(1) : input);
	}
}
