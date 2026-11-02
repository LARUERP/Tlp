using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneMediaUploadMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)1;

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		if (!NetworkInfo.IsHost || !((ReceivedMessage)(ref message)).IsServerHandled || !((ReceivedMessage)(ref message)).Sender.HasValue || ((ReceivedMessage)(ref message)).Bytes.Length > 4096)
		{
			return;
		}
		try
		{
			PlayerID playerID = PlayerIDManager.GetPlayerID(((ReceivedMessage)(ref message)).Sender.Value);
			if (playerID == null || !playerID.IsValid)
			{
				return;
			}
			byte[] payload = ((ReceivedMessage)(ref message)).ReadData<PhonePacket>().Payload;
			if (payload != null && payload.Length <= 3800)
			{
				PhoneMediaChunk c = JsonSerializer.Deserialize<PhoneMediaChunk>(payload);
				if (PhoneMediaRules.ValidChunk(c))
				{
					PhoneMod.Instance?.Media?.Enqueue(playerID.PlatformID, c, host: true);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
