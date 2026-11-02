using System;

namespace BonelabRP.Telephone;

public sealed class WindyPost
{
	public string Id { get; set; } = "";

	public ulong Author { get; set; }

	public string Text { get; set; } = "";

	public string Photo { get; set; } = "";

	public DateTime Utc { get; set; }
}
