using System;
using System.Buffers.Binary;
using LabFusion.Network.Serialization;

namespace BonelabRP.Telephone;

public sealed class PhoneSpeakerPacket : INetSerializable
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

	public static PhoneSpeakerPacket Create(string token, ushort device, PhoneVoicePacket frame)
	{
		byte[] array = new byte[18 + frame.Payload.Length];
		new Guid(token).ToByteArray().CopyTo(array, 0);
		BinaryPrimitives.WriteUInt16LittleEndian(array.AsSpan(16, 2), device);
		frame.Payload.CopyTo(array, 18);
		return new PhoneSpeakerPacket
		{
			Payload = array
		};
	}

	public bool Read(out string token, out ushort device, out string call, out ulong actor, out uint seq, out byte[] audio)
	{
		token = (call = "");
		device = 0;
		actor = 0uL;
		seq = 0u;
		audio = null;
		if (Payload == null || Payload.Length < 47 || Payload.Length > 49200)
		{
			return false;
		}
		token = new Guid(Payload.AsSpan(0, 16)).ToString("N");
		device = BinaryPrimitives.ReadUInt16LittleEndian(Payload.AsSpan(16, 2));
		return new PhoneVoicePacket
		{
			Payload = Payload.AsSpan(18).ToArray()
		}.Read(out call, out actor, out seq, out audio);
	}
}
