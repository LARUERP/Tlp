using System.Collections.Generic;

namespace BonelabRP.Telephone;

public sealed class WindyLiveView
{
	public int Protocol { get; set; } = 16;

	public ulong Player { get; set; }

	public string Session { get; set; } = "";

	public long Revision { get; set; }

	public WindyLiveSummary Active { get; set; }

	public bool Casting { get; set; }

	public bool Liked { get; set; }

	public List<WindyLiveSummary> Available { get; set; } = new List<WindyLiveSummary>();

	public List<WindyLiveComment> Comments { get; set; } = new List<WindyLiveComment>();
}
