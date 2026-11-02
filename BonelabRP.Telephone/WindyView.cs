using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class WindyView
{
	public int Protocol { get; set; } = 16;

	public string Session { get; set; } = "";

	public ulong Player { get; set; }

	public ulong Owner { get; set; }

	public long Revision { get; set; }

	public string Handle { get; set; } = "";

	public string Notice { get; set; } = "";

	public int Offset { get; set; }

	public int Total { get; set; }

	public List<WindyPostView> Posts { get; set; } = new List<WindyPostView>();
}
