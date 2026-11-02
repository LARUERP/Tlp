using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneCallStateMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)2;

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		if (NetworkInfo.IsHost || ((ReceivedMessage)(ref message)).IsServerHandled || ((ReceivedMessage)(ref message)).Sender != 0 || ((ReceivedMessage)(ref message)).Bytes.Length > 4096)
		{
			return;
		}
		try
		{
			byte[] payload = ((ReceivedMessage)(ref message)).ReadData<PhonePacket>().Payload;
			if (payload != null && payload.Length <= 4000)
			{
				PhoneCallUpdate phoneCallUpdate = JsonSerializer.Deserialize<PhoneCallUpdate>(payload);
				if (phoneCallUpdate != null && phoneCallUpdate.Protocol == 16 && phoneCallUpdate.Player == PlayerIDManager.LocalPlatformID)
				{
					PhoneMod.Instance?.EnqueueCall(phoneCallUpdate);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
