using BoneLib;
using LabFusion.Audio;
using LabFusion.Player;
using LabFusion.Voice;
using LabFusion.Voice.Unity;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneAmbientCapture
{
	private readonly PhoneAmbientBuffer buffer = new PhoneAmbientBuffer();

	private string scope = "";

	private string Scope(PhoneMod mod)
	{
		PhoneVoiceNotes notes = mod.Notes;
		return (notes != null && notes.Recording) ? ("note/" + mod.ScreenOwner + "/" + mod.Access.Token) : (mod.IsCasting ? ("live/" + mod.Live.Active.Id) : ((mod.CallActive && mod.View.Call.Speaker) ? ("call/" + mod.View.Call.Id) : ""));
	}

	private bool Nearby(PhoneMod mod, ulong actor)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID == null || !playerID.IsValid || playerID.IsMe || (mod.CallActive && mod.View.Call.Peer == actor) || !PhoneMod.VoiceHead(playerID, out var head))
		{
			return false;
		}
		Vector3 position;
		if (mod.ScreenDevice.HasValue)
		{
			GameObject val = PhoneDevices.Resolve(mod.ScreenDevice.Value);
			if ((Object)(object)val == (Object)null)
			{
				return false;
			}
			position = val.transform.position;
		}
		else
		{
			if ((Object)(object)Player.Head == (Object)null)
			{
				return false;
			}
			position = Player.Head.position;
		}
		return PhoneMod.Within(head, position, 2f);
	}

	private bool Ready(PhoneMod mod)
	{
		string text = Scope(mod);
		if (text != scope)
		{
			scope = text;
			buffer.Clear();
		}
		return text != "" && !VoiceInfo.IsDeafened;
	}

	public void Receive(PhoneMod mod, UnityVoiceSpeaker speaker, byte[] data)
	{
		if (Ready(mod) && ((VoiceSpeaker)speaker).ID != null && Nearby(mod, ((VoiceSpeaker)speaker).ID.PlatformID) && PhoneVoiceGuard.Valid(data))
		{
			buffer.Add(((VoiceSpeaker)speaker).ID.PlatformID, VoiceConverter.Decode(data), AudioInfo.OutputSampleRate, ((VoiceSpeaker)speaker).Volume * 0.5f, Time.realtimeSinceStartup);
		}
	}

	public short[] Mix(PhoneMod mod, short[] own, int rate)
	{
		return Ready(mod) ? buffer.Mix(own, rate, Time.realtimeSinceStartup, (ulong id) => Nearby(mod, id)) : own;
	}
}
