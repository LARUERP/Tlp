using System;
using System.Text.Json;
using LabFusion.Network.Serialization;

namespace BonelabRP.Telephone;

public sealed class PhonePacket : INetSerializable
{
	public byte[] Payload = Array.Empty<byte>();

	public int? GetSize()
	{
		return 4 + Payload.Length;
	}

	public void Serialize(INetSerializer serializer)
	{
		serializer.SerializeValue(ref Payload);
	}

	public static PhonePacket From<T>(T value)
	{
		return new PhonePacket
		{
			Payload = JsonSerializer.SerializeToUtf8Bytes(value)
		};
	}
}
