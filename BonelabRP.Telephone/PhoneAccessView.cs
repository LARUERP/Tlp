using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class PhoneAccessView
{
	public int Protocol { get; set; } = 16;

	public string Session { get; set; } = "";

	public ulong Player { get; set; }

	public ulong Owner { get; set; }

	public ushort Device { get; set; }

	public bool BankPinSet { get; set; }

	public bool BankUnlocked { get; set; }

	public long Revision { get; set; }

	public int Epoch { get; set; }

	public bool PinSet { get; set; }

	public string Wallpaper { get; set; } = "";

	public string Token { get; set; } = "";

	public string Notice { get; set; } = "";

	public PhoneView Profile { get; set; }

	public WindyView Windy { get; set; }

	public List<string> Deleted { get; set; } = new List<string>();

	public List<string> Gallery { get; set; } = new List<string>();
}
