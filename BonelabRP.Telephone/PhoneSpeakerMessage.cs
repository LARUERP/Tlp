using System;
using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneSpeakerMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)2;

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		if (NetworkInfo.IsHost || ((ReceivedMessage)(ref message)).IsServerHandled || ((ReceivedMessage)(ref message)).Sender != 0 || ((ReceivedMessage)(ref message)).Bytes.Length > 50000)
		{
			return;
		}
		try
		{
			PhoneSpeakerPacket phoneSpeakerPacket = ((ReceivedMessage)(ref message)).ReadData<PhoneSpeakerPacket>();
			if (phoneSpeakerPacket.Read(out var _, out var _, out var _, out var _, out var _, out var _))
			{
				PhoneMod.Instance?.EnqueueSpeaker(phoneSpeakerPacket);
			}
		}
		catch (Exception)
		{
		}
	}
}
