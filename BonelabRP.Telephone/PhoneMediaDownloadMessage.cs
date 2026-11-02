using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneMediaDownloadMessage : ModuleMessageHandler
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
			if (payload != null && payload.Length <= 3800)
			{
				PhoneMediaChunk c = JsonSerializer.Deserialize<PhoneMediaChunk>(payload);
				if (PhoneMediaRules.ValidChunk(c))
				{
					PhoneMod.Instance?.Media?.Enqueue(0uL, c, host: false);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
