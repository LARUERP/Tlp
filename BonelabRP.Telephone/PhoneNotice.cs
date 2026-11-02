using System;

namespace BonelabRP.Telephone;

public sealed class PhoneNotice
{
	public int Protocol { get; set; } = 16;

	public string Id { get; set; } = Guid.NewGuid().ToString("N");

	public string Session { get; set; } = "";

	public string Kind { get; set; } = "";

	public string Name { get; set; } = "";

	public string Number { get; set; } = "";

	public string Text { get; set; } = "";

	public ulong Player { get; set; }

	public ulong Owner { get; set; }
}
