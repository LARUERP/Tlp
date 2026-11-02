using System;

namespace BonelabRP.Telephone;

public sealed class PhoneSms
{
	public string Id { get; set; } = "";

	public string From { get; set; } = "";

	public string To { get; set; } = "";

	public string Text { get; set; } = "";

	public string Photo { get; set; } = "";

	public string Voice { get; set; } = "";

	public int VoiceSeconds { get; set; }

	public DateTime Utc { get; set; }

	public bool Read { get; set; }
}
