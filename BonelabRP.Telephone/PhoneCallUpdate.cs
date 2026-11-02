namespace BonelabRP.Telephone;

public sealed class PhoneCallUpdate
{
	public int Protocol { get; set; } = 16;

	public string Session { get; set; } = "";

	public ulong Player { get; set; }

	public long Revision { get; set; }

	public PhoneCallView Call { get; set; } = new PhoneCallView();

	public string Notice { get; set; } = "";
}
