using System;
using System.Linq;
using System.Security.Cryptography;

namespace BonelabRP.Telephone;

public static class PhonePin
{
	public static bool Valid(string pin)
	{
		return pin != null && pin.Length == 4 && pin.All((char c) => c >= '0' && c <= '9');
	}

	public static string Salt()
	{
		return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
	}

	public static string Hash(string pin, string salt)
	{
		if (!Valid(pin))
		{
			throw new InvalidOperationException("Le code doit contenir quatre chiffres.");
		}
		using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(pin, Convert.FromBase64String(salt), 100000, HashAlgorithmName.SHA256);
		return Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(32));
	}

	public static bool Verify(string pin, string salt, string hash)
	{
		if (!Valid(pin) || salt == "" || hash == "")
		{
			return false;
		}
		try
		{
			return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(Hash(pin, salt)), Convert.FromBase64String(hash));
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
