namespace BonelabRP.Telephone;

public sealed class PhoneDeviceInfo
{
	public ulong Owner { get; set; }

	public ushort Device { get; set; }

	public bool PinSet { get; set; }

	public bool Parked { get; set; }

	public long Placement { get; set; }

	public float X { get; set; }

	public float Y { get; set; }

	public float Z { get; set; }

	public float Yaw { get; set; }
}
