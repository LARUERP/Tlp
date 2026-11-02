using System;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneVoiceToHost : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)1;

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		if (!NetworkInfo.IsHost || !((ReceivedMessage)(ref message)).IsServerHandled || !((ReceivedMessage)(ref message)).Sender.HasValue || ((ReceivedMessage)(ref message)).Bytes.Length > 50000)
		{
			return;
		}
		try
		{
			PlayerID playerID = PlayerIDManager.GetPlayerID(((ReceivedMessage)(ref message)).Sender.Value);
			if (playerID != null && playerID.IsValid)
			{
				PhoneVoicePacket phoneVoicePacket = ((ReceivedMessage)(ref message)).ReadData<PhoneVoicePacket>();
				if (phoneVoicePacket.Read(out var call, out var _, out var sequence, out var audio))
				{
					PhoneMod.Instance?.EnqueueVoice(playerID.PlatformID, call, sequence, audio, host: true);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
