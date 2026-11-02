using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneViewMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)2;

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		if (NetworkInfo.IsHost || ((ReceivedMessage)(ref message)).IsServerHandled || ((ReceivedMessage)(ref message)).Sender != 0 || ((ReceivedMessage)(ref message)).Bytes.Length > 262144)
		{
			return;
		}
		try
		{
			byte[] payload = ((ReceivedMessage)(ref message)).ReadData<PhonePacket>().Payload;
			if (payload != null && payload.Length <= 260000)
			{
				PhoneView phoneView = JsonSerializer.Deserialize<PhoneView>(payload);
				if (phoneView != null && phoneView.Player == PlayerIDManager.LocalPlatformID && phoneView.Protocol == 16)
				{
					PhoneMod.Instance?.Enqueue(phoneView);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
