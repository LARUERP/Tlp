using System;

namespace BonelabRP.Telephone;

public sealed class PhoneMediaChunk
{
	public int Protocol { get; set; } = 16;

	public string Session { get; set; } = "";

	public string Id { get; set; } = "";

	public string Call { get; set; } = "";

	public ulong Owner { get; set; }

	public ushort? Device { get; set; }

	public string Access { get; set; } = "";

	public string Kind { get; set; } = "";

	public uint Frame { get; set; }

	public int Index { get; set; }

	public int Count { get; set; }

	public int Total { get; set; }

	public byte[] Bytes { get; set; } = Array.Empty<byte>();
}
