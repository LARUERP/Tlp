using System;
using System.Text.Json;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public sealed class WindyLiveMessage : ModuleMessageHandler
{
	public override ExpectedReceiverType ExpectedReceiver => (ExpectedReceiverType)2;

	protected override void OnHandleMessage(ReceivedMessage m)
	{
		if (NetworkInfo.IsHost || ((ReceivedMessage)(ref m)).IsServerHandled || ((ReceivedMessage)(ref m)).Sender != 0 || ((ReceivedMessage)(ref m)).Bytes.Length > 16000)
		{
			return;
		}
		try
		{
			WindyLiveView windyLiveView = JsonSerializer.Deserialize<WindyLiveView>(((ReceivedMessage)(ref m)).ReadData<PhonePacket>().Payload);
			if (windyLiveView != null && windyLiveView.Player == PlayerIDManager.LocalPlatformID && windyLiveView.Protocol == 16)
			{
				PhoneMod.Instance?.EnqueueLive(windyLiveView);
			}
		}
		catch (Exception)
		{
		}
	}
}
