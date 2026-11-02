using System;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LabFusion.Utilities;
using LabFusion.Voice;
using LabFusion.Voice.Unity;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneHighQualityMicrophone
{
	private static readonly FieldInfo clipField = typeof(UnityVoiceReceiver).GetField("_voiceClip", BindingFlags.Instance | BindingFlags.NonPublic);

	private AudioClip clip;

	private int cursor;

	private string session = "";

	private float last;

	private float warning;

	private PhoneStreamEncoder encoder = new PhoneStreamEncoder();

	public void Tick(string id, Action<byte[]> send, Action<string> warn, Func<short[], int, short[]> mix)
	{
		if (id != session)
		{
			session = id;
			clip = null;
			encoder = new PhoneStreamEncoder();
			last = Time.realtimeSinceStartup;
		}
		if (id == "")
		{
			return;
		}
		try
		{
			IVoiceManager voiceManager = VoiceInfo.VoiceManager;
			IVoiceReceiver val = ((voiceManager != null) ? voiceManager.GetReceiver() : null);
			AudioClip val2 = (AudioClip)((val == null) ? null : /*isinst with value type is only supported in some contexts*/);
			if ((Object)(object)val2 == (Object)null)
			{
				if (Time.realtimeSinceStartup - last > 2f)
				{
					throw new InvalidOperationException("Micro Fusion indisponible pour l’audio HD.");
				}
				return;
			}
			string validMicrophoneName = UnityVoiceReceiver.GetValidMicrophoneName();
			if (!Microphone.IsRecording(validMicrophoneName))
			{
				return;
			}
			int position = Microphone.GetPosition(validMicrophoneName);
			if (position < 0 || position >= val2.samples)
			{
				return;
			}
			if ((Object)(object)clip != (Object)(object)val2)
			{
				clip = val2;
				cursor = position;
				last = Time.realtimeSinceStartup;
				encoder = new PhoneStreamEncoder();
				return;
			}
			int num = PhoneMicrophoneRules.Available(cursor, position, val2.samples);
			if (num == 0)
			{
				return;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup - last > 0.2f)
			{
				cursor = position;
				last = realtimeSinceStartup;
				encoder = new PhoneStreamEncoder();
				return;
			}
			Il2CppStructArray<float> val3 = new Il2CppStructArray<float>((long)(num * val2.channels));
			if (!val2.GetData(val3, cursor))
			{
				throw new InvalidOperationException("Lecture micro HD impossible.");
			}
			float[] array = new float[((Il2CppArrayBase<float>)(object)val3).Length];
			InteropUtilities.Copy(((Il2CppObjectBase)val3).Pointer, IntPtr.Size, ((Il2CppArrayBase<float>)(object)val3).Length, array);
			cursor = position;
			last = realtimeSinceStartup;
			short[] arg = PhoneMicrophoneRules.Mono(array, val2.channels);
			encoder.Push(mix(arg, val2.frequency), val2.frequency, send);
		}
		catch (Exception ex)
		{
			if (Time.realtimeSinceStartup >= warning)
			{
				warning = Time.realtimeSinceStartup + 5f;
				warn(ex.Message);
			}
			clip = null;
			encoder = new PhoneStreamEncoder();
		}
	}
}
