namespace BonelabRP.Telephone;

public sealed class PhoneCallView
{
	public string Id { get; set; } = "";

	public string State { get; set; } = "Idle";

	public ulong Peer { get; set; }

	public string Number { get; set; } = "";

	public bool VideoRequested { get; set; }

	public bool Video { get; set; }

	public bool PeerVideo { get; set; }

	public bool Speaker { get; set; }

	public bool PeerSpeaker { get; set; }

	public bool Masked { get; set; }

	public bool Incoming { get; set; }
}
