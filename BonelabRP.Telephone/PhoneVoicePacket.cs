using System;
using System.Buffers.Binary;
using LabFusion.Network.Serialization;

namespace BonelabRP.Telephone;

public sealed class PhoneVoicePacket : INetSerializable
{
	public const int Header = 28;

	public const int MaxAudio = 49152;

	public byte[] Payload = Array.Empty<byte>();

	public int? GetSize()
	{
		return 4 + Payload.Length;
	}

	public void Serialize(INetSerializer serializer)
	{
		serializer.SerializeValue(ref Payload);
	}

	public static PhoneVoicePacket Create(string call, ulong speaker, uint sequence, byte[] audio)
	{
		if (audio == null || audio.Length == 0 || audio.Length > 49152)
		{
			throw new InvalidOperationException("Trame vocale invalide.");
		}
		byte[] array = new byte[28 + audio.Length];
		new Guid(call).ToByteArray().CopyTo(array, 0);
		BinaryPrimitives.WriteUInt64LittleEndian(array.AsSpan(16, 8), speaker);
		BinaryPrimitives.WriteUInt32LittleEndian(array.AsSpan(24, 4), sequence);
		audio.CopyTo(array, 28);
		return new PhoneVoicePacket
		{
			Payload = array
		};
	}

	public bool Read(out string call, out ulong speaker, out uint sequence, out byte[] audio)
	{
		call = "";
		speaker = 0uL;
		sequence = 0u;
		audio = null;
		if (Payload == null || Payload.Length <= 28 || Payload.Length > 49180)
		{
			return false;
		}
		call = new Guid(Payload.AsSpan(0, 16)).ToString("N");
		speaker = BinaryPrimitives.ReadUInt64LittleEndian(Payload.AsSpan(16, 8));
		sequence = BinaryPrimitives.ReadUInt32LittleEndian(Payload.AsSpan(24, 4));
		audio = Payload.AsSpan(28).ToArray();
		return true;
	}
}
