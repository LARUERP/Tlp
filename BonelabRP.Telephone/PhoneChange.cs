namespace BonelabRP.Telephone;

public sealed class PhoneChange
{
	public ulong? Recipient { get; set; }

	public string Notice { get; set; } = "";

	public bool Duplicate { get; set; }
}
