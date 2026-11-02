using System;
using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class WindyLiveVoiceClient : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)2;

	protected override void OnHandleMessage(ReceivedMessage m)
	{
		if (NetworkInfo.IsHost || ((ReceivedMessage)(ref m)).IsServerHandled || ((ReceivedMessage)(ref m)).Sender != 0 || ((ReceivedMessage)(ref m)).Bytes.Length > 50000)
		{
			return;
		}
		try
		{
			PhoneVoicePacket phoneVoicePacket = ((ReceivedMessage)(ref m)).ReadData<PhoneVoicePacket>();
			if (phoneVoicePacket.Read(out var call, out var speaker, out var sequence, out var audio))
			{
				PhoneMod.Instance?.EnqueueLiveVoice(speaker, call, sequence, audio, host: false);
			}
		}
		catch (Exception)
		{
		}
	}
}
