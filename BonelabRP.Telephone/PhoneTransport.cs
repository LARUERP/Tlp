using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public static class PhoneTransport
{
	public static void Register()
	{
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneCommandMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneCommandMessage>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneViewMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneViewMessage>();
		}
	}

	public static void Send(PhoneCommand command)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneCommandMessage, PhonePacket>(PhonePacket.From(command), CommonMessageRoutes.ReliableToServer);
	}

	public static void Reply(PhoneView view, byte id)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneViewMessage, PhonePacket>(PhonePacket.From(view), new MessageRoute(id, (NetworkChannel)0));
	}
}
