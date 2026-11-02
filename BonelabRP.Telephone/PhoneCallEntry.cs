using System;

namespace BonelabRP.Telephone;

public sealed class PhoneCallEntry
{
	public string Id { get; set; } = "";

	public string Number { get; set; } = "";

	public string Outcome { get; set; } = "";

	public bool Incoming { get; set; }

	public bool Masked { get; set; }

	public int Seconds { get; set; }

	public DateTime Utc { get; set; }
}
