using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneNoticeMessage : ModuleMessageHandler
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
			if (payload.Length <= 4000)
			{
				PhoneNotice phoneNotice = JsonSerializer.Deserialize<PhoneNotice>(payload);
				if (phoneNotice != null)
				{
					PhoneMod.Instance?.EnqueueNotice(phoneNotice);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
