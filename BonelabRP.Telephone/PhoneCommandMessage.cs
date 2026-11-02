using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneCommandMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)1;

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		if (!NetworkInfo.IsHost || !((ReceivedMessage)(ref message)).IsServerHandled || !((ReceivedMessage)(ref message)).Sender.HasValue || ((ReceivedMessage)(ref message)).Bytes.Length > 8192)
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
			if (payload != null && payload.Length <= 8000)
			{
				PhoneCommand phoneCommand = JsonSerializer.Deserialize<PhoneCommand>(payload);
				if (phoneCommand != null)
				{
					PhoneMod.Instance?.Enqueue(playerID.PlatformID, phoneCommand);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
