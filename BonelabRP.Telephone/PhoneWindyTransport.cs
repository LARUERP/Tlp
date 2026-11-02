using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public static class PhoneWindyTransport
{
	public static void Register()
	{
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneWindyViewMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneWindyViewMessage>();
		}
	}

	public static void Reply(WindyView view, byte target)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneWindyViewMessage, PhonePacket>(PhonePacket.From(view), new MessageRoute(target, (NetworkChannel)0));
	}
}
