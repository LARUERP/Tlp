using System;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class WindyLiveVoiceHost : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)1;

	protected override void OnHandleMessage(ReceivedMessage m)
	{
		if (!NetworkInfo.IsHost || !((ReceivedMessage)(ref m)).IsServerHandled || !((ReceivedMessage)(ref m)).Sender.HasValue || ((ReceivedMessage)(ref m)).Bytes.Length > 50000)
		{
			return;
		}
		try
		{
			PlayerID playerID = PlayerIDManager.GetPlayerID(((ReceivedMessage)(ref m)).Sender.Value);
			PhoneVoicePacket phoneVoicePacket = ((ReceivedMessage)(ref m)).ReadData<PhoneVoicePacket>();
			if (playerID != null && playerID.IsValid && phoneVoicePacket.Read(out var call, out var _, out var sequence, out var audio))
			{
				PhoneMod.Instance?.EnqueueLiveVoice(playerID.PlatformID, call, sequence, audio, host: true);
			}
		}
		catch (Exception)
		{
		}
	}
}
