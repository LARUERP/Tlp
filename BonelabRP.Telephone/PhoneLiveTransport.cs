using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace BonelabRP.Telephone;

public static class PhoneLiveTransport
{
	public static void Register()
	{
		if (ModuleMessageManager.GetHandlerByType(typeof(PhoneAccessMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<PhoneAccessMessage>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(WindyLiveMessage)) == null)
		{
			ModuleMessageManager.RegisterHandler<WindyLiveMessage>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(WindyLiveVoiceHost)) == null)
		{
			ModuleMessageManager.RegisterHandler<WindyLiveVoiceHost>();
		}
		if (ModuleMessageManager.GetHandlerByType(typeof(WindyLiveVoiceClient)) == null)
		{
			ModuleMessageManager.RegisterHandler<WindyLiveVoiceClient>();
		}
	}

	public static void Access(PhoneAccessView v, byte target)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<PhoneAccessMessage, PhonePacket>(PhonePacket.From(v), new MessageRoute(target, (NetworkChannel)0));
	}

	public static void State(WindyLiveView v, byte target)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<WindyLiveMessage, PhonePacket>(PhonePacket.From(v), new MessageRoute(target, (NetworkChannel)0));
	}

	public static void Send(PhoneVoicePacket p)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<WindyLiveVoiceHost, PhoneVoicePacket>(p, new MessageRoute((RelayType)1, (NetworkChannel)1));
	}

	public static void Voice(PhoneVoicePacket p, byte target)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		MessageRelay.RelayModule<WindyLiveVoiceClient, PhoneVoicePacket>(p, new MessageRoute(target, (NetworkChannel)1));
	}
}
