namespace BonelabRP.Telephone;

public sealed class WindyLiveSummary
{
	public string Id { get; set; } = "";

	public ulong Host { get; set; }

	public string Handle { get; set; } = "";

	public ulong Owner { get; set; }

	public ushort Device { get; set; }

	public bool Front { get; set; }

	public int Viewers { get; set; }

	public int Likes { get; set; }
}
