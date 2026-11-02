using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneWindyViewMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)2;

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		if (NetworkInfo.IsHost || ((ReceivedMessage)(ref message)).IsServerHandled || ((ReceivedMessage)(ref message)).Sender != 0 || ((ReceivedMessage)(ref message)).Bytes.Length > 16000)
		{
			return;
		}
		try
		{
			byte[] payload = ((ReceivedMessage)(ref message)).ReadData<PhonePacket>().Payload;
			if (payload != null && payload.Length <= 15000)
			{
				WindyView windyView = JsonSerializer.Deserialize<WindyView>(payload);
				if (windyView != null && windyView.Protocol == 16 && windyView.Player == PlayerIDManager.LocalPlatformID)
				{
					PhoneMod.Instance?.EnqueueWindy(windyView);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
