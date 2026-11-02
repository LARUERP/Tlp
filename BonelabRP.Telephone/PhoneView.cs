using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class PhoneView
{
	public int Protocol { get; set; } = 16;

	public string Session { get; set; } = "";

	public ulong Player { get; set; }

	public List<PhoneDeviceInfo> Devices { get; set; } = new List<PhoneDeviceInfo>();

	public long Balance { get; set; }

	public List<PhoneCallEntry> Calls { get; set; } = new List<PhoneCallEntry>();

	public List<PhoneBankEntry> Transactions { get; set; } = new List<PhoneBankEntry>();

	public bool BankReady { get; set; }

	public PhoneCallView Call { get; set; } = new PhoneCallView();

	public long CallRevision { get; set; }

	public long Revision { get; set; }

	public string Number { get; set; } = "";

	public List<PhoneContact> Contacts { get; set; } = new List<PhoneContact>();

	public List<PhoneSms> Messages { get; set; } = new List<PhoneSms>();

	public ushort? Device { get; set; }

	public string Scene { get; set; } = "";

	public string DeviceStatus { get; set; } = "En attente du téléphone.";

	public PhoneConfiguration Configuration { get; set; } = new PhoneConfiguration();

	public string Notice { get; set; } = "";
}
