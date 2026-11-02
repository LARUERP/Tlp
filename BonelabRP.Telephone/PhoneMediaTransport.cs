using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public static class PhoneMediaTransport
{
	public static void Register()
	{
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneMediaUploadMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneMediaUploadMessage>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneMediaDownloadMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneMediaDownloadMessage>();
		}
	}

	public static void Send(PhoneMediaChunk c)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneMediaUploadMessage, PhonePacket>(PhonePacket.From(c), (MessageRoute)(PhoneMediaRules.Stream(c.Kind) ? new MessageRoute((RelayType)1, (NetworkChannel)1) : CommonMessageRoutes.ReliableToServer));
	}

	public static void Reply(PhoneMediaChunk c, byte id)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneMediaDownloadMessage, PhonePacket>(PhonePacket.From(c), new MessageRoute(id, (NetworkChannel)(PhoneMediaRules.Stream(c.Kind) ? 1 : 0)));
	}
}
