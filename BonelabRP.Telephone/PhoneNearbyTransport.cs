using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public static class PhoneNearbyTransport
{
	public static void Register()
	{
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneNoticeMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneNoticeMessage>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneSpeakerMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneSpeakerMessage>();
		}
	}

	public static void Notice(PhoneNotice p, byte target)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneNoticeMessage, PhonePacket>(PhonePacket.From(p), new MessageRoute(target, (NetworkChannel)0));
	}

	public static void Speaker(PhoneSpeakerPacket p, byte target)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneSpeakerMessage, PhoneSpeakerPacket>(p, new MessageRoute(target, (NetworkChannel)1));
	}
}
