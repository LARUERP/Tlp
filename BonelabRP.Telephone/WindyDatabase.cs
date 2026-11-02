using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class WindyDatabase
{
	public int Version { get; set; } = 1;

	public List<WindyAccount> Accounts { get; set; } = new List<WindyAccount>();

	public List<WindyPost> Posts { get; set; } = new List<WindyPost>();
}
