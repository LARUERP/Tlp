using HarmonyLib;
using LabFusion.Senders;

namespace BonelabRP.Telephone;

[HarmonyPatch(typeof(PlayerSender), "SendPlayerVoiceChat")]
public static class PhoneOutgoingVoicePatch
{
	[HarmonyPrefix]
	public static bool Prefix(byte[] __0)
	{
		return PhoneMod.Instance?.InterceptVoice(__0) ?? true;
	}
}
