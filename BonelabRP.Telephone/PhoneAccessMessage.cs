using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class PhoneAccessMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)2;

	protected override void OnHandleMessage(ReceivedMessage m)
	{
		if (NetworkInfo.IsHost || ((ReceivedMessage)(ref m)).IsServerHandled || ((ReceivedMessage)(ref m)).Sender != 0 || ((ReceivedMessage)(ref m)).Bytes.Length > 262144)
		{
			return;
		}
		try
		{
			PhoneAccessView phoneAccessView = JsonSerializer.Deserialize<PhoneAccessView>(((ReceivedMessage)(ref m)).ReadData<PhonePacket>().Payload);
			if (phoneAccessView != null && phoneAccessView.Player == PlayerIDManager.LocalPlatformID && phoneAccessView.Protocol == 16)
			{
				PhoneMod.Instance?.EnqueueAccess(phoneAccessView);
			}
		}
		catch (Exception)
		{
		}
	}
}
