using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public static class PhoneVoiceTransport
{
	public static void Register()
	{
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneVoiceToHost)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneVoiceToHost>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneVoiceToClient)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneVoiceToClient>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneCallStateMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneCallStateMessage>();
		}
	}

	public static void Send(PhoneVoicePacket packet)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneVoiceToHost, PhoneVoicePacket>(packet, new MessageRoute((RelayType)1, (NetworkChannel)1));
	}

	public static void Reply(PhoneVoicePacket packet, byte recipient)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneVoiceToClient, PhoneVoicePacket>(packet, new MessageRoute(recipient, (NetworkChannel)1));
	}

	public static void State(PhoneCallUpdate update, byte recipient)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneCallStateMessage, PhonePacket>(PhonePacket.From(update), new MessageRoute(recipient, (NetworkChannel)0));
	}
}
