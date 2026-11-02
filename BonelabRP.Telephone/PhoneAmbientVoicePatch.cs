using System;
using HarmonyLib;
using LabFusion.Voice.Unity;

namespace BonelabRP.Telephone;

[HarmonyPatch(typeof(UnityVoiceSpeaker), "OnVoiceDataReceived")]
public static class PhoneAmbientVoicePatch
{
	[HarmonyPrefix]
	public static void Prefix(UnityVoiceSpeaker __instance, byte[] __0)
	{
		try
		{
			PhoneMod instance = PhoneMod.Instance;
			instance?.Ambient.Receive(instance, __instance, __0);
		}
		catch (Exception)
		{
		}
	}
}
