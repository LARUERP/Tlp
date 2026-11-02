using System;
using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneVoiceToClient : ModuleMessageHandler
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
			PhoneVoicePacket phoneVoicePacket = ((ReceivedMessage)(ref message)).ReadData<PhoneVoicePacket>();
			if (phoneVoicePacket.Read(out var call, out var speaker, out var sequence, out var audio))
			{
				PhoneMod.Instance?.EnqueueVoice(speaker, call, sequence, audio, host: false);
			}
		}
		catch (Exception)
		{
		}
	}
}
