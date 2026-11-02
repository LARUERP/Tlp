using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BoneLib;
using BoneLib.BoneMenu;
using Il2CppSLZ.Marrow;
using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Metadata;
using LabFusion.Scene;
using LabRP.Economy;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneMod : MelonMod
{
	private readonly PhoneAccess access = new PhoneAccess();

	private readonly PhoneAccess bankAttempts = new PhoneAccess();

	private readonly Dictionary<(ulong Actor, ulong Owner, ushort Device), bool> heldChecks = new Dictionary<(ulong, ulong, ushort), bool>();

	private readonly PhoneLive lives = new PhoneLive();

	private readonly ConcurrentQueue<PhoneAccessView> accessViews = new ConcurrentQueue<PhoneAccessView>();

	private readonly ConcurrentQueue<WindyLiveView> liveViews = new ConcurrentQueue<WindyLiveView>();

	private readonly ConcurrentQueue<(ulong Actor, string Id, uint Seq, byte[] Audio, bool Host)> liveAudio = new ConcurrentQueue<(ulong, string, uint, byte[], bool)>();

	private readonly Dictionary<ulong, PhoneDeviceInfo> inspected = new Dictionary<ulong, PhoneDeviceInfo>();

	private readonly Dictionary<ulong, ulong> callIdentities = new Dictionary<ulong, ulong>();

	private readonly Dictionary<ulong, int> inspectedEpoch = new Dictionary<ulong, int>();

	private int accessEpoch;

	private long lastAccessRevision;

	private readonly Dictionary<ulong, (string Id, PhoneAudioWindow Window)> liveSequences = new Dictionary<ulong, (string, PhoneAudioWindow)>();

	private float nextLivePulse;

	private float accessAt;

	private float liveAt;

	private float nextAccessPulse;

	private float nextSelection;

	private long accessRevision;

	private long liveRevision;

	private string catalogKey = "";

	private readonly ConcurrentQueue<(ulong Player, PhoneCommand Command)> commands = new ConcurrentQueue<(ulong, PhoneCommand)>();

	private readonly ConcurrentQueue<PhoneView> snapshots = new ConcurrentQueue<PhoneView>();

	private readonly PhoneDevices devices = new PhoneDevices();

	private readonly Dictionary<ulong, float> lastCommands = new Dictionary<ulong, float>();

	private readonly HashSet<ulong> online = new HashSet<ulong>();

	private readonly Dictionary<ulong, string> deviceStatuses = new Dictionary<ulong, string>();

	private readonly HashSet<string> retiredSessions = new HashSet<string>();

	private readonly PhoneCalls calls = new PhoneCalls();

	private readonly ConcurrentQueue<PhoneCallUpdate> callUpdates = new ConcurrentQueue<PhoneCallUpdate>();

	private readonly ConcurrentQueue<(ulong Actor, string Call, uint Sequence, byte[] Audio, bool Host)> voiceFrames = new ConcurrentQueue<(ulong, string, uint, byte[], bool)>();

	private readonly Dictionary<ulong, bool> voiceReady = new Dictionary<ulong, bool>();

	private readonly Dictionary<ulong, Queue<string>> callNonces = new Dictionary<ulong, Queue<string>>();

	private readonly Dictionary<ulong, (string Call, PhoneAudioWindow Window)> audioSequences = new Dictionary<ulong, (string, PhoneAudioWindow)>();

	private readonly HashSet<string> retiredCalls = new HashSet<string>();

	private PhoneVoice voice;

	private PhoneWindy windy;

	private readonly ConcurrentQueue<WindyView> windyViews = new ConcurrentQueue<WindyView>();

	private readonly Dictionary<ulong, int> windyOffsets = new Dictionary<ulong, int>();

	private readonly Dictionary<ulong, float> lastWindyPosts = new Dictionary<ulong, float>();

	private readonly HashSet<string> publicPhotos = new HashSet<string>();

	private long windyRevision;

	public readonly List<PhoneNotice> SharedNumbers = new List<PhoneNotice>();

	private readonly ConcurrentQueue<PhoneNotice> notices = new ConcurrentQueue<PhoneNotice>();

	private readonly ConcurrentQueue<PhoneSpeakerPacket> speakers = new ConcurrentQueue<PhoneSpeakerPacket>();

	private readonly HashSet<string> seenNotices = new HashSet<string>();

	private readonly Dictionary<ulong, bool> listenReady = new Dictionary<ulong, bool>();

	private readonly Dictionary<ulong, float> lastShares = new Dictionary<ulong, float>();

	private long callRevision;

	private float nextHeartbeat;

	private float lastCallSync;

	private PhoneService service;

	private PhoneConfiguration configuration = new PhoneConfiguration();

	private PhoneApp app;

	private PhoneSurface surface;

	private ushort? bound;

	private string screenKey = "";

	private string session = "";

	private string scene = "";

	private string directory;

	private bool connected;

	private bool wasHost;

	private bool loading;

	private float nextTick;

	private float nextSync;

	private float nextWarning;

	private long revision;

	private FunctionElement menuNumber;

	private FunctionElement menuStatus;

	private Page menuRoot;

	private Page menuAdmin;

	private bool adminVisible;

	private readonly PhoneParking parking = new PhoneParking();

	public PhoneAccessView Access { get; private set; } = new PhoneAccessView();

	public WindyLiveView Live { get; private set; } = new WindyLiveView();

	public PhoneView ScreenProfile => ScreenUnlocked ? Access.Profile : null;

	public bool ScreenUnlocked => Access.Token != "" && Access.Profile != null && Access.Device == bound && surface != null && (surface.IsHeld || IsCasting) && Time.realtimeSinceStartup - accessAt < 8f;

	public ulong ScreenOwner { get; private set; }

	public string VisibleWallpaper => (Access.Owner == ScreenOwner && Access.Device == bound) ? (Access.Wallpaper ?? "") : "";

	public ushort? ScreenDevice => bound;

	public bool ScreenPinSet
	{
		get
		{
			PhoneDeviceInfo? phoneDeviceInfo = View.Devices.FirstOrDefault((PhoneDeviceInfo d) => d.Device == bound);
			return (phoneDeviceInfo != null && phoneDeviceInfo.PinSet) || (Access.Device == bound && Access.PinSet);
		}
	}

	public bool IsCasting => !loading && View.Scene == scene && Live.Casting && Live.Active != null && Live.Active.Host == PlayerIDManager.LocalPlatformID && surface != null && (Object)(object)surface.Body != (Object)null && bound == Live.Active.Device && View.Devices.Any((PhoneDeviceInfo d) => d.Owner == Live.Active.Owner && d.Device == Live.Active.Device) && Time.realtimeSinceStartup - liveAt < 10f;

	public bool IsWatching => ScreenUnlocked && !Live.Casting && Live.Active != null && Time.realtimeSinceStartup - liveAt < 10f;

	public List<string> Gallery => ScreenUnlocked ? Access.Gallery.Concat(from id in Media.Photos.Captured(ScreenOwner)
		where !Access.Deleted.Contains(id)
		select id).Distinct().ToList() : new List<string>();

	public static PhoneMod Instance { get; private set; }

	public PhoneView View { get; private set; } = new PhoneView();

	public string Status { get; private set; } = "Rejoindre un lobby Fusion.";

	public WindyView Windy { get; private set; } = new WindyView();

	public PhoneMedia Media { get; private set; }

	public string ServerSession => session;

	public PhoneSounds Sounds { get; private set; }

	public bool CallMicMuted { get; private set; }

	public bool CallActive => View.Call != null && View.Call.State == "Active" && Time.realtimeSinceStartup - lastCallSync < 12f;

	public PhoneVoiceNotes Notes { get; private set; }

	public PhoneAmbientCapture Ambient { get; } = new PhoneAmbientCapture();

	private bool LiveSourceAvailable(ulong actor, ulong owner, ushort device)
	{
		return !loading && online.Contains(actor) && devices.Id(owner) == device;
	}

	private bool CastingValid(ulong actor, string id)
	{
		return lives.SourceValid(actor, id, LiveSourceAvailable);
	}

	public void EnqueueAccess(PhoneAccessView v)
	{
		if (accessViews.Count < 32)
		{
			accessViews.Enqueue(v);
		}
	}

	public void EnqueueLive(WindyLiveView v)
	{
		if (liveViews.Count < 32)
		{
			liveViews.Enqueue(v);
		}
	}

	public void EnqueueLiveVoice(ulong a, string id, uint seq, byte[] bytes, bool host)
	{
		if (liveAudio.Count < 64)
		{
			liveAudio.Enqueue((a, id, seq, bytes, host));
		}
	}

	private static bool HandHolds(Hand hand, GameObject body)
	{
		if ((Object)(object)body == (Object)null)
		{
			return false;
		}
		GameObject val = ((hand != null) ? hand.m_CurrentAttachedGO : null);
		return (Object)(object)val != (Object)null && ((Object)(object)val == (Object)(object)body || val.transform.IsChildOf(body.transform) || body.transform.IsChildOf(val.transform));
	}

	private bool ServerHolds(ulong actor, ulong owner, ushort device)
	{
		(ulong, ulong, ushort) key = (actor, owner, device);
		if (heldChecks.TryGetValue(key, out var value))
		{
			return value;
		}
		value = CheckServerHands(actor, owner, device);
		heldChecks[key] = value;
		return value;
	}

	private bool CheckServerHands(ulong actor, ulong owner, ushort device)
	{
		if (loading || devices.Id(owner) != device)
		{
			return false;
		}
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		NetworkPlayer val = default(NetworkPlayer);
		if (playerID == null || !playerID.IsValid || !NetworkPlayerManager.TryGetPlayer(playerID.SmallID, ref val) || !val.HasRig)
		{
			return false;
		}
		GameObject body = PhoneDevices.Resolve(device);
		return HandHolds(val.RigRefs.LeftHand, body) || HandHolds(val.RigRefs.RightHand, body);
	}

	private bool LeaseHeld(ulong actor, ulong owner, ushort device)
	{
		return ServerHolds(actor, owner, device) || lives.SourceValid(actor, lives.Casting(actor), (ulong a, ulong o, ushort d) => o == owner && d == device && LiveSourceAvailable(a, o, d));
	}

	private bool LeaseValid(ulong actor)
	{
		PhoneAccess.Lease lease = access.Get(actor);
		return lease != null && access.Valid(actor, lease.Owner, lease.Device, lease.Token, LeaseHeld(actor, lease.Owner, lease.Device), Time.realtimeSinceStartup);
	}

	public ulong UploadOwner(ulong actor, PhoneMediaChunk chunk)
	{
		if (!chunk.Device.HasValue || !access.Valid(actor, chunk.Owner, chunk.Device.Value, chunk.Access, ServerHolds(actor, chunk.Owner, chunk.Device.Value), Time.realtimeSinceStartup))
		{
			throw new InvalidOperationException("Déverrouiller le téléphone avant d'envoyer une photo.");
		}
		return chunk.Owner;
	}

	private ulong AuthorizedOwner(ulong actor, PhoneCommand c)
	{
		if (!c.Device.HasValue || !access.Valid(actor, c.Owner, c.Device.Value, c.Access, LeaseHeld(actor, c.Owner, c.Device.Value), Time.realtimeSinceStartup))
		{
			throw new InvalidOperationException("Téléphone verrouillé. Saisir son code.");
		}
		return c.Owner;
	}

	private bool HandleAccess(ulong actor, PhoneCommand c)
	{
		if (c.Operation != "phoneInspect" && c.Operation != "unlock" && c.Operation != "pin" && c.Operation != "lock" && c.Operation != "phonePulse")
		{
			return false;
		}
		if (!c.Device.HasValue || devices.Id(c.Owner) != c.Device)
		{
			throw new InvalidOperationException("Téléphone introuvable.");
		}
		if (c.Operation == "lock")
		{
			access.Lock(actor);
			lives.Disconnect(actor);
			CloseBorrowedCall(actor);
			PublishAccess(actor);
			PublishLives();
			return true;
		}
		if (c.Operation == "phonePulse" && !ServerHolds(actor, c.Owner, c.Device.Value) && LeaseHeld(actor, c.Owner, c.Device.Value))
		{
			if (!access.Renew(actor, c.Owner, c.Device.Value, c.Access, held: true, Time.realtimeSinceStartup))
			{
				throw new InvalidOperationException("Téléphone verrouillé.");
			}
			lives.Pulse(actor, Time.realtimeSinceStartup);
			PublishAccess(actor, "", light: true);
			PublishLive(actor);
			return true;
		}
		if (!ServerHolds(actor, c.Owner, c.Device.Value))
		{
			throw new InvalidOperationException("Tenir le téléphone, puis réessayer.");
		}
		inspected[actor] = new PhoneDeviceInfo
		{
			Owner = c.Owner,
			Device = c.Device.Value
		};
		inspectedEpoch[actor] = c.Epoch;
		if (c.Operation == "pin")
		{
			if (actor != c.Owner)
			{
				throw new InvalidOperationException("Seul le propriétaire peut définir ou modifier le code.");
			}
			if (service.PinSet(c.Owner))
			{
				AuthorizedOwner(actor, c);
			}
			service.SetPin(c.Owner, c.Text);
			access.Lock(actor);
			access.Unlock(actor, c.Owner, c.Device.Value, c.Text, held: true, (string p) => service.VerifyPin(c.Owner, p), Time.realtimeSinceStartup);
			foreach (ulong item in online)
			{
				Publish(item);
			}
		}
		else if (c.Operation == "unlock")
		{
			access.Unlock(actor, c.Owner, c.Device.Value, c.Text, held: true, (string p) => service.VerifyPin(c.Owner, p), Time.realtimeSinceStartup);
		}
		else if (c.Operation == "phonePulse")
		{
			if (access.Renew(actor, c.Owner, c.Device.Value, c.Access, held: true, Time.realtimeSinceStartup))
			{
				lives.Pulse(actor, Time.realtimeSinceStartup);
			}
			else
			{
				lives.Disconnect(actor);
			}
			PublishAccess(actor, "", light: true);
			PublishLive(actor);
			return true;
		}
		PublishAccess(actor);
		return true;
	}

	private bool HandleBankAccess(ulong actor, ulong owner, PhoneCommand c)
	{
		if (c.Operation != "bankUnlock" && c.Operation != "bankPin" && c.Operation != "bankLock")
		{
			return false;
		}
		PhoneAccess.Lease lease = access.Get(actor);
		if (c.Operation == "bankLock")
		{
			lease.BankAuthorized = false;
			PublishAccess(actor);
			return true;
		}
		if (c.Operation == "bankPin")
		{
			if (actor != owner)
			{
				throw new InvalidOperationException("Seul le propriétaire peut créer le code bancaire.");
			}
			if (service.BankPinSet(owner))
			{
				throw new InvalidOperationException("Un code bancaire existe déjà.");
			}
			service.SetBankPin(owner, c.Text);
		}
		bankAttempts.Unlock(actor, owner, c.Device.Value, c.Text, held: true, (string p) => service.VerifyBankPin(owner, p), Time.realtimeSinceStartup);
		bankAttempts.Lock(actor);
		lease.BankAuthorized = true;
		PublishAccess(actor, "Application Banque déverrouillée.");
		return true;
	}

	private void PublishAccess(ulong actor, string notice = "", bool light = false)
	{
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID == null || !playerID.IsValid || service == null)
		{
			return;
		}
		PhoneAccess.Lease lease = (LeaseValid(actor) ? access.Get(actor) : null);
		inspected.TryGetValue(actor, out var value);
		int value2;
		PhoneAccessView phoneAccessView = new PhoneAccessView
		{
			Epoch = (inspectedEpoch.TryGetValue(actor, out value2) ? value2 : 0),
			Player = actor,
			Session = session,
			Revision = ++accessRevision,
			Owner = (lease?.Owner ?? value?.Owner ?? actor),
			Device = (lease?.Device ?? value?.Device ?? 0),
			Notice = notice
		};
		phoneAccessView.BankPinSet = service.BankPinSet(phoneAccessView.Owner);
		phoneAccessView.BankUnlocked = lease?.BankAuthorized ?? false;
		phoneAccessView.PinSet = service.PinSet(phoneAccessView.Owner);
		phoneAccessView.Wallpaper = service.Wallpaper(phoneAccessView.Owner);
		if (lease != null)
		{
			phoneAccessView.Token = lease.Token;
			if (!light)
			{
				phoneAccessView.Profile = service.PrivateView(lease.Owner);
				phoneAccessView.Profile.BankReady = ServerEconomy.IsHosting;
				phoneAccessView.Profile.Balance = ((phoneAccessView.BankUnlocked && phoneAccessView.Profile.BankReady) ? ServerEconomy.GetBalance(lease.Owner) : 0);
				if (!phoneAccessView.BankUnlocked)
				{
					phoneAccessView.Profile.Transactions.Clear();
				}
				phoneAccessView.Windy = windy?.View(lease.Owner, windyOffsets.TryGetValue(actor, out var value3) ? value3 : 0);
				phoneAccessView.Gallery = service.Gallery(lease.Owner);
				phoneAccessView.Deleted = service.DeletedPhotos(lease.Owner);
				if (phoneAccessView.Windy != null)
				{
					phoneAccessView.Windy.Player = actor;
					phoneAccessView.Windy.Session = session;
					phoneAccessView.Windy.Revision = ++windyRevision;
				}
			}
		}
		if (playerID.IsMe)
		{
			AcceptAccess(phoneAccessView);
		}
		else
		{
			PhoneLiveTransport.Access(phoneAccessView, playerID.SmallID);
		}
	}

	private void AcceptAccess(PhoneAccessView v)
	{
		if (v.Protocol != 16 || v.Player != PlayerIDManager.LocalPlatformID || v.Session != View.Session || v.Device != bound || v.Owner != ScreenOwner || v.Epoch != accessEpoch || v.Revision <= lastAccessRevision || (v.Token != "" && !Guid.TryParseExact(v.Token, "N", out var _)))
		{
			return;
		}
		if (v.Token != "" && v.Token == Access.Token && v.Profile == null)
		{
			v.Profile = Access.Profile;
			v.Gallery = Access.Gallery;
			v.Deleted = Access.Deleted;
		}
		bool flag = v.BankUnlocked != Access.BankUnlocked || v.BankPinSet != Access.BankPinSet || v.Wallpaper != Access.Wallpaper || v.PinSet != Access.PinSet || !v.Gallery.SequenceEqual(Access.Gallery) || v.Token != Access.Token || (v.Profile != null && JsonSerializer.Serialize(v.Profile) != JsonSerializer.Serialize(Access.Profile));
		lastAccessRevision = v.Revision;
		PhoneView old = Access.Profile;
		Access = v;
		accessAt = Time.realtimeSinceStartup;
		if (v.Token == "" && old != null)
		{
			app?.ForgetPrivate();
			Media?.ClearPrivate();
		}
		if (v.Windy != null)
		{
			AcceptWindy(v.Windy);
		}
		if (old != null && v.Profile != null)
		{
			foreach (PhoneSms item in v.Profile.Messages.Where((PhoneSms m) => m.To == v.Profile.Number && !old.Messages.Any((PhoneSms o) => o.Id == m.Id)))
			{
				LocalPopup("Messages", PhoneService.Display(item.From) + " : " + ((item.Voice != "") ? "Message vocal. " : ((item.Photo != "") ? "Photo reçue. " : "")) + item.Text, null, v.Owner);
			}
		}
		if (v.Notice != "")
		{
			app?.LockStatus(v.Notice);
			Notice(v.Notice);
		}
		if (flag || v.Notice != "")
		{
			app?.Refresh();
		}
	}

	private void PublishReaders(ulong owner)
	{
		ulong[] array = access.Readers(owner);
		foreach (ulong actor in array)
		{
			PublishAccess(actor);
		}
	}

	private void SelectSurface()
	{
		if (loading || View.Scene != scene)
		{
			if (surface != null)
			{
				Unbind();
			}
			return;
		}
		if (Time.realtimeSinceStartup >= nextSelection)
		{
			nextSelection = Time.realtimeSinceStartup + 0.2f;
			PhoneDeviceInfo phoneDeviceInfo = (IsCasting ? View.Devices.FirstOrDefault((PhoneDeviceInfo d) => d.Device == Live.Active.Device) : View.Devices.FirstOrDefault(delegate(PhoneDeviceInfo d)
			{
				GameObject body = PhoneDevices.Resolve(d.Device);
				return HandHolds(Player.LeftHand, body) || HandHolds(Player.RightHand, body);
			}));
			if (phoneDeviceInfo == null && surface != null && bound.HasValue)
			{
				phoneDeviceInfo = View.Devices.FirstOrDefault((PhoneDeviceInfo d) => d.Device == bound.Value);
			}
			string text = JsonSerializer.Serialize(View.Configuration.Screen);
			if (surface != null && (phoneDeviceInfo == null || bound != phoneDeviceInfo.Device || (Object)(object)surface.Body == (Object)null || screenKey != text))
			{
				Unbind();
			}
			if (surface == null && phoneDeviceInfo != null)
			{
				GameObject val = PhoneDevices.Resolve(phoneDeviceInfo.Device);
				if ((Object)(object)val != (Object)null)
				{
					ScreenOwner = phoneDeviceInfo.Owner;
					bound = phoneDeviceInfo.Device;
					screenKey = text;
					Access = new PhoneAccessView();
					app.ForgetPrivate();
					surface = new PhoneSurface(val, View.Configuration.Screen, Warning);
					app.Bind(surface);
				}
			}
		}
		bool flag = surface != null && surface.IsHeld;
		if (!flag && !IsCasting && Access.Token != "")
		{
			Request("lock", "", "", "", "", enabled: false, 0L);
			ClearAccessLocal();
		}
		if (IsCasting && Time.realtimeSinceStartup >= nextLivePulse)
		{
			nextLivePulse = Time.realtimeSinceStartup + 2f;
			Request("livePulse", "", "", "", Live.Active.Id, enabled: false, 0L);
		}
		if ((flag || IsCasting) && Access.Token != "" && Time.realtimeSinceStartup - accessAt >= 8f)
		{
			ClearAccessLocal();
		}
		if ((flag || IsCasting) && Time.realtimeSinceStartup >= nextAccessPulse)
		{
			nextAccessPulse = Time.realtimeSinceStartup + 2f;
			Request((Access.Token != "") ? "phonePulse" : "phoneInspect", "", "", "", "", enabled: false, 0L);
		}
	}

	public void LockPhone()
	{
		Request("lock", "", "", "", "", enabled: false, 0L);
		ClearAccessLocal();
	}

	private void ClearAccessLocal()
	{
		accessEpoch++;
		Access = new PhoneAccessView();
		app?.ForgetPrivate();
		Media?.ClearPrivate();
		PhoneShutter.RestoreMenu();
	}

	private void TickExperience(bool host)
	{
		if (host)
		{
			ulong[] array = access.Expire(Time.realtimeSinceStartup, (ulong actor2, PhoneAccess.Lease l) => LeaseHeld(actor2, l.Owner, l.Device));
			ulong[] array2 = array;
			foreach (ulong actor in array2)
			{
				if (lives.Casting(actor) == "")
				{
					lives.Disconnect(actor);
				}
				CloseBorrowedCall(actor);
				PublishAccess(actor);
			}
			if (array.Length != 0)
			{
				PublishLives();
			}
			if (lives.Tick(Time.realtimeSinceStartup, LiveSourceAvailable))
			{
				PublishLives();
			}
			devices.Tick(DeviceHeld);
			string text = string.Join("/", from d in devices.Catalog(service.PinSet)
				select d.Owner + ":" + d.Device + ":" + d.PinSet + ":" + d.Placement + ":" + d.Parked);
			if (text != catalogKey)
			{
				catalogKey = text;
				foreach (ulong item in online)
				{
					Publish(item);
				}
			}
		}
		if (!loading && View.Scene == scene)
		{
			PhoneParking phoneParking = parking;
			IEnumerable<PhoneDeviceInfo> catalog;
			if (!host)
			{
				IEnumerable<PhoneDeviceInfo> enumerable = View.Devices;
				catalog = enumerable;
			}
			else
			{
				IEnumerable<PhoneDeviceInfo> enumerable = devices.Catalog(service.PinSet);
				catalog = enumerable;
			}
			phoneParking.Tick(catalog);
		}
		else
		{
			parking.Clear();
		}
		SelectSurface();
		PhoneAccessView result;
		while (accessViews.TryDequeue(out result))
		{
			AcceptAccess(result);
		}
		WindyLiveView result2;
		while (liveViews.TryDequeue(out result2))
		{
			AcceptLive(result2);
		}
		int num2 = 64;
		(ulong, string, uint, byte[], bool) result3;
		while (num2-- > 0 && liveAudio.TryDequeue(out result3))
		{
			if (result3.Item5 && host)
			{
				ForwardLiveVoice(result3.Item1, result3.Item2, result3.Item3, result3.Item4);
			}
			else if (!result3.Item5)
			{
				voice.ReceiveLive(this, result3.Item1, result3.Item2, result3.Item3, result3.Item4);
			}
		}
	}

	private ulong ControllerFor(ulong owner)
	{
		ulong[] array = access.Readers(owner);
		foreach (ulong num in array)
		{
			if (LeaseValid(num))
			{
				return num;
			}
		}
		return owner;
	}

	private void CloseBorrowedCall(ulong actor)
	{
		if (CallIdentity(actor) != actor)
		{
			ulong[] array = calls.Disconnect(actor);
			foreach (ulong id in array)
			{
				PublishCall(id, "Téléphone reverrouillé : appel terminé.");
			}
			callIdentities.Remove(actor);
		}
	}

	private ulong CallIdentity(ulong actor)
	{
		ulong value;
		return callIdentities.TryGetValue(actor, out value) ? value : actor;
	}

	private void HandleLive(ulong actor, ulong owner, PhoneCommand c)
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		string handle = windy.View(owner, 0).Handle;
		switch (c.Operation)
		{
		case "liveStart":
		{
			if (calls.View(actor, (ulong _) => "").State != "Idle")
			{
				throw new InvalidOperationException("Terminer l'appel avant de passer en live.");
			}
			if (!voiceReady.TryGetValue(actor, out var value) || !value)
			{
				throw new InvalidOperationException("Activer la voix Fusion pour passer en live.");
			}
			lives.Start(actor, owner, c.Device.Value, handle, c.Enabled, realtimeSinceStartup);
			NotifyWindy("windyLive", actor, owner, handle, "", lives.Casting(actor));
			break;
		}
		case "liveJoin":
			if (calls.View(actor, (ulong _) => "").State != "Idle")
			{
				throw new InvalidOperationException("Terminer l'appel avant de rejoindre un live.");
			}
			lives.Join(actor, c.Call, realtimeSinceStartup);
			break;
		case "liveLeave":
			lives.Leave(actor);
			break;
		case "liveStop":
			lives.Stop(actor, c.Call);
			break;
		case "liveCamera":
			lives.Camera(actor, c.Call, c.Enabled);
			break;
		case "liveLike":
			lives.Like(actor, c.Call);
			break;
		case "liveComment":
			lives.Comment(actor, c.Call, handle, c.Text, realtimeSinceStartup);
			break;
		default:
			throw new InvalidOperationException("Action live inconnue.");
		}
		PublishLives();
	}

	private void PublishLive(ulong actor)
	{
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID != null && playerID.IsValid)
		{
			WindyLiveView windyLiveView = lives.View(actor);
			windyLiveView.Session = session;
			windyLiveView.Revision = ++liveRevision;
			if (playerID.IsMe)
			{
				AcceptLive(windyLiveView);
			}
			else
			{
				PhoneLiveTransport.State(windyLiveView, playerID.SmallID);
			}
		}
	}

	private void PublishLives()
	{
		foreach (ulong item in online.Where((ulong a) => listenReady.ContainsKey(a)))
		{
			PublishLive(item);
		}
	}

	private void AcceptLive(WindyLiveView v)
	{
		if (v.Protocol == 16 && v.Player == PlayerIDManager.LocalPlatformID && !(v.Session != View.Session) && v.Revision > Live.Revision && v.Available != null && v.Available.Count <= 4 && v.Comments != null && v.Comments.Count <= 12 && !v.Available.Any((WindyLiveSummary s) => s == null || !Guid.TryParseExact(s.Id, "N", out var _) || s.Handle == null || s.Handle.Length > 20) && !v.Comments.Any((WindyLiveComment c) => c == null || c.Text == null || c.Text.Length > 140))
		{
			bool flag = JsonSerializer.Serialize(v.Active) != JsonSerializer.Serialize(Live.Active) || v.Casting != Live.Casting || v.Liked != Live.Liked || JsonSerializer.Serialize(v.Comments) != JsonSerializer.Serialize(Live.Comments) || JsonSerializer.Serialize(v.Available) != JsonSerializer.Serialize(Live.Available);
			Live = v;
			liveAt = Time.realtimeSinceStartup;
			if (flag)
			{
				app?.Refresh();
			}
		}
	}

	public ulong[] LiveTargets(ulong actor, string id)
	{
		return CastingValid(actor, id) ? lives.Targets(actor, id, Time.realtimeSinceStartup) : Array.Empty<ulong>();
	}

	public bool LiveRouteValid(string id, ulong actor)
	{
		return LeaseValid(actor) && lives.Routes(id, actor, Time.realtimeSinceStartup);
	}

	public void SendLiveVoice(string id, uint sequence, byte[] bytes)
	{
		if (NetworkInfo.IsHost)
		{
			EnqueueLiveVoice(PlayerIDManager.LocalPlatformID, id, sequence, bytes, host: true);
		}
		else
		{
			PhoneLiveTransport.Send(PhoneVoicePacket.Create(id, 0uL, sequence, bytes));
		}
	}

	private void ForwardLiveVoice(ulong actor, string id, uint seq, byte[] bytes)
	{
		if (!PhoneVoice.ValidCompressedFrame(bytes) || !voiceReady.TryGetValue(actor, out var value) || !value)
		{
			return;
		}
		if (!liveSequences.TryGetValue(actor, out (string, PhoneAudioWindow) value2) || value2.Item1 != id)
		{
			value2 = (id, new PhoneAudioWindow());
		}
		if (!value2.Item2.Accept(seq))
		{
			return;
		}
		ulong[] array = LiveTargets(actor, id);
		if (array.Length == 0)
		{
			return;
		}
		liveSequences[actor] = value2;
		ulong[] array2 = array;
		foreach (ulong num in array2)
		{
			if (!LeaseValid(num) || !listenReady.TryGetValue(num, out var value3) || !value3)
			{
				continue;
			}
			PlayerID playerID = PlayerIDManager.GetPlayerID(num);
			if (playerID != null && playerID.IsValid)
			{
				if (playerID.IsMe)
				{
					voice.ReceiveLive(this, actor, id, seq, bytes);
				}
				else
				{
					PhoneLiveTransport.Voice(PhoneVoicePacket.Create(id, actor, seq, bytes), playerID.SmallID);
				}
			}
		}
	}

	private void ResetExperience()
	{
		heldChecks.Clear();
		access.Clear();
		bankAttempts.Clear();
		lives.Clear();
		inspected.Clear();
		inspectedEpoch.Clear();
		accessEpoch = 0;
		lastAccessRevision = 0L;
		callIdentities.Clear();
		liveSequences.Clear();
		accessViews.Clear();
		liveViews.Clear();
		liveAudio.Clear();
		Access = new PhoneAccessView();
		Live = new WindyLiveView();
		ScreenOwner = 0uL;
		accessAt = (liveAt = (nextAccessPulse = (nextSelection = (nextLivePulse = 0f))));
		accessRevision = (liveRevision = 0L);
		catalogKey = "";
	}

	public void EnqueueWindy(WindyView v)
	{
		if (windyViews.Count < 32)
		{
			windyViews.Enqueue(v);
		}
	}

	public void EnqueueNotice(PhoneNotice n)
	{
		if (notices.Count < 32)
		{
			notices.Enqueue(n);
		}
	}

	public void EnqueueSpeaker(PhoneSpeakerPacket p)
	{
		if (speakers.Count < 64)
		{
			speakers.Enqueue(p);
		}
	}

	public override void OnInitializeMelon()
	{
		calls.Clock = () => Time.realtimeSinceStartup;
		calls.Finished += SaveCallHistory;
		Instance = this;
		devices.Parked = delegate(PhoneDeviceInfo info, GameObject body)
		{
			parking.Hold(info, body);
		};
		app = new PhoneApp(this);
		directory = Path.Combine(MelonEnvironment.UserDataDirectory, "BonelabRP", "Telephone");
		Sounds = new PhoneSounds(Path.Combine(directory, "preferences.json"));
		voice = new PhoneVoice(Warning);
		Media = new PhoneMedia(this, directory);
		Notes = new PhoneVoiceNotes(this, directory);
		PhoneTransport.Register();
		PhoneVoiceTransport.Register();
		PhoneNearbyTransport.Register();
		PhoneMediaTransport.Register();
		PhoneWindyTransport.Register();
		PhoneLiveTransport.Register();
		((MelonBase)this).HarmonyInstance.PatchAll(typeof(PhoneMod).Assembly);
		BuildMenu();
		((MelonBase)this).LoggerInstance.Msg("Téléphone 0.6 : Galerie privée, code PIN, notifications intégrées et lives Windy. Même version requise chez tous les participants.");
	}

	public void Enqueue(ulong player, PhoneCommand command)
	{
		if (commands.Count < 128)
		{
			commands.Enqueue((player, command));
		}
	}

	public void Enqueue(PhoneView view)
	{
		if (snapshots.Count < 16)
		{
			snapshots.Enqueue(view);
		}
	}

	public void EnqueueCall(PhoneCallUpdate update)
	{
		if (callUpdates.Count < 64)
		{
			callUpdates.Enqueue(update);
		}
	}

	public void EnqueueVoice(ulong actor, string call, uint sequence, byte[] audio, bool host)
	{
		if (voiceFrames.Count < 64)
		{
			voiceFrames.Enqueue((actor, call, sequence, audio, host));
		}
	}

	private bool DeviceHeld(ulong owner, ushort id)
	{
		return PlayerIDManager.PlayerIDs.Any((PlayerID p) => p != null && p.IsValid && ServerHolds(p.PlatformID, owner, id));
	}

	public static bool VoiceHead(PlayerID id, out Vector3 head)
	{
		return Head(id, out head);
	}

	public bool InterceptVoice(byte[] audio)
	{
		return voice?.Intercept(audio, this) ?? true;
	}

	public void ToggleCallMicrophone()
	{
		CallMicMuted = !CallMicMuted;
		app.Refresh();
	}

	public void SendVoice(string call, uint sequence, byte[] audio)
	{
		if (NetworkInfo.IsHost)
		{
			EnqueueVoice(PlayerIDManager.LocalPlatformID, call, sequence, audio, host: true);
		}
		else
		{
			PhoneVoiceTransport.Send(PhoneVoicePacket.Create(call, 0uL, sequence, audio));
		}
	}

	public void Request(string operation, string number = "", string name = "", string text = "", string call = "", bool enabled = false, long amount = 0L, string photo = "", int offset = 0, string voice = "")
	{
		try
		{
			if (!NetworkInfo.HasServer || PlayerIDManager.LocalID == null)
			{
				throw new InvalidOperationException("Rejoindre un lobby Fusion.");
			}
			if (operation != "sync" && View.Session == "")
			{
				throw new InvalidOperationException("Attendre l'attribution du numéro.");
			}
			PhoneCommand command = new PhoneCommand
			{
				Operation = operation,
				Session = View.Session,
				Nonce = Guid.NewGuid().ToString("N"),
				Number = number,
				Name = name,
				Text = text,
				Call = call,
				VoiceReady = PhoneVoice.Ready,
				ListenReady = PhoneVoice.CanListen,
				Enabled = enabled,
				Amount = amount,
				Photo = photo,
				Voice = voice,
				Offset = offset,
				Owner = ScreenOwner,
				Device = bound,
				Access = Access.Token,
				Epoch = accessEpoch
			};
			if (NetworkInfo.IsHost)
			{
				Enqueue(PlayerIDManager.LocalPlatformID, command);
			}
			else
			{
				PhoneTransport.Send(command);
			}
		}
		catch (Exception ex)
		{
			Notice(ex.Message);
		}
	}

	public void LocalStatus(string text)
	{
		Notice(text);
	}

	private void Notice(string text)
	{
		Status = text;
		app.Refresh();
		if (menuStatus != null)
		{
			((Element)menuStatus).ElementName = text;
		}
	}

	public override void OnUpdate()
	{
		heldChecks.Clear();
		try
		{
			Notes?.Tick();
			if (!NetworkInfo.HasServer || PlayerIDManager.LocalID == null)
			{
				if (connected)
				{
					Reset();
				}
				return;
			}
			bool isHost = NetworkInfo.IsHost;
			ShowAdmin(isHost);
			if (!connected || isHost != wasHost)
			{
				Reset();
				connected = true;
				wasHost = isHost;
				if (isHost)
				{
					try
					{
						service = new PhoneService(Path.Combine(directory, "accounts.json"));
						windy = new PhoneWindy(Path.Combine(directory, "windy.json"));
						string path = Path.Combine(directory, "configuration.json");
						configuration = (File.Exists(path) ? (JsonSerializer.Deserialize<PhoneConfiguration>(File.ReadAllText(path)) ?? throw new InvalidOperationException("Configuration vide.")) : new PhoneConfiguration());
						configuration.Validate();
						session = Guid.NewGuid().ToString("N");
						Media.BeginHost();
						SaveConfig();
					}
					catch
					{
						service = null;
						throw;
					}
				}
				nextTick = (nextSync = 0f);
			}
			if (isHost && service == null)
			{
				return;
			}
			PhoneView result;
			while (snapshots.TryDequeue(out result))
			{
				Accept(result);
			}
			PhoneCallUpdate result2;
			while (callUpdates.TryDequeue(out result2))
			{
				AcceptCall(result2);
			}
			WindyView result3;
			while (windyViews.TryDequeue(out result3))
			{
				AcceptWindy(result3);
			}
			bool flag = FusionSceneManager.IsLoading();
			string text = FusionSceneManager.Barcode ?? "";
			if (scene != text || loading != flag)
			{
				loading = flag;
				scene = text;
				if (isHost)
				{
					ulong[] array = calls.StopAll();
					foreach (ulong id in array)
					{
						PublishCall(id, "Appel terminé : changement de map.");
					}
				}
				StopLocalCall("Appel terminé : changement de map.");
				access.Clear();
				lives.Clear();
				PublishLives();
				ClearAccessLocal();
				Media.Reset();
				if (isHost)
				{
					Media.BeginHost();
				}
				devices.Reset();
				Unbind();
				View.Device = null;
				if (isHost)
				{
					ulong[] array2 = online.ToArray();
					foreach (ulong id2 in array2)
					{
						Publish(id2);
					}
				}
				nextSync = 0f;
			}
			if (isHost)
			{
				if (Time.realtimeSinceStartup >= nextTick)
				{
					nextTick = Time.realtimeSinceStartup + 1f;
					PlayerID[] array3 = PlayerIDManager.PlayerIDs.Where((PlayerID p) => p != null && p.IsValid).ToArray();
					HashSet<ulong> active = array3.Select((PlayerID p) => p.PlatformID).ToHashSet();
					ulong[] array4 = online.Where((ulong item) => !active.Contains(item)).ToArray();
					foreach (ulong num2 in array4)
					{
						ulong[] array5 = calls.Disconnect(num2);
						foreach (ulong id3 in array5)
						{
							PublishCall(id3, "Correspondant déconnecté.");
						}
						access.Lock(num2);
						lives.Disconnect(num2);
						devices.Remove(num2);
						online.Remove(num2);
						lastCommands.Remove(num2);
						deviceStatuses.Remove(num2);
						listenReady.Remove(num2);
						lastShares.Remove(num2);
						windyOffsets.Remove(num2);
						lastWindyPosts.Remove(num2);
						voiceReady.Remove(num2);
						callNonces.Remove(num2);
						audioSequences.Remove(num2);
					}
					PlayerID[] array6 = array3;
					foreach (PlayerID val in array6)
					{
						bool flag2 = online.Add(val.PlatformID);
						service.Ensure(val.PlatformID);
						deviceStatuses.TryGetValue(val.PlatformID, out var value);
						if (!loading && configuration.AutoSpawn)
						{
							devices.Ensure(val, configuration);
						}
						string text2 = devices.Status(val.PlatformID);
						if (flag2 || value != text2)
						{
							deviceStatuses[val.PlatformID] = text2;
							Publish(val.PlatformID);
						}
					}
				}
				ulong[] array7 = calls.Tick(Time.realtimeSinceStartup);
				foreach (ulong id4 in array7)
				{
					PublishCall(id4, "Appel terminé : aucune réponse ou liaison interrompue.");
				}
				int num6 = 32;
				(ulong, PhoneCommand) result4;
				while (num6-- > 0 && commands.TryDequeue(out result4))
				{
					Handle(result4.Item1, result4.Item2);
				}
			}
			if (Time.realtimeSinceStartup >= nextSync)
			{
				nextSync = Time.realtimeSinceStartup + 10f;
				Request("sync", "", "", "", "", enabled: false, 0L);
			}
			TickExperience(isHost);
			if (View.Call.State != "Idle")
			{
				if (Time.realtimeSinceStartup - lastCallSync >= 12f)
				{
					StopLocalCall("Liaison avec l'hôte interrompue.");
				}
				else if (Time.realtimeSinceStartup >= nextHeartbeat)
				{
					nextHeartbeat = Time.realtimeSinceStartup + 2f;
					Request("heartbeat", "", "", "", View.Call.Id, enabled: false, 0L);
				}
			}
			int num7 = 64;
			(ulong, string, uint, byte[], bool) result5;
			while (num7-- > 0 && voiceFrames.TryDequeue(out result5))
			{
				if (result5.Item5 && isHost)
				{
					ForwardVoice(result5.Item1, result5.Item2, result5.Item3, result5.Item4);
				}
				else if (!result5.Item5)
				{
					voice.Receive(this, result5.Item1, result5.Item2, result5.Item3, result5.Item4);
				}
			}
			PhoneNotice result6;
			while (notices.TryDequeue(out result6))
			{
				AcceptNotice(result6);
			}
			PhoneSpeakerPacket result7;
			while (speakers.TryDequeue(out result7))
			{
				voice.ReceiveSpeaker(this, result7);
			}
			voice.Tick(this);
			surface?.Tick();
			app.Tick();
			Media.Tick(surface);
		}
		catch (Exception ex)
		{
			if (Time.realtimeSinceStartup >= nextWarning)
			{
				nextWarning = Time.realtimeSinceStartup + 5f;
				Warning(ex.ToString());
				Notice(ex.Message);
			}
		}
	}

	private void Handle(ulong id, PhoneCommand command)
	{
		PlayerID playerID = PlayerIDManager.GetPlayerID(id);
		if (playerID == null || !playerID.IsValid || command.Protocol != 16)
		{
			return;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		voiceReady[id] = command.VoiceReady;
		listenReady[id] = command.ListenReady;
		if (command.Operation == "livePulse")
		{
			if (command.Session == session && CastingValid(id, command.Call))
			{
				lives.Pulse(id, realtimeSinceStartup);
				PublishLive(id);
			}
			else
			{
				PublishLive(id);
			}
			return;
		}
		if (command.Operation == "phonePulse")
		{
			try
			{
				if (command.Session == session)
				{
					HandleAccess(id, command);
				}
				return;
			}
			catch (Exception ex)
			{
				access.Lock(id);
				lives.Disconnect(id);
				PublishAccess(id, ex.Message);
				return;
			}
		}
		if (command.Operation == "heartbeat")
		{
			try
			{
				calls.Heartbeat(id, command.Call, realtimeSinceStartup);
				if (!command.VoiceReady)
				{
					ulong[] array = calls.Disconnect(id);
					foreach (ulong id2 in array)
					{
						PublishCall(id2, "Voix Fusion désactivée : appel terminé.");
					}
				}
				else
				{
					PublishCall(id);
				}
				return;
			}
			catch (Exception)
			{
				PublishCall(id);
				return;
			}
		}
		bool flag = command.Operation == "bankLock" || command.Operation == "sync" || command.Operation == "phoneInspect" || command.Operation == "photoFetch" || command.Operation == "voiceFetch" || command.Operation == "wallpaperFetch" || command.Operation == "windySync" || command.Operation == "galleryAdd" || command.Operation == "galleryDelete";
		if (!flag && lastCommands.TryGetValue(id, out var value) && realtimeSinceStartup - value < 0.25f)
		{
			Publish(id, "Action trop rapide, réessayer.");
			return;
		}
		if (!flag)
		{
			lastCommands[id] = realtimeSinceStartup;
		}
		try
		{
			if (command.Operation == "sync")
			{
				Publish(id);
				PublishWindy(id);
				PublishLive(id);
				if (LeaseValid(id))
				{
					PublishAccess(id);
				}
			}
			else if (command.Session != session)
			{
				Publish(id, "Session actualisée, réessayer.");
			}
			else
			{
				if (HandleAccess(id, command))
				{
					return;
				}
				if (command.Operation == "spawn")
				{
					if (loading)
					{
						throw new InvalidOperationException("Attendre le chargement.");
					}
					devices.Ensure(playerID, configuration, replace: true, DeviceHeld);
					access.Lock(id);
					Publish(id);
					return;
				}
				if (command.Operation == "wallpaperFetch")
				{
					if (!command.Device.HasValue || !ServerHolds(id, command.Owner, command.Device.Value) || !service.IsWallpaper(command.Owner, command.Photo))
					{
						throw new InvalidOperationException("Fond d’écran indisponible.");
					}
					Media.Fetch(id, command.Photo);
					return;
				}
				ulong num = AuthorizedOwner(id, command);
				if (HandleBankAccess(id, num, command))
				{
					return;
				}
				if (command.Operation == "wallpaper")
				{
					if (command.Photo != "" && !Media.Owns(num, command.Photo))
					{
						throw new InvalidOperationException("Photo indisponible.");
					}
					service.SetWallpaper(num, command.Photo);
					PublishReaders(num);
					return;
				}
				if (command.Operation == "voiceFetch")
				{
					Notes.Fetch(id, command.Voice);
					return;
				}
				if (command.Operation.StartsWith("live", StringComparison.Ordinal))
				{
					HandleLive(id, num, command);
					return;
				}
				if (command.Operation == "galleryAdd")
				{
					if (service.PhotoDeleted(num, command.Photo))
					{
						SendNotice(playerID, new PhoneNotice
						{
							Kind = "galleryRemoved",
							Number = command.Photo
						});
						return;
					}
					if (!Media.Owns(num, command.Photo))
					{
						throw new InvalidOperationException("Photo indisponible.");
					}
					service.AddPhoto(num, command.Photo);
					PublishReaders(num);
					return;
				}
				if (command.Operation == "galleryDelete")
				{
					service.DeletePhoto(num, command.Photo);
					ulong[] array2 = access.Readers(num);
					foreach (ulong num2 in array2)
					{
						PlayerID playerID2 = PlayerIDManager.GetPlayerID(num2);
						if (playerID2 != null && playerID2.IsValid)
						{
							SendNotice(playerID2, new PhoneNotice
							{
								Kind = "galleryRemoved",
								Number = command.Photo
							});
						}
					}
					PublishReaders(num);
					return;
				}
				if (command.Operation == "call" || command.Operation == "answer" || command.Operation == "reject" || command.Operation == "hangup")
				{
					HandleCall(id, command);
					return;
				}
				if (command.Operation == "windySync" || command.Operation == "windyJoin" || command.Operation == "windyPost")
				{
					HandleWindy(id, command, num);
					return;
				}
				if (command.Operation == "photoFetch")
				{
					Media.Fetch(id, command.Photo);
					return;
				}
				if (command.Operation == "video")
				{
					ulong[] array3 = calls.SetVideo(id, command.Call, command.Enabled);
					foreach (ulong id3 in array3)
					{
						PublishCall(id3, command.Enabled ? "Caméra de visio activée." : "Caméra de visio coupée.");
					}
					return;
				}
				if (command.Operation == "share")
				{
					ulong result = 0uL;
					if (command.Text != "" && !ulong.TryParse(command.Text, out result))
					{
						throw new InvalidOperationException("Destinataire AirDrop invalide.");
					}
					ShareNumber(playerID, num, result);
					return;
				}
				if (command.Operation == "transfer")
				{
					PhoneAccess.Lease lease = access.Get(id);
					if (lease == null || !lease.BankAuthorized)
					{
						throw new InvalidOperationException("Déverrouiller l’application Banque avant un virement.");
					}
					Transfer(playerID, command, num);
					return;
				}
				if (command.Operation == "speaker")
				{
					if (command.Enabled && !devices.Id(num).HasValue)
					{
						throw new InvalidOperationException("Téléphone absent.");
					}
					ulong[] array4 = calls.SetSpeaker(id, command.Call, command.Enabled);
					foreach (ulong id4 in array4)
					{
						PublishCall(id4, command.Enabled ? "Haut-parleur activé : conversation audible à 2 m." : "Haut-parleur désactivé.");
					}
					return;
				}
				if (command.Operation == "sms" && !string.IsNullOrEmpty(command.Photo) && !Media.Owns(num, command.Photo))
				{
					throw new InvalidOperationException("Photo non disponible pour cet expéditeur.");
				}
				if (command.Operation == "contactPhoto" && !string.IsNullOrEmpty(command.Photo) && !Media.Owns(num, command.Photo))
				{
					throw new InvalidOperationException("Photo de contact indisponible pour ce téléphone.");
				}
				if (command.Operation == "sms" && !string.IsNullOrEmpty(command.Voice))
				{
					command.VoiceSeconds = Notes.Duration(num, command.Voice);
				}
				PhoneChange phoneChange = service.Apply(num, command, DateTime.UtcNow);
				Publish(id, phoneChange.Notice);
				PublishReaders(num);
				if (phoneChange.Recipient.HasValue && phoneChange.Recipient.Value != num)
				{
					Publish(phoneChange.Recipient.Value, "Nouveau SMS.");
					PublishReaders(phoneChange.Recipient.Value);
				}
			}
		}
		catch (Exception ex3)
		{
			Publish(id, ex3.Message);
			PublishAccess(id, ex3.Message);
		}
	}

	private void HandleCall(ulong id, PhoneCommand command)
	{
		if (!Guid.TryParseExact(command.Nonce, "N", out var _))
		{
			throw new InvalidOperationException("Commande appel invalide.");
		}
		if (!callNonces.TryGetValue(id, out var value))
		{
			value = (callNonces[id] = new Queue<string>());
		}
		if (value.Contains(command.Nonce))
		{
			PublishCall(id);
			return;
		}
		double now = Time.realtimeSinceStartup;
		ulong[] array;
		string notice;
		if (command.Operation == "call")
		{
			if (loading || !NetworkSceneManager.IsLevelNetworked)
			{
				throw new InvalidOperationException("Attendre une map chargée dans Fusion.");
			}
			if (!command.VoiceReady)
			{
				throw new InvalidOperationException("Activer la voix Fusion et vérifier le microphone.");
			}
			bool masked;
			string input = PhoneDial.Parse(command.Number, out masked);
			ulong? num = service.FindPlayer(input);
			if (!num.HasValue)
			{
				throw new InvalidOperationException("Ce numéro n'est pas attribué.");
			}
			PlayerID playerID = PlayerIDManager.GetPlayerID(ControllerFor(num.Value));
			if (playerID == null || !playerID.IsValid || !online.Contains(num.Value))
			{
				throw new InvalidOperationException("Ce correspondant est hors ligne.");
			}
			if (!voiceReady.TryGetValue(playerID.PlatformID, out var value2) || !value2)
			{
				throw new InvalidOperationException("Correspondant indisponible : vérifier sa version du mod et sa voix Fusion.");
			}
			if (!NetworkSceneManager.InCurrentLevel(playerID) || ((MetadataVariableT<bool>)(object)playerID.Metadata.Loading).GetValue())
			{
				throw new InvalidOperationException("Le correspondant charge une map.");
			}
			if (lives.Casting(id) != "" || lives.Watching(id) != "" || lives.Casting(playerID.PlatformID) != "" || lives.Watching(playerID.PlatformID) != "")
			{
				throw new InvalidOperationException("Quitter le live avant l’appel.");
			}
			if (calls.Busy(id) || calls.Busy(playerID.PlatformID))
			{
				throw new InvalidOperationException("Ce correspondant est occupé.");
			}
			callIdentities[id] = AuthorizedOwner(id, command);
			callIdentities[playerID.PlatformID] = num.Value;
			array = calls.Start(id, playerID.PlatformID, now, command.Enabled, masked);
			notice = "Appel en cours…";
		}
		else if (command.Operation == "answer")
		{
			if (!command.VoiceReady || loading)
			{
				throw new InvalidOperationException("Activer la voix Fusion avant d'accepter.");
			}
			array = calls.Answer(id, command.Call, now, command.Enabled);
			notice = "Appel connecté.";
		}
		else if (command.Operation == "reject")
		{
			array = calls.Reject(id, command.Call);
			notice = "Appel refusé.";
		}
		else
		{
			array = calls.Hangup(id, command.Call);
			notice = "Appel terminé.";
		}
		value.Enqueue(command.Nonce);
		while (value.Count > 128)
		{
			value.Dequeue();
		}
		ulong[] array2 = array;
		foreach (ulong id2 in array2)
		{
			PublishCall(id2, notice);
		}
	}

	private void SaveCallHistory(PhoneCallResult result)
	{
		if (service == null)
		{
			return;
		}
		try
		{
			ulong num = CallIdentity(result.Caller);
			ulong num2 = CallIdentity(result.Callee);
			service.RecordCall(num, num2, result);
			PublishReaders(num);
			PublishReaders(num2);
		}
		catch (Exception ex)
		{
			Warning("Historique appel : " + ex.Message);
		}
	}

	private void PublishCall(ulong id, string notice = "")
	{
		PlayerID playerID = PlayerIDManager.GetPlayerID(id);
		if (playerID != null && playerID.IsValid && service != null)
		{
			PhoneCallUpdate update = new PhoneCallUpdate
			{
				Session = session,
				Player = id,
				Revision = ++callRevision,
				Call = calls.View(id, (ulong peer) => service.Ensure(CallIdentity(peer)).Number),
				Notice = notice
			};
			if (playerID.IsMe)
			{
				AcceptCall(update);
			}
			else
			{
				PhoneVoiceTransport.State(update, playerID.SmallID);
			}
		}
	}

	private void AcceptCall(PhoneCallUpdate update)
	{
		if (update.Protocol != 16 || update.Player != PlayerIDManager.LocalPlatformID || update.Session != View.Session || update.Revision <= View.CallRevision || update.Call == null)
		{
			return;
		}
		PhoneCallView call = update.Call;
		if ((call.State != "Idle" && call.State != "Ringing" && call.State != "Active") || (call.State != "Idle" && (!Guid.TryParseExact(call.Id, "N", out var _) || retiredCalls.Contains(call.Id))))
		{
			return;
		}
		if (call.Id != View.Call.Id)
		{
			if (View.Call.Id != "")
			{
				retiredCalls.Add(View.Call.Id);
			}
			CallMicMuted = false;
		}
		bool flag = View.Call.Id != call.Id || View.Call.State != call.State || View.Call.Peer != call.Peer || View.Call.Masked != call.Masked || View.Call.Number != call.Number || View.Call.Incoming != call.Incoming || View.Call.Speaker != call.Speaker || View.Call.PeerSpeaker != call.PeerSpeaker || View.Call.Video != call.Video || View.Call.PeerVideo != call.PeerVideo || View.Call.VideoRequested != call.VideoRequested;
		View.Call = call;
		View.CallRevision = update.Revision;
		lastCallSync = Time.realtimeSinceStartup;
		if (update.Notice != "")
		{
			Notice(update.Notice);
		}
		if (flag)
		{
			app.Refresh();
		}
	}

	private void StopLocalCall(string reason)
	{
		if (View.Call.Id != "")
		{
			retiredCalls.Add(View.Call.Id);
			View.Call = new PhoneCallView();
			Notice(reason);
		}
		voice?.Dispose();
		CallMicMuted = false;
	}

	private void ForwardVoice(ulong actor, string call, uint sequence, byte[] audio)
	{
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		if (!calls.TryRoute(actor, call, Time.realtimeSinceStartup, out var recipient) || !voiceReady.TryGetValue(actor, out var value) || !value)
		{
			return;
		}
		if (!audioSequences.TryGetValue(actor, out (string, PhoneAudioWindow) value2) || value2.Item1 != call)
		{
			value2 = (call, new PhoneAudioWindow());
		}
		if (!value2.Item2.Accept(sequence) || !PhoneVoice.ValidCompressedFrame(audio))
		{
			return;
		}
		audioSequences[actor] = value2;
		PlayerID playerID = PlayerIDManager.GetPlayerID(recipient);
		if (playerID == null || !playerID.IsValid)
		{
			return;
		}
		if (playerID.IsMe)
		{
			voice.Receive(this, actor, call, sequence, audio);
		}
		else
		{
			PhoneVoiceTransport.Reply(PhoneVoicePacket.Create(call, actor, sequence, audio), playerID.SmallID);
		}
		(ulong, string)[] array = calls.Speakers(actor, call);
		for (int i = 0; i < array.Length; i++)
		{
			(ulong, string) tuple = array[i];
			PhoneAccess.Lease lease = access.Get(tuple.Item1);
			ushort? num = ((lease != null && LeaseValid(tuple.Item1)) ? new ushort?(lease.Device) : devices.Id(CallIdentity(tuple.Item1)));
			GameObject val = (num.HasValue ? PhoneDevices.Resolve(num.Value) : null);
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			foreach (PlayerID playerID2 in PlayerIDManager.PlayerIDs)
			{
				if (playerID2 != null && playerID2.IsValid && playerID2.PlatformID != actor && playerID2.PlatformID != recipient && listenReady.TryGetValue(playerID2.PlatformID, out var value3) && value3 && Head(playerID2, out var head) && Within(head, val.transform.position, 2f))
				{
					PhoneSpeakerPacket p = PhoneSpeakerPacket.Create(tuple.Item2, num.Value, PhoneVoicePacket.Create(call, actor, sequence, audio));
					if (playerID2.IsMe)
					{
						EnqueueSpeaker(p);
					}
					else
					{
						PhoneNearbyTransport.Speaker(p, playerID2.SmallID);
					}
				}
			}
		}
	}

	private void Publish(ulong id, string notice = "")
	{
		PlayerID playerID = PlayerIDManager.GetPlayerID(id);
		if (playerID != null && playerID.IsValid && service != null)
		{
			PhoneView phoneView = service.PrivateView(id);
			phoneView.BankReady = ServerEconomy.IsHosting;
			phoneView.Balance = (phoneView.BankReady ? ServerEconomy.GetBalance(id) : 0);
			phoneView.Call = calls.View(id, (ulong peer) => service.Ensure(CallIdentity(peer)).Number);
			phoneView.CallRevision = ++callRevision;
			phoneView.Scene = scene;
			phoneView.Session = session;
			phoneView.Revision = ++revision;
			phoneView.Device = (loading ? ((ushort?)null) : devices.Id(id));
			phoneView.Devices = devices.Catalog(service.PinSet).ToList();
			phoneView.DeviceStatus = devices.Status(id);
			phoneView.Configuration = configuration;
			phoneView.Notice = notice;
			if (playerID.IsMe)
			{
				Accept(phoneView);
			}
			else
			{
				PhoneTransport.Reply(phoneView, playerID.SmallID);
			}
		}
	}

	private void Accept(PhoneView view)
	{
		if (view.Protocol != 16 || view.Player != PlayerIDManager.LocalPlatformID || string.IsNullOrEmpty(view.Session) || retiredSessions.Contains(view.Session) || (view.Session == View.Session && view.Revision <= View.Revision))
		{
			return;
		}
		if (view.Session != View.Session && View.Session != "")
		{
			retiredSessions.Add(View.Session);
			Windy = new WindyView();
			publicPhotos.Clear();
			Unbind();
			StopLocalCall("Session actualisée.");
			View.CallRevision = 0L;
			retiredCalls.Clear();
		}
		PhoneSms[] array = ((View.Session == view.Session && View.Number != "") ? view.Messages.Where((PhoneSms m) => m.To == view.Number && !View.Messages.Any((PhoneSms old) => old.Id == m.Id)).ToArray() : Array.Empty<PhoneSms>());
		view.Configuration.Validate();
		PhoneCallUpdate update = new PhoneCallUpdate
		{
			Session = view.Session,
			Player = view.Player,
			Revision = view.CallRevision,
			Call = view.Call
		};
		PhoneCallView call = View.Call;
		long num = View.CallRevision;
		view.Call = call;
		view.CallRevision = num;
		View = view;
		AcceptCall(update);
		Status = ((view.Notice != "") ? view.Notice : view.DeviceStatus);
		if (menuNumber != null)
		{
			((Element)menuNumber).ElementName = "Mon numéro : " + PhoneService.Display(view.Number);
		}
		if (menuStatus != null)
		{
			((Element)menuStatus).ElementName = Status;
		}
		if (!ScreenUnlocked || ScreenOwner != PlayerIDManager.LocalPlatformID)
		{
			PhoneSms[] array2 = array;
			foreach (PhoneSms phoneSms in array2)
			{
				LocalPopup("Messages", PhoneService.Display(phoneSms.From) + " : " + ((phoneSms.Photo != "") ? "Photo reçue. " : "") + phoneSms.Text, null, 0uL);
			}
		}
		app.Refresh();
	}

	public bool OwnsPhotoController(BaseController controller)
	{
		return (Object)(object)controller != (Object)null && Media != null && Media.PhotoPage && surface != null && surface.PlacementReady && ((surface.HoldsLeft && (Object)(object)controller == (Object)(object)Player.LeftController) || (surface.HoldsRight && (Object)(object)controller == (Object)(object)Player.RightController));
	}

	public void ShutterPhoto()
	{
		if (Media != null && Media.PhotoPage)
		{
			Media.CapturePhoto(surface);
		}
	}

	private void HandleWindy(ulong id, PhoneCommand c, ulong owner)
	{
		if (windy == null)
		{
			throw new InvalidOperationException("Windy indisponible.");
		}
		if (c.Operation == "windySync")
		{
			if (c.Offset < 0 || c.Offset >= 300)
			{
				throw new InvalidOperationException("Page Windy invalide.");
			}
			windyOffsets[id] = c.Offset;
			PublishWindy(id);
			return;
		}
		if (c.Operation == "windyJoin")
		{
			windy.Register(owner, c.Name, c.Nonce);
			PublishWindy(id, "Compte Windy prêt.");
			return;
		}
		if (!string.IsNullOrEmpty(c.Photo) && !Media.Owns(owner, c.Photo))
		{
			throw new InvalidOperationException("Photo non disponible pour cet auteur.");
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (lastWindyPosts.TryGetValue(id, out var value) && realtimeSinceStartup - value < 3f)
		{
			throw new InvalidOperationException("Attendre trois secondes entre deux publications.");
		}
		if (windy.Post(owner, c.Text, c.Photo, c.Nonce, DateTime.UtcNow))
		{
			lastWindyPosts[id] = realtimeSinceStartup;
			WindyPostView windyPostView = windy.View(owner, 0).Posts.First();
			NotifyWindy("windyPost", id, owner, windyPostView.Handle, windyPostView.Text, windyPostView.Id, windyPostView.Photo != "");
		}
		windyOffsets[id] = 0;
		ulong[] array = online.Where((ulong peer) => listenReady.ContainsKey(peer)).ToArray();
		foreach (ulong num2 in array)
		{
			PublishWindy(num2, (num2 == id) ? "Publication Windy envoyée." : "");
		}
	}

	private void PublishWindy(ulong id, string notice = "")
	{
		if (windy == null)
		{
			return;
		}
		PlayerID playerID = PlayerIDManager.GetPlayerID(id);
		if (playerID != null && playerID.IsValid)
		{
			windyOffsets.TryGetValue(id, out var value);
			WindyView windyView = windy.View(LeaseValid(id) ? access.Get(id).Owner : id, value);
			windyView.Player = id;
			windyView.Session = session;
			windyView.Revision = ++windyRevision;
			windyView.Notice = notice;
			if (playerID.IsMe)
			{
				AcceptWindy(windyView);
			}
			else
			{
				PhoneWindyTransport.Reply(windyView, playerID.SmallID);
			}
		}
	}

	private void AcceptWindy(WindyView v)
	{
		if (v.Protocol != 16 || v.Player != PlayerIDManager.LocalPlatformID || v.Session != View.Session || (ScreenUnlocked && v.Owner != ScreenOwner) || v.Revision <= Windy.Revision || v.Posts == null || v.Posts.Count > 1 || v.Total < 0 || v.Total > 300)
		{
			return;
		}
		if (publicPhotos.Count > 1000)
		{
			publicPhotos.Clear();
		}
		foreach (WindyPostView post in v.Posts)
		{
			if (post == null || !Guid.TryParseExact(post.Id, "N", out var result) || post.Text == null || post.Text.Length > 280 || post.Photo == null || (post.Photo != "" && !Guid.TryParseExact(post.Photo, "N", out result)))
			{
				return;
			}
			if (post.Photo != "")
			{
				publicPhotos.Add(post.Photo);
			}
		}
		Windy = v;
		if (v.Notice == "Publication Windy envoyée.")
		{
			app.WindyPosted();
		}
		if (v.Notice != "")
		{
			Notice(v.Notice);
		}
		app.Refresh();
	}

	public void VoiceSent()
	{
		app.VoiceSent();
	}

	public void RefreshApp()
	{
		app?.Refresh();
	}

	public void MediaWarning(string text)
	{
		Warning(text);
	}

	public void MediaStatus(ulong actor, string text)
	{
		Publish(actor, text);
	}

	public bool PhotoReferenced(string id)
	{
		return (service != null && service.PhotoReferenced(id)) || (windy != null && windy.PhotoReferenced(id));
	}

	public void MediaUploadFailed(ulong actor, string id, string text)
	{
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID != null && playerID.IsValid)
		{
			SendNotice(playerID, new PhoneNotice
			{
				Kind = "uploadFailed",
				Number = id,
				Text = text
			});
		}
	}

	public bool CanReadPhoto(ulong actor, string id)
	{
		PhoneDeviceInfo value;
		return (service != null && inspected.TryGetValue(actor, out value) && service.IsWallpaper(value.Owner, id) && ServerHolds(actor, value.Owner, value.Device)) || (LeaseValid(actor) && ((service != null && service.CanReadPhoto(access.Get(actor).Owner, id)) || (windy != null && windy.PhotoReferenced(id))));
	}

	public bool CanReadVoice(ulong actor, string id)
	{
		return LeaseValid(actor) && service != null && service.CanReadVoice(access.Get(actor).Owner, id);
	}

	public bool IsVoiceKnown(string id)
	{
		return ScreenUnlocked && ScreenProfile.Messages.Any((PhoneSms m) => m.Voice == id);
	}

	public bool IsPhotoKnown(string id)
	{
		return (!string.IsNullOrEmpty(id) && id == VisibleWallpaper) || (ScreenUnlocked && (Gallery.Contains(id) || ScreenProfile.Contacts.Any((PhoneContact c) => c.Photo == id) || ScreenProfile.Messages.Any((PhoneSms m) => m.Photo == id) || publicPhotos.Contains(id)));
	}

	public bool MediaVideoRoute(ulong actor, string token, out ulong peer)
	{
		peer = 0uL;
		return calls.VideoEnabled(actor, token) && calls.TryRoute(actor, token, Time.realtimeSinceStartup, out peer);
	}

	public bool MediaRouteValid(string token, ulong target)
	{
		ulong recipient;
		return calls.TryRoute(target, token, Time.realtimeSinceStartup, out recipient) && calls.VideoEnabled(recipient, token);
	}

	public void MediaUploadReady(PlayerID player, string id)
	{
		SendNotice(player, new PhoneNotice
		{
			Kind = "uploadReady",
			Number = id
		});
	}

	public static bool Within(Vector3 a, Vector3 b, float radius)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		return PhoneProximity.Within(a.x, a.y, a.z, b.x, b.y, b.z, radius);
	}

	private static bool Head(PlayerID id, out Vector3 head)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		head = default(Vector3);
		if (!NetworkSceneManager.InCurrentLevel(id) || ((MetadataVariableT<bool>)(object)id.Metadata.Loading).GetValue())
		{
			return false;
		}
		NetworkPlayer val = default(NetworkPlayer);
		if (!NetworkPlayerManager.TryGetPlayer(id.SmallID, ref val) || !val.HasRig || (Object)(object)val.RigRefs.Head == (Object)null)
		{
			return false;
		}
		head = val.RigRefs.Head.position;
		return true;
	}

	public (ulong Id, string Name, float Distance)[] NearbyPeople()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)Player.Head == (Object)null)
		{
			return Array.Empty<(ulong, string, float)>();
		}
		Vector3 pos = Player.Head.position;
		Vector3 head;
		return (from p in PlayerIDManager.PlayerIDs.Where((PlayerID p) => p != null && p.IsValid && !p.IsMe && View.Devices.Any((PhoneDeviceInfo d) => d.Owner == p.PlatformID) && Head(p, out head) && Within(pos, head, 3f)).Select(delegate(PlayerID p)
			{
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0021: Unknown result type (might be due to invalid IL or missing references)
				//IL_0026: Unknown result type (might be due to invalid IL or missing references)
				//IL_002b: Unknown result type (might be due to invalid IL or missing references)
				Head(p, out head);
				ulong platformID = p.PlatformID;
				string item = ServerEconomy.ResolveName(p) ?? "Joueur";
				Vector3 val = head - pos;
				return (PlatformID: platformID, item, magnitude: ((Vector3)(ref val)).magnitude);
			})
			orderby p.magnitude
			select p).ToArray();
	}

	private void ShareNumber(PlayerID actor, ulong owner, ulong selected = 0uL)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (lastShares.TryGetValue(actor.PlatformID, out var value) && realtimeSinceStartup - value < 5f)
		{
			throw new InvalidOperationException("Attendre 5 secondes entre deux partages.");
		}
		if (!Head(actor, out var head))
		{
			throw new InvalidOperationException("Attendre une map chargée.");
		}
		lastShares[actor.PlatformID] = realtimeSinceStartup;
		int num = 0;
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (playerID != null && playerID.IsValid && (selected == 0L || playerID.PlatformID == selected) && playerID.PlatformID != actor.PlatformID && listenReady.ContainsKey(playerID.PlatformID) && Head(playerID, out var head2) && Within(head, head2, 3f))
			{
				string text = ServerEconomy.ResolveName(PlayerIDManager.GetPlayerID(owner) ?? actor) ?? "Joueur";
				if (text.Length > 32)
				{
					text = text.Substring(0, 32);
				}
				SendNotice(playerID, new PhoneNotice
				{
					Owner = (LeaseValid(playerID.PlatformID) ? access.Get(playerID.PlatformID).Owner : playerID.PlatformID),
					Kind = "share",
					Name = text,
					Number = service.Ensure(owner).Number,
					Text = text + " partage son numéro."
				});
				num++;
			}
		}
		Publish(actor.PlatformID, (num == 0) ? "Aucun joueur compatible à moins de 3 m." : ("Numéro partagé avec " + num + " joueur(s)."));
	}

	private void SendNotice(PlayerID peer, PhoneNotice n)
	{
		n.Player = peer.PlatformID;
		if (n.Owner == 0)
		{
			n.Owner = peer.PlatformID;
		}
		n.Session = session;
		if (peer.IsMe)
		{
			EnqueueNotice(n);
		}
		else
		{
			PhoneNearbyTransport.Notice(n, peer.SmallID);
		}
	}

	private void NotifyWindy(string kind, ulong author, ulong authorOwner, string handle, string text, string content, bool photo = false)
	{
		ulong[] array = online.Where((ulong id) => id != author && listenReady.ContainsKey(id)).ToArray();
		foreach (ulong num2 in array)
		{
			ulong num3 = (LeaseValid(num2) ? access.Get(num2).Owner : num2);
			if (num3 != authorOwner)
			{
				PlayerID playerID = PlayerIDManager.GetPlayerID(num2);
				if (playerID != null && playerID.IsValid)
				{
					SendNotice(playerID, new PhoneNotice
					{
						Kind = kind,
						Owner = num3,
						Name = handle,
						Text = text,
						Number = content
					});
				}
			}
		}
	}

	private void AcceptNotice(PhoneNotice n)
	{
		if (n.Protocol != 16 || n.Player != PlayerIDManager.LocalPlatformID || n.Session != View.Session || !Guid.TryParseExact(n.Id, "N", out var _) || !seenNotices.Add(n.Id))
		{
			return;
		}
		if (seenNotices.Count > 256)
		{
			seenNotices.Clear();
		}
		if (Notes.Notice(n) || Media.HandleNotice(n))
		{
			return;
		}
		if (n.Kind == "windyPost" || n.Kind == "windyLive")
		{
			string text = "@" + n.Name + " " + PhoneLanguage.Text(Sounds.Preferences.Language, (n.Kind == "windyLive") ? "vient de lancer un live" : "a publié sur Windy");
			if (n.Kind == "windyPost" && n.Text != "")
			{
				text = text + " • " + ((n.Text.Length > 80) ? (n.Text.Substring(0, 79) + "…") : n.Text);
			}
			LocalPopup("Windy", text, delegate
			{
				app.OpenWindyNotification(n.Kind == "windyLive");
			}, n.Owner);
			return;
		}
		if (n.Kind == "share")
		{
			SharedNumbers.Insert(0, n);
			if (SharedNumbers.Count > 10)
			{
				SharedNumbers.RemoveAt(10);
			}
		}
		LocalPopup((n.Kind == "share") ? "AirDrop" : "Banque", n.Text, (n.Kind == "share") ? ((Action)delegate
		{
			app.OpenShared(n);
		}) : null, n.Owner);
		app.Refresh();
	}

	private void LocalPopup(string title, string text, Action accept = null, ulong owner = 0uL)
	{
		Sounds.Message();
		app.PushNotification(title, text, accept, (owner == 0L) ? PlayerIDManager.LocalPlatformID : owner);
	}

	private void Transfer(PlayerID actor, PhoneCommand c, ulong owner)
	{
		if (!ServerEconomy.IsHosting)
		{
			throw new InvalidOperationException("Banque LabRP indisponible.");
		}
		ulong num = service.FindPlayer(c.Number) ?? throw new InvalidOperationException("Numéro inconnu.");
		PlayerID playerID = PlayerIDManager.GetPlayerID(num);
		if (playerID == null || !playerID.IsValid || num == owner)
		{
			throw new InvalidOperationException("Choisir un autre joueur connecté.");
		}
		long balance = ServerEconomy.GetBalance(owner);
		long balance2 = ServerEconomy.GetBalance(num);
		if (c.Amount <= 0 || c.Amount > 999999999999L || balance < c.Amount || balance2 > 999999999999L - c.Amount)
		{
			throw new InvalidOperationException("Montant invalide, fonds insuffisants ou compte destinataire plein.");
		}
		if (!service.ReserveExternal(owner, c.Nonce))
		{
			Publish(actor.PlatformID, "Commande déjà traitée.");
			return;
		}
		ServerEconomy.TryTransfer((PlayerIDManager.GetPlayerID(owner) ?? throw new InvalidOperationException("Propriétaire hors ligne.")).SmallID, num, c.Amount);
		bool flag = ServerEconomy.GetBalance(owner) == balance - c.Amount && ServerEconomy.GetBalance(num) == balance2 + c.Amount;
		if (flag)
		{
			ServerEconomy.SaveIfHosting();
			try
			{
				service.BankMovement(owner, num, c.Amount, c.Nonce, DateTime.UtcNow);
			}
			catch (Exception ex)
			{
				Warning("Virement effectué, historique indisponible : " + ex.Message);
			}
			Publish(num);
			PublishReaders(num);
			SendNotice(PlayerIDManager.GetPlayerID(ControllerFor(num)) ?? playerID, new PhoneNotice
			{
				Owner = num,
				Kind = "bank",
				Text = "Virement reçu : " + c.Amount + " $ de " + PhoneService.Display(service.Ensure(owner).Number)
			});
		}
		PublishReaders(owner);
		Publish(actor.PlatformID, flag ? ("Virement effectué : " + c.Amount + " $.") : "Virement refusé par LabRP. Attendre puis réessayer.");
	}

	private void Warning(string text)
	{
		((MelonBase)this).LoggerInstance.Warning(text);
	}

	private void Unbind()
	{
		ClearAccessLocal();
		PhoneShutter.RestoreMenu();
		surface?.Dispose();
		surface = null;
		bound = null;
		app?.Bind(null);
	}

	private void Reset()
	{
		parking.Clear();
		Notes?.Clear();
		calls.StopAll();
		ShowAdmin(host: false);
		Unbind();
		ResetExperience();
		windy = null;
		Windy = new WindyView();
		windyViews.Clear();
		windyOffsets.Clear();
		lastWindyPosts.Clear();
		publicPhotos.Clear();
		windyRevision = 0L;
		Media?.Reset();
		devices.Reset();
		service = null;
		online.Clear();
		lastCommands.Clear();
		deviceStatuses.Clear();
		commands.Clear();
		snapshots.Clear();
		retiredSessions.Clear();
		callUpdates.Clear();
		voiceFrames.Clear();
		calls.StopAll();
		listenReady.Clear();
		lastShares.Clear();
		SharedNumbers.Clear();
		seenNotices.Clear();
		notices.Clear();
		speakers.Clear();
		voiceReady.Clear();
		callNonces.Clear();
		audioSequences.Clear();
		retiredCalls.Clear();
		voice?.Dispose();
		CallMicMuted = false;
		callRevision = 0L;
		nextHeartbeat = (lastCallSync = 0f);
		View = new PhoneView();
		connected = (wasHost = (loading = false));
		scene = (session = "");
		revision = 0L;
		if (app != null)
		{
			app = new PhoneApp(this);
			Notice("Rejoindre un lobby Fusion.");
		}
	}

	public override void OnDeinitializeMelon()
	{
		Reset();
		Sounds?.Dispose();
		Media?.Dispose();
		Notes?.Dispose();
		Instance = null;
	}

	private void SaveConfig()
	{
		configuration.Validate();
		Directory.CreateDirectory(directory);
		string text = Path.Combine(directory, "configuration.json");
		string text2 = text + ".tmp";
		File.WriteAllText(text2, JsonSerializer.Serialize(configuration, new JsonSerializerOptions
		{
			WriteIndented = true
		}));
		File.Move(text2, text, overwrite: true);
	}

	private void Configure(Action<ScreenSettings> change)
	{
		if (!NetworkInfo.HasServer || !NetworkInfo.IsHost || service == null)
		{
			Notice("Réglages réservés à l'hôte.");
			return;
		}
		string json = JsonSerializer.Serialize(configuration);
		try
		{
			change(configuration.Screen);
			SaveConfig();
			foreach (ulong item in online)
			{
				Publish(item);
			}
		}
		catch (Exception ex)
		{
			configuration = JsonSerializer.Deserialize<PhoneConfiguration>(json);
			Notice(ex.Message);
		}
	}

	private void ShowAdmin(bool host)
	{
		if (host == adminVisible)
		{
			return;
		}
		if (host)
		{
			menuRoot.CreatePageLink(menuAdmin);
		}
		else
		{
			menuRoot.RemovePageLinks(menuAdmin);
			if (Menu.CurrentPage == menuAdmin)
			{
				Menu.OpenPage(menuRoot);
			}
		}
		adminVisible = host;
	}

	private void BuildMenu()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		Menu.Initialize();
		Page val = (menuRoot = Page.Root.CreatePage("Téléphone", Color.cyan, 0, true));
		menuNumber = val.CreateFunction("Mon numéro : en attente", Color.white, (Action)delegate
		{
		});
		menuStatus = val.CreateFunction(Status, Color.white, (Action)delegate
		{
		});
		val.CreateFunction("Synchroniser", Color.cyan, (Action)delegate
		{
			Request("sync", "", "", "", "", enabled: false, 0L);
		});
		val.CreateFunction("Rappeler mon téléphone devant moi", Color.green, (Action)delegate
		{
			Request("spawn", "", "", "", "", enabled: false, 0L);
		});
		val.CreateFunction("Accepter l'appel", Color.green, (Action)delegate
		{
			Request("answer", "", "", "", View.Call.Id, enabled: false, 0L);
		});
		val.CreateFunction("Refuser l'appel", Color.red, (Action)delegate
		{
			Request("reject", "", "", "", View.Call.Id, enabled: false, 0L);
		});
		val.CreateFunction("Raccrocher / annuler", Color.red, (Action)delegate
		{
			Request("hangup", "", "", "", View.Call.Id, enabled: false, 0L);
		});
		val.CreateFunction("Activer / couper mon micro d'appel", Color.yellow, (Action)ToggleCallMicrophone);
		val.CreateFunction("Activer / couper ma caméra", Color.cyan, (Action)delegate
		{
			Request("video", "", "", "", View.Call.Id, !View.Call.Video, 0L);
		});
		val.CreateFunction("Diagnostic audio", Color.white, (Action)delegate
		{
			((MelonBase)this).LoggerInstance.Msg($"Voix prête={PhoneVoice.Ready}, appel={View.Call.State}, pair={View.Call.Number}, micro coupé={CallMicMuted}");
		});
		val.CreateFunction("Diagnostic écran", Color.white, (Action)delegate
		{
			((MelonBase)this).LoggerInstance.Msg($"Session={View.Session}, téléphone={View.Device}, écran={surface != null}, placement={surface?.PlacementInfo}, état={Status}");
		});
		Page val2 = (menuAdmin = val.CreatePage("Écran — hôte uniquement", Color.yellow, 0, false));
		val2.CreateFunction("Inverser la face", Color.cyan, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.ReverseFace = !s.ReverseFace;
			});
		});
		val2.CreateFunction("Taille +10 %", Color.cyan, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.SizeMultiplier *= 1.1f;
			});
		});
		val2.CreateFunction("Taille -10 %", Color.cyan, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.SizeMultiplier /= 1.1f;
			});
		});
		val2.CreateFunction("X +1 cm", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.X += 0.01f;
			});
		});
		val2.CreateFunction("X -1 cm", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.X -= 0.01f;
			});
		});
		val2.CreateFunction("Y +1 cm", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.Y += 0.01f;
			});
		});
		val2.CreateFunction("Y -1 cm", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.Y -= 0.01f;
			});
		});
		val2.CreateFunction("Z +1 mm", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.Z += 0.001f;
			});
		});
		val2.CreateFunction("Z -1 mm", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.Z -= 0.001f;
			});
		});
		val2.CreateFunction("Rotation X +90°", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.RotationX += 90f;
			});
		});
		val2.CreateFunction("Rotation Y +90°", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.RotationY += 90f;
			});
		});
		val2.CreateFunction("Rotation Z +90°", Color.white, (Action)delegate
		{
			Configure(delegate(ScreenSettings s)
			{
				s.RotationZ += 90f;
			});
		});
		val2.CreateFunction("Réinitialiser l'écran", Color.red, (Action)delegate
		{
			Configure(delegate
			{
				configuration.Screen = new ScreenSettings();
			});
		});
	}
}
