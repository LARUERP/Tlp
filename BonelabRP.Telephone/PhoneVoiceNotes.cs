using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LabFusion.Player;
using LabFusion.Utilities;
using LabFusion.Voice;
using LabFusion.Voice.Unity;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneVoiceNotes : IDisposable
{
	private readonly PhoneMod mod;

	private readonly PhoneNoteStore local;

	private readonly PhoneNoteStore server;

	private readonly Dictionary<ulong, float> uploads = new Dictionary<ulong, float>();

	private PhoneNoteRecorder recorder;

	private float started;

	private float deadline;

	private ulong owner;

	private ushort? device;

	private string access = "";

	private string upload = "";

	private string recipient = "";

	private string pending = "";

	private float downloadAt;

	private int downloadTries;

	private readonly Dictionary<ulong, float> fetches = new Dictionary<ulong, float>();

	private static readonly FieldInfo microphoneClip = typeof(UnityVoiceReceiver).GetField("_voiceClip", BindingFlags.Instance | BindingFlags.NonPublic);

	private int capturedSamples;

	private AudioClip inputClip;

	private int inputCursor;

	private float lastInput;

	private byte[] draft;

	private GameObject go;

	private AudioSource source;

	private AudioClip clip;

	public string DownloadingId => pending;

	public string PlayingId { get; private set; } = "";

	public string MicrophoneStatus { get; private set; } = "";

	public float InputLevel { get; private set; }

	public bool Recording => recorder != null;

	public bool Draft => draft != null;

	public bool Sending => upload != "";

	public float Elapsed => Recording ? Math.Min(15f, Time.realtimeSinceStartup - started) : PhoneVoiceNoteRules.Duration(draft);

	private bool Scope => mod.ScreenUnlocked && mod.ScreenOwner == owner && mod.ScreenDevice == device && mod.Access.Token == access;

	public PhoneVoiceNotes(PhoneMod mod, string dir)
	{
		this.mod = mod;
		local = new PhoneNoteStore(Path.Combine(dir, "voice-cache"), 2000, 2000);
		server = new PhoneNoteStore(Path.Combine(dir, "server-voice"));
	}

	public void Start()
	{
		if (!mod.ScreenUnlocked || !PhoneVoice.Ready || VoiceInfo.IsMuted || microphoneClip == null || VoiceInfo.VoiceManager.GetReceiver() == null)
		{
			throw new InvalidOperationException("Activer le micro Fusion et déverrouiller le téléphone.");
		}
		if (mod.CallActive || mod.View.Call.State != "Idle" || mod.IsCasting || mod.IsWatching || Sending)
		{
			throw new InvalidOperationException("Quitter l’appel ou le live avant d’enregistrer.");
		}
		StopPlayback();
		draft = null;
		owner = mod.ScreenOwner;
		device = mod.ScreenDevice;
		access = mod.Access.Token;
		recorder = new PhoneNoteRecorder(48000);
		inputClip = null;
		capturedSamples = 0;
		MicrophoneStatus = "Démarrage du micro…";
		InputLevel = 0f;
		started = Time.realtimeSinceStartup;
		lastInput = started;
		ReadMicrophone(prime: true);
		mod.RefreshApp();
	}

	public bool Capture(byte[] audio)
	{
		return Recording;
	}

	private void ReadMicrophone(bool prime = false)
	{
		if (!Recording || !Scope)
		{
			return;
		}
		if (VoiceInfo.IsMuted)
		{
			throw new InvalidOperationException("Activer le micro Fusion avant d’enregistrer.");
		}
		IVoiceManager voiceManager = VoiceInfo.VoiceManager;
		IVoiceReceiver val = ((voiceManager != null) ? voiceManager.GetReceiver() : null);
		AudioClip val2 = (AudioClip)((val == null) ? null : /*isinst with value type is only supported in some contexts*/);
		if ((Object)(object)val2 == (Object)null)
		{
			if (!(Time.realtimeSinceStartup - started > 2f))
			{
				return;
			}
			throw new InvalidOperationException("Fusion n’a pas ouvert le micro. Vérifier le périphérique sélectionné.");
		}
		string validMicrophoneName = UnityVoiceReceiver.GetValidMicrophoneName();
		int position = Microphone.GetPosition(validMicrophoneName);
		if (!Microphone.IsRecording(validMicrophoneName))
		{
			if (Time.realtimeSinceStartup - started > 2f)
			{
				throw new InvalidOperationException("Le micro Fusion n’est pas en cours de capture.");
			}
		}
		else
		{
			if (position < 0 || position >= val2.samples)
			{
				return;
			}
			if (prime || (Object)(object)inputClip != (Object)(object)val2)
			{
				recorder = new PhoneNoteRecorder((val2.frequency >= 48000) ? 48000 : ((val2.frequency >= 32000) ? 32000 : ((val2.frequency >= 16000) ? 16000 : 8000)));
				inputClip = val2;
				inputCursor = position;
				lastInput = Time.realtimeSinceStartup;
				return;
			}
			int num = PhoneMicrophoneRules.Available(inputCursor, position, val2.samples);
			if (num == 0)
			{
				if (!(Time.realtimeSinceStartup - lastInput > 2f))
				{
					return;
				}
				throw new InvalidOperationException("Le curseur du micro reste immobile. Vérifier le périphérique Fusion.");
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup - lastInput > (float)val2.samples / (float)val2.frequency)
			{
				inputCursor = position;
				lastInput = realtimeSinceStartup;
				return;
			}
			Il2CppStructArray<float> val3 = new Il2CppStructArray<float>((long)(num * val2.channels));
			if (!val2.GetData(val3, inputCursor))
			{
				throw new InvalidOperationException("Lecture du micro impossible.");
			}
			float[] array = new float[((Il2CppArrayBase<float>)(object)val3).Length];
			InteropUtilities.Copy(((Il2CppObjectBase)val3).Pointer, IntPtr.Size, ((Il2CppArrayBase<float>)(object)val3).Length, array);
			inputCursor = position;
			lastInput = realtimeSinceStartup;
			capturedSamples += num;
			MicrophoneStatus = "Micro actif";
			short[] array2 = PhoneMicrophoneRules.Mono(array, val2.channels);
			InputLevel = Math.Min(1f, PhoneMicrophoneRules.Peak(array2) * 10f);
			recorder.Capture(mod.Ambient.Mix(mod, array2, val2.frequency), val2.frequency, realtimeSinceStartup - started);
		}
	}

	public void Stop()
	{
		if (Recording)
		{
			ReadMicrophone();
			PhoneNoteRecorder phoneNoteRecorder = recorder;
			recorder = null;
			if (!Scope)
			{
				draft = null;
				return;
			}
			byte[] bytes = phoneNoteRecorder.Finish(Time.realtimeSinceStartup - started);
			draft = PhoneMicrophoneRules.Audible(bytes);
			MicrophoneStatus = "Vocal prêt";
			InputLevel = 0f;
			mod.RefreshApp();
		}
	}

	public void Send(string number)
	{
		if (!Scope || draft == null || Sending)
		{
			throw new InvalidOperationException("Enregistrer un vocal avant de l’envoyer.");
		}
		recipient = PhoneService.Normalize(number);
		upload = Guid.NewGuid().ToString("N");
		deadline = Time.realtimeSinceStartup + 30f;
		try
		{
			mod.Media.QueueVoice(null, PhoneMediaRules.Split(mod.View.Session, upload, "voiceUpload", draft));
		}
		catch
		{
			upload = "";
			throw;
		}
		mod.RefreshApp();
	}

	public bool Notice(PhoneNotice n)
	{
		if (n.Number != upload || upload == "")
		{
			return false;
		}
		if (n.Kind == "uploadFailed")
		{
			upload = "";
			mod.LocalStatus(n.Text);
			return true;
		}
		if (n.Kind != "uploadReady")
		{
			return false;
		}
		string text = upload;
		upload = "";
		if (Scope)
		{
			local.Save(0uL, text, draft);
			mod.Request("sms", recipient, "", "", "", enabled: false, 0L, "", 0, text);
			draft = null;
			mod.VoiceSent();
			mod.RefreshApp();
		}
		return true;
	}

	public void SaveUpload(ulong actor, PhoneMediaChunk chunk, byte[] bytes)
	{
		ulong num = mod.UploadOwner(actor, chunk);
		if (!server.Owns(num, chunk.Id))
		{
			if (uploads.TryGetValue(actor, out var value) && Time.realtimeSinceStartup - value < 2f)
			{
				throw new InvalidOperationException("Attendre avant d’envoyer un autre vocal.");
			}
			uploads[actor] = Time.realtimeSinceStartup;
		}
		server.Save(num, chunk.Id, bytes);
		mod.MediaUploadReady(PlayerIDManager.GetPlayerID(actor), chunk.Id);
	}

	public int Duration(ulong o, string id)
	{
		if (!server.Owns(o, id))
		{
			throw new InvalidOperationException("Vocal indisponible pour cet expéditeur.");
		}
		return (int)Math.Ceiling(PhoneVoiceNoteRules.Duration(server.Read(id)));
	}

	public void Fetch(ulong actor, string id)
	{
		if (fetches.TryGetValue(actor, out var value) && Time.realtimeSinceStartup - value < 2f)
		{
			throw new InvalidOperationException("Attendre le chargement du vocal.");
		}
		fetches[actor] = Time.realtimeSinceStartup;
		if (!mod.CanReadVoice(actor, id))
		{
			throw new InvalidOperationException("Accès à ce vocal refusé.");
		}
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID != null && playerID.IsValid)
		{
			mod.Media.QueueVoice(playerID.SmallID, PhoneMediaRules.Split(mod.ServerSession, id, "voiceDownload", server.Read(id)));
		}
	}

	public void Receive(string id, byte[] bytes)
	{
		if (mod.IsVoiceKnown(id))
		{
			local.Save(0uL, id, bytes);
			if (pending == id)
			{
				pending = "";
				Play(bytes);
				PlayingId = id;
			}
			mod.RefreshApp();
		}
	}

	public void Listen(string id)
	{
		if (mod.IsVoiceKnown(id) && !(pending == id))
		{
			StopPlayback();
			if (local.TryRead(id, out var bytes))
			{
				Play(bytes);
				PlayingId = id;
				mod.RefreshApp();
			}
			else
			{
				local.Forget(id);
				pending = id;
				downloadTries = 0;
				FetchPending();
			}
		}
	}

	private void FetchPending()
	{
		downloadAt = Time.realtimeSinceStartup;
		downloadTries++;
		mod.LocalStatus("Chargement du vocal…");
		mod.Request("voiceFetch", "", "", "", "", enabled: false, 0L, "", 0, pending);
		mod.RefreshApp();
	}

	public void Preview()
	{
		if (Scope && draft != null)
		{
			StopPlayback();
			Play(draft);
		}
	}

	private void Play(byte[] bytes)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		if (!mod.ScreenUnlocked)
		{
			throw new InvalidOperationException("Déverrouiller le téléphone.");
		}
		float[] array = PhoneVoiceNoteRules.Decode(bytes);
		StopPlayback();
		if ((Object)(object)go != (Object)null)
		{
			Object.Destroy((Object)(object)go);
		}
		go = new GameObject("RP_VoiceMessagePlayback");
		source = go.AddComponent<AudioSource>();
		source.spatialBlend = 0f;
		source.dopplerLevel = 0f;
		source.playOnAwake = false;
		source.loop = false;
		source.bypassReverbZones = true;
		source.reverbZoneMix = 0f;
		clip = AudioClip.Create("WindyPhone_VoiceNote", array.Length, 1, PhoneVoiceNoteRules.SampleRate(bytes), false);
		clip.SetData(Il2CppStructArray<float>.op_Implicit(array), 0);
		source.clip = clip;
		source.volume = 0.9f;
		source.Play();
	}

	public void StopPlayback()
	{
		PlayingId = "";
		pending = "";
		if ((Object)(object)source != (Object)null)
		{
			source.Stop();
			source.clip = null;
		}
		if ((Object)(object)clip != (Object)null)
		{
			Object.Destroy((Object)(object)clip);
			clip = null;
		}
	}

	public void Tick()
	{
		if (pending != "" && Time.realtimeSinceStartup - downloadAt >= 10f)
		{
			if (!mod.IsVoiceKnown(pending))
			{
				StopPlayback();
			}
			else if (downloadTries < 3)
			{
				FetchPending();
			}
			else
			{
				string text = pending;
				StopPlayback();
				mod.MediaWarning("Téléchargement vocal expiré : " + text);
				mod.LocalStatus("Téléchargement du vocal expiré. Réessayer.");
			}
		}
		if (PlayingId != "" && (Object)(object)source != (Object)null && !source.isPlaying)
		{
			PlayingId = "";
			mod.RefreshApp();
		}
		if ((Recording || Draft || Sending) && !Scope)
		{
			Clear();
			return;
		}
		if (Recording)
		{
			try
			{
				ReadMicrophone();
				if (Time.realtimeSinceStartup - started >= 15f)
				{
					Stop();
				}
			}
			catch (Exception ex)
			{
				recorder = null;
				draft = null;
				InputLevel = 0f;
				MicrophoneStatus = ex.Message;
				mod.MediaWarning("Capture vocal : " + ex.Message + " | échantillons=" + capturedSamples);
				mod.LocalStatus(ex.Message);
			}
		}
		if (Sending && Time.realtimeSinceStartup > deadline)
		{
			upload = "";
			mod.LocalStatus("Envoi du vocal expiré.");
		}
		if (!mod.ScreenUnlocked)
		{
			StopPlayback();
		}
	}

	public void Clear()
	{
		MicrophoneStatus = "";
		capturedSamples = 0;
		inputClip = null;
		InputLevel = 0f;
		recorder = null;
		draft = null;
		upload = "";
		StopPlayback();
		mod.RefreshApp();
	}

	public void Dispose()
	{
		Clear();
		if ((Object)(object)go != (Object)null)
		{
			Object.Destroy((Object)(object)go);
		}
		go = null;
		source = null;
	}
}
