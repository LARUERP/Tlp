using System;
using System.Collections.Generic;
using System.Linq;
using BoneLib;
using LabFusion.Audio;
using LabFusion.Player;
using LabFusion.Voice;
using LabFusion.Voice.Unity;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneVoice : IDisposable
{
	private sealed class Broadcast
	{
		public PhoneAudioPlayer Source;

		public ushort Device;

		public float Last;

		public uint Sequence;

		public ulong Actor;

		public PhoneAudioBuffer Buffer = new PhoneAudioBuffer();
	}

	private readonly PhoneHighQualityMicrophone highQuality = new PhoneHighQualityMicrophone();

	private PhoneAudioPlayer earpiece;

	private PhoneAudioPlayer liveSource;

	private readonly PhoneAudioBuffer privateBuffer = new PhoneAudioBuffer();

	private readonly PhoneAudioBuffer liveBuffer = new PhoneAudioBuffer();

	private ulong privateActor;

	private ulong liveActor;

	private string liveSend = "";

	private string liveReceive = "";

	private uint liveSent;

	private float liveLast;

	private GameObject ringObject;

	private AudioSource ringtone;

	private string token = "";

	private string ringToken = "";

	private uint sequence;

	private uint received;

	private readonly Action<string> warn;

	private readonly Dictionary<string, Broadcast> broadcasts = new Dictionary<string, Broadcast>();

	public static bool CanListen => VoiceInfo.VoiceManager is UnityVoiceManager && VoiceInfo.CanHear && !VoiceInfo.IsDeafened;

	public static bool Ready => VoiceInfo.VoiceManager is UnityVoiceManager && VoiceInfo.CanTalk && VoiceInfo.CanHear && !VoiceInfo.IsDeafened;

	public PhoneVoice(Action<string> warn)
	{
		this.warn = warn;
	}

	public bool Intercept(byte[] audio, PhoneMod mod)
	{
		if (mod.Notes != null && mod.Notes.Capture(audio))
		{
			return false;
		}
		if (mod.View.Call.State == "Active")
		{
			return false;
		}
		return true;
	}

	public void Receive(PhoneMod mod, ulong speaker, string call, uint seq, byte[] audio)
	{
		if (!mod.CallActive || mod.View.Call.Id != call || mod.View.Call.Peer != speaker || VoiceInfo.IsDeafened)
		{
			return;
		}
		if (token != call)
		{
			StopEarpiece();
			token = call;
			sequence = (received = 0u);
		}
		if (!ValidCompressedFrame(audio))
		{
			return;
		}
		PlayerID playerID = PlayerIDManager.GetPlayerID(speaker);
		if (playerID != null && playerID.IsValid && CanListen)
		{
			received = seq;
			if (earpiece == null)
			{
				earpiece = new PhoneAudioPlayer();
				((Object)earpiece.gameObject).name = "RP_PrivatePhoneEarpiece";
				earpiece.AudioSource.spatialBlend = 0f;
				earpiece.AudioSource.dopplerLevel = 0f;
				earpiece.AudioSource.volume = 0.9f;
			}
			privateActor = speaker;
			privateBuffer.Push(seq, audio, Time.realtimeSinceStartup);
		}
	}

	private static void Play(PhoneAudioPlayer source, ulong actor, byte[] bytes)
	{
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (source != null && playerID != null && playerID.IsValid && CanListen)
		{
			short[] pcm = (PhoneStreamPcm.Valid(bytes) ? PhoneStreamPcm.Decode(bytes, AudioInfo.OutputSampleRate) : VoiceConverter.Decode(bytes));
			float volume = Math.Max(0f, Math.Min(1f, VoiceVolume.GetVolumeMultiplier() * VoiceInfo.VoiceManager.GetSpeaker(playerID).Volume));
			source.Push(pcm, volume);
		}
	}

	public static bool ValidCompressedFrame(byte[] bytes)
	{
		return PhoneStreamPcm.Valid(bytes) || PhoneVoiceGuard.Valid(bytes);
	}

	public void ReceiveLive(PhoneMod mod, ulong actor, string id, uint seq, byte[] bytes)
	{
		if (!mod.IsWatching || mod.Live.Active.Id != id || mod.Live.Active.Host != actor || !CanListen || !ValidCompressedFrame(bytes))
		{
			return;
		}
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID != null && playerID.IsValid)
		{
			if (liveReceive != id)
			{
				StopLiveSource();
				liveReceive = id;
			}
			liveLast = Time.realtimeSinceStartup;
			if (liveSource == null)
			{
				liveSource = new PhoneAudioPlayer();
				((Object)liveSource.gameObject).name = "RP_WindyLiveAudio";
				liveSource.AudioSource.spatialBlend = 0f;
				liveSource.AudioSource.dopplerLevel = 0f;
				liveSource.AudioSource.volume = 0.9f;
			}
			liveActor = actor;
			liveBuffer.Push(seq, bytes, Time.realtimeSinceStartup);
		}
	}

	private void StopLiveSource()
	{
		liveBuffer.Clear();
		if (liveSource != null)
		{
			liveSource.Dispose();
			liveSource = null;
		}
	}

	public void Tick(PhoneMod mod)
	{
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		string capture = (mod.CallActive ? mod.View.Call.Id : (mod.IsCasting ? mod.Live.Active.Id : ""));
		bool flag = capture != "" && Ready && !VoiceInfo.IsMuted && (!mod.CallActive || !mod.CallMicMuted);
		highQuality.Tick(flag ? capture : "", delegate(byte[] bytes)
		{
			if (mod.CallActive)
			{
				if (token != capture)
				{
					token = capture;
					sequence = 0u;
				}
				mod.SendVoice(capture, ++sequence, bytes);
			}
			else if (mod.IsCasting)
			{
				if (liveSend != capture)
				{
					liveSend = capture;
					liveSent = 0u;
				}
				mod.SendLiveVoice(capture, ++liveSent, bytes);
			}
		}, warn, (short[] pcm, int rate) => mod.Ambient.Mix(mod, pcm, rate));
		if (!mod.IsWatching || mod.Live.Active?.Id != liveReceive || Time.realtimeSinceStartup - liveLast > 3f)
		{
			StopLiveSource();
		}
		if (!mod.CallActive && token != "")
		{
			StopEarpiece();
			token = "";
			sequence = (received = 0u);
		}
		if (earpiece != null)
		{
			earpiece.AudioSource.volume = (CanListen ? 0.9f : 0f);
		}
		if (liveSource != null)
		{
			liveSource.AudioSource.volume = (CanListen ? 0.9f : 0f);
		}
		if (earpiece != null && (Object)(object)Player.Head != (Object)null)
		{
			earpiece.transform.position = Player.Head.position;
		}
		privateBuffer.Drain(Time.realtimeSinceStartup, delegate(byte[] bytes)
		{
			Play(earpiece, privateActor, bytes);
		}, delegate(int gap)
		{
			earpiece?.Conceal(gap);
		});
		liveBuffer.Drain(Time.realtimeSinceStartup, delegate(byte[] bytes)
		{
			Play(liveSource, liveActor, bytes);
		}, delegate(int gap)
		{
			liveSource?.Conceal(gap);
		});
		PhoneCallView call = mod.View.Call;
		TickSpeakers();
		if ((Object)(object)ringtone != (Object)null)
		{
			ringtone.volume = mod.Sounds.Preferences.RingVolume * (call.Incoming ? 1f : 0.3f);
		}
		if (!(call.State == "Ringing"))
		{
			if ((Object)(object)ringtone != (Object)null && ringtone.isPlaying)
			{
				ringtone.Stop();
			}
			ringToken = "";
			return;
		}
		AudioClip val = mod.Sounds.Clip(call: true, mod.Sounds.Preferences.CallTone);
		if ((Object)(object)val == (Object)null)
		{
			if ((Object)(object)ringtone != (Object)null)
			{
				ringtone.Stop();
			}
			ringToken = "";
			return;
		}
		if ((Object)(object)ringtone == (Object)null)
		{
			CreateRing();
		}
		if ((Object)(object)ringtone.clip != (Object)(object)val)
		{
			ringtone.Stop();
			ringtone.clip = val;
			ringToken = "";
		}
		if (ringToken != call.Id)
		{
			ringToken = call.Id;
			ringtone.volume = mod.Sounds.Preferences.RingVolume * (call.Incoming ? 1f : 0.3f);
			ringtone.Play();
		}
	}

	private void CreateRing()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		ringObject = new GameObject("RP_LocalPhoneRingtone");
		ringtone = ringObject.AddComponent<AudioSource>();
		ringtone.spatialBlend = 0f;
		ringtone.loop = true;
		ringtone.playOnAwake = false;
	}

	public void ReceiveSpeaker(PhoneMod mod, PhoneSpeakerPacket packet)
	{
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		if (!CanListen || !packet.Read(out var text, out var device, out var call, out var actor, out var seq, out var audio) || mod.View.Call.Id == call || !ValidCompressedFrame(audio))
		{
			return;
		}
		GameObject val = PhoneDevices.Resolve(device);
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if ((Object)(object)val == (Object)null || (Object)(object)Player.Head == (Object)null || playerID == null || !playerID.IsValid || !PhoneMod.Within(Player.Head.position, val.transform.position, 2f))
		{
			return;
		}
		string key = text + "/" + actor;
		if (!broadcasts.TryGetValue(key, out var value))
		{
			if (broadcasts.Count >= 32)
			{
				return;
			}
			PhoneAudioPlayer phoneAudioPlayer = new PhoneAudioPlayer();
			((Object)phoneAudioPlayer.gameObject).name = "RP_PhoneSpeaker";
			phoneAudioPlayer.AudioSource.spatialBlend = 1f;
			phoneAudioPlayer.AudioSource.minDistance = 0.15f;
			phoneAudioPlayer.AudioSource.maxDistance = 2f;
			phoneAudioPlayer.AudioSource.rolloffMode = (AudioRolloffMode)1;
			phoneAudioPlayer.AudioSource.dopplerLevel = 0f;
			value = (broadcasts[key] = new Broadcast
			{
				Source = phoneAudioPlayer,
				Device = device
			});
		}
		value.Sequence = seq;
		value.Last = Time.realtimeSinceStartup;
		value.Source.transform.position = val.transform.position;
		value.Actor = actor;
		value.Buffer.Push(seq, audio, Time.realtimeSinceStartup);
	}

	private void TickSpeakers()
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		KeyValuePair<string, Broadcast>[] array = broadcasts.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, Broadcast> keyValuePair = array[i];
			Broadcast b = keyValuePair.Value;
			GameObject val = PhoneDevices.Resolve(b.Device);
			if (!CanListen || (Object)(object)val == (Object)null || (Object)(object)Player.Head == (Object)null || !PhoneMod.Within(Player.Head.position, val.transform.position, 2f) || Time.realtimeSinceStartup - b.Last > 2f)
			{
				Drop(keyValuePair.Key);
				continue;
			}
			b.Source.transform.position = val.transform.position;
			b.Buffer.Drain(Time.realtimeSinceStartup, delegate(byte[] bytes)
			{
				Play(b.Source, b.Actor, bytes);
			}, delegate(int gap)
			{
				b.Source.Conceal(gap);
			});
		}
	}

	private void Drop(string key)
	{
		Broadcast broadcast = broadcasts[key];
		broadcast.Source.Dispose();
		broadcasts.Remove(key);
	}

	private void StopEarpiece()
	{
		privateBuffer.Clear();
		if (earpiece != null)
		{
			earpiece.Dispose();
			earpiece = null;
		}
	}

	public void Dispose()
	{
		StopEarpiece();
		StopLiveSource();
		liveSend = (liveReceive = "");
		liveSent = 0u;
		token = (ringToken = "");
		sequence = (received = 0u);
		if ((Object)(object)ringtone != (Object)null)
		{
			ringtone.Stop();
		}
		if ((Object)(object)ringObject != (Object)null)
		{
			Object.Destroy((Object)(object)ringObject);
		}
		string[] array = broadcasts.Keys.ToArray();
		foreach (string key in array)
		{
			Drop(key);
		}
		ringtone = null;
		ringObject = null;
	}
}
