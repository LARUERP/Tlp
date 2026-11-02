namespace BonelabRP.Telephone;

public sealed class PhoneCommand
{
	public int Protocol { get; set; } = 16;

	public string Session { get; set; } = "";

	public ulong Owner { get; set; }

	public ushort? Device { get; set; }

	public string Access { get; set; } = "";

	public int Epoch { get; set; }

	public string Operation { get; set; } = "sync";

	public string Nonce { get; set; } = "";

	public string Call { get; set; } = "";

	public bool VoiceReady { get; set; }

	public bool ListenReady { get; set; }

	public bool Enabled { get; set; }

	public string Voice { get; set; } = "";

	public int VoiceSeconds { get; set; }

	public int Offset { get; set; }

	public long Amount { get; set; }

	public string Number { get; set; } = "";

	public string Name { get; set; } = "";

	public string Text { get; set; } = "";

	public string Photo { get; set; } = "";
}
