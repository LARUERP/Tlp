using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class WindyAccount
{
	public ulong Player { get; set; }

	public string Handle { get; set; } = "";

	public List<string> Nonces { get; set; } = new List<string>();
}
