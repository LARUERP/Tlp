using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class PhoneAccount
{
	public ulong Player { get; set; }

	public string Number { get; set; } = "";

	public List<PhoneContact> Contacts { get; set; } = new List<PhoneContact>();

	public List<PhoneSms> Messages { get; set; } = new List<PhoneSms>();

	public List<PhoneCallEntry> Calls { get; set; } = new List<PhoneCallEntry>();

	public List<PhoneBankEntry> Transactions { get; set; } = new List<PhoneBankEntry>();

	public string Wallpaper { get; set; } = "";

	public string BankPinSalt { get; set; } = "";

	public string BankPinHash { get; set; } = "";

	public string PinSalt { get; set; } = "";

	public string PinHash { get; set; } = "";

	public int PinDigits { get; set; } = 6;

	public List<string> GalleryDeleted { get; set; } = new List<string>();

	public List<string> Gallery { get; set; } = new List<string>();

	public List<string> RecentCommands { get; set; } = new List<string>();
}
