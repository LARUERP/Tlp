using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LabFusion.Network;
using LabFusion.Player;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneMedia : IDisposable
{
	private readonly PhoneMod mod;

	private string localLive = "";

	private uint liveFrame;

	private uint receivedLive;

	private float liveAt;

	private float nextFetch;

	private readonly Dictionary<string, Texture2D> thumbnails = new Dictionary<string, Texture2D>();

	private readonly Dictionary<string, (float At, int Tries)> fetchAttempts = new Dictionary<string, (float, int)>();

	private readonly Queue<string> fetchQueue = new Queue<string>();

	private readonly HashSet<string> wanted = new HashSet<string>();

	private readonly Queue<(string Id, ulong Owner)> captured = new Queue<(string, ulong)>();

	private float nextUpload;

	private string galleryPending = "";

	private float galleryRetry;

	private ulong galleryOwner;

	private ulong uploadOwner;

	private ushort? uploadDevice;

	private string uploadAccess = "";

	private readonly string folder;

	private PhoneMediaStore hostStore;

	private readonly PhoneMediaAssembler assembler = new PhoneMediaAssembler();

	private readonly ConcurrentQueue<(ulong Actor, PhoneMediaChunk Chunk, bool Host)> incoming = new ConcurrentQueue<(ulong, PhoneMediaChunk, bool)>();

	private readonly Queue<(byte? Target, PhoneMediaChunk Chunk)> reliable = new Queue<(byte?, PhoneMediaChunk)>();

	private readonly Queue<(byte? Target, PhoneMediaChunk Chunk)> video = new Queue<(byte?, PhoneMediaChunk)>();

	private readonly Dictionary<ulong, (float At, int Count)> budgets = new Dictionary<ulong, (float, int)>();

	private readonly Dictionary<ulong, (string Call, uint Frame, float At)> frames = new Dictionary<ulong, (string, uint, float)>();

	private readonly Dictionary<ulong, float> fetches = new Dictionary<ulong, float>();

	private float nextSend;

	private float nextCapture;

	private float nextPreview;

	private float remoteAt;

	private float uploadDeadline;

	private float nextCleanup;

	private bool photoRequested;

	private readonly Dictionary<ulong, float> uploads = new Dictionary<ulong, float>();

	private uint sendFrame;

	private uint receivedFrame;

	private string currentCall = "";

	private string remoteCall = "";

	private string uploadId = "";

	private string uploadNumber = "";

	private string uploadText = "";

	private string uploadOperation = "sms";

	private string pendingPhoto = "";

	public Texture2D RemoteLive { get; private set; }

	public PhoneMediaStore Photos { get; }

	public PhoneCamera Camera { get; private set; }

	public Texture2D RemoteVideo { get; private set; }

	public Texture2D OpenPhoto { get; private set; }

	public bool PreviewPage { get; set; }

	public bool PhotoPage { get; set; }

	public bool FrontCamera { get; set; }

	public bool UploadBusy => uploadId != "";

	public PhoneMedia(PhoneMod mod, string folder)
	{
		this.mod = mod;
		this.folder = folder;
		Photos = new PhoneMediaStore(Path.Combine(folder, "photos"), 800);
		Camera = new PhoneCamera(mod.MediaWarning);
	}

	public void BeginHost()
	{
		hostStore = new PhoneMediaStore(Path.Combine(folder, "server-media"));
	}

	public bool Owns(ulong actor, string id)
	{
		return hostStore != null && hostStore.Owns(actor, id);
	}

	public void Enqueue(ulong actor, PhoneMediaChunk c, bool host)
	{
		if (incoming.Count < (PhoneMediaRules.Stream(c.Kind) ? 256 : 8192))
		{
			incoming.Enqueue((actor, c, host));
		}
	}

	public void Tick(PhoneSurface screen)
	{
		PhoneShutter.Poll(screen);
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		int num = 64;
		(ulong, PhoneMediaChunk, bool) result;
		while (num-- > 0 && incoming.TryDequeue(out result))
		{
			try
			{
				if (result.Item3 && NetworkInfo.IsHost)
				{
					ReceiveHost(result.Item1, result.Item2, realtimeSinceStartup);
				}
				else if (!result.Item3)
				{
					ReceiveLocal(result.Item2, realtimeSinceStartup);
				}
			}
			catch (Exception ex)
			{
				if (result.Item3)
				{
					if (result.Item2.Kind == "upload" || result.Item2.Kind == "voiceUpload")
					{
						mod.MediaUploadFailed(result.Item1, result.Item2.Id, ex.Message);
					}
					else
					{
						mod.MediaStatus(result.Item1, ex.Message);
					}
				}
				else
				{
					mod.LocalStatus(ex.Message);
				}
			}
		}
		assembler.Expire(realtimeSinceStartup);
		if (mod.ScreenUnlocked && !UploadBusy && captured.Count == 0)
		{
			string text = Photos.Captured(mod.ScreenOwner).FirstOrDefault((string id) => !mod.Access.Gallery.Contains(id) && !mod.Access.Deleted.Contains(id) && id != galleryPending);
			if (text != null)
			{
				captured.Enqueue((text, mod.ScreenOwner));
			}
		}
		if (galleryPending != "" && mod.ScreenUnlocked && galleryOwner == mod.ScreenOwner)
		{
			if (mod.Access.Gallery.Contains(galleryPending))
			{
				galleryPending = "";
			}
			else if (realtimeSinceStartup >= galleryRetry)
			{
				galleryRetry = realtimeSinceStartup + 2f;
				mod.Request("galleryAdd", "", "", "", "", enabled: false, 0L, galleryPending);
			}
		}
		if (mod.ScreenUnlocked || mod.VisibleWallpaper != "")
		{
			KeyValuePair<string, (float, int)>[] array = fetchAttempts.ToArray();
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				KeyValuePair<string, (float, int)> keyValuePair = array[num2];
				if (realtimeSinceStartup - keyValuePair.Value.Item1 > 4f && keyValuePair.Value.Item2 < 3)
				{
					fetchAttempts[keyValuePair.Key] = (realtimeSinceStartup, keyValuePair.Value.Item2);
					if (!fetchQueue.Contains(keyValuePair.Key))
					{
						fetchQueue.Enqueue(keyValuePair.Key);
					}
				}
			}
			if (realtimeSinceStartup >= nextFetch && fetchQueue.Count > 0)
			{
				nextFetch = realtimeSinceStartup + 1.1f;
				string text2 = fetchQueue.Dequeue();
				fetchAttempts.TryGetValue(text2, out (float, int) value);
				fetchAttempts[text2] = (realtimeSinceStartup, value.Item2 + 1);
				if (mod.ScreenUnlocked)
				{
					mod.Request("photoFetch", "", "", "", "", enabled: false, 0L, text2);
				}
				else if (text2 == mod.VisibleWallpaper)
				{
					mod.Request("wallpaperFetch", "", "", "", "", enabled: false, 0L, text2);
				}
			}
		}
		if (mod.ScreenUnlocked && !UploadBusy && captured.Count > 0 && realtimeSinceStartup >= nextUpload)
		{
			(string, ulong) tuple = captured.Dequeue();
			if (tuple.Item2 == mod.ScreenOwner)
			{
				nextUpload = realtimeSinceStartup + 2.1f;
				StartUpload(tuple.Item1, "", "", "galleryAdd");
			}
		}
		string text3 = mod.Live.Active?.Id ?? "";
		if (text3 != localLive)
		{
			localLive = text3;
			liveFrame = (receivedLive = 0u);
			ClearLive();
		}
		if (!mod.IsWatching || realtimeSinceStartup - liveAt > 3f)
		{
			ClearLive();
		}
		if (NetworkInfo.IsHost && hostStore != null && realtimeSinceStartup >= nextCleanup)
		{
			nextCleanup = realtimeSinceStartup + 60f;
			hostStore.Prune(mod.PhotoReferenced, DateTime.UtcNow.AddMinutes(-15.0));
		}
		if (uploadId != "" && realtimeSinceStartup >= uploadDeadline)
		{
			uploadId = "";
			mod.LocalStatus("Transfert photo expiré, réessayer.");
		}
		if (realtimeSinceStartup >= nextSend)
		{
			nextSend = realtimeSinceStartup + 0.025f;
			for (int num3 = 0; num3 < 20; num3++)
			{
				Queue<(byte?, PhoneMediaChunk)> queue = ((num3 < 4 && video.Count > 0) ? video : ((reliable.Count > 0) ? reliable : video));
				if (queue.Count == 0)
				{
					break;
				}
				(byte?, PhoneMediaChunk) tuple2 = queue.Dequeue();
				PhoneMediaChunk item = tuple2.Item2;
				if (item.Session != mod.View.Session || (item.Kind == "live" && !tuple2.Item1.HasValue && (!mod.IsCasting || item.Call != mod.Live.Active.Id)) || (item.Kind == "video" && !tuple2.Item1.HasValue && (!mod.CallActive || !mod.View.Call.Video || item.Call != mod.View.Call.Id)))
				{
					continue;
				}
				if (tuple2.Item1.HasValue)
				{
					PlayerID playerID = PlayerIDManager.GetPlayerID(tuple2.Item1.Value);
					if (playerID != null && playerID.IsValid && (!(item.Kind == "live") || mod.LiveRouteValid(item.Call, playerID.PlatformID)) && (!(item.Kind == "video") || mod.MediaRouteValid(item.Call, playerID.PlatformID)) && (!(item.Kind == "download") || mod.CanReadPhoto(playerID.PlatformID, item.Id)) && (!(item.Kind == "voiceDownload") || mod.CanReadVoice(playerID.PlatformID, item.Id)))
					{
						if (playerID.IsMe)
						{
							Enqueue(0uL, item, host: false);
						}
						else
						{
							PhoneMediaTransport.Reply(item, playerID.SmallID);
						}
					}
				}
				else if (NetworkInfo.IsHost)
				{
					Enqueue(PlayerIDManager.LocalPlatformID, item, host: true);
				}
				else
				{
					PhoneMediaTransport.Send(item);
				}
			}
		}
		PhoneCallView call = mod.View.Call;
		if (currentCall != call.Id)
		{
			currentCall = call.Id;
			sendFrame = (receivedFrame = 0u);
			ClearRemote();
		}
		if (!mod.CallActive || !call.PeerVideo || realtimeSinceStartup - remoteAt > 3f)
		{
			ClearRemote();
		}
		if (photoRequested && !Camera.Busy)
		{
			photoRequested = false;
			ulong photoOwner = mod.ScreenOwner;
			string photoToken = mod.Access.Token;
			if (!Camera.Capture(screen, large: true, FrontCamera, encode: true, delegate(byte[] bytes)
			{
				string text4 = Guid.NewGuid().ToString("N");
				Photos.Save(text4, photoOwner, bytes);
				Photos.MarkCaptured(text4, photoOwner);
				if (mod.ScreenUnlocked && mod.ScreenOwner == photoOwner && mod.Access.Token == photoToken)
				{
					captured.Enqueue((text4, photoOwner));
					mod.LocalStatus("Photo prise • enregistrement dans la Galerie…");
				}
				else
				{
					mod.LocalStatus("Photo conservée localement ; synchronisation au prochain déverrouillage.");
				}
				mod.RefreshApp();
			}))
			{
				mod.LocalStatus("Tenir le téléphone pour prendre la photo.");
			}
			nextCapture = realtimeSinceStartup + 0.03f;
		}
		bool flag = mod.ScreenUnlocked && mod.CallActive && call.Video;
		if (screen == null || (!screen.IsHeld && !mod.IsCasting) || Camera.Busy)
		{
			return;
		}
		bool firstPreview = (Object)(object)Camera.Preview == (Object)null;
		if (mod.IsCasting && realtimeSinceStartup >= nextCapture)
		{
			nextCapture = realtimeSinceStartup + 1f / Math.Min(mod.Sounds.Preferences.VideoFps, Math.Min(15f, 60f / (float)Math.Max(1, mod.Live.Active.Viewers)));
			string captureLive = mod.Live.Active.Id;
			bool firstLive = (Object)(object)Camera.Live == (Object)null;
			Camera.Capture(screen, large: false, mod.Live.Active.Front, encode: true, delegate(byte[] bytes)
			{
				if (bytes != null && mod.IsCasting && mod.Live.Active.Id == captureLive)
				{
					Queue(null, PhoneMediaRules.Split(mod.View.Session, Guid.NewGuid().ToString("N"), "live", bytes, captureLive, ++liveFrame));
				}
				if (firstLive)
				{
					mod.RefreshApp();
				}
			}, photoPreview: false, allowUnheld: true);
		}
		else if (flag && realtimeSinceStartup >= nextCapture)
		{
			nextCapture = realtimeSinceStartup + 1f / (float)mod.Sounds.Preferences.VideoFps;
			string captureCall = call.Id;
			Camera.Capture(screen, large: false, front: true, encode: true, delegate(byte[] bytes)
			{
				if (bytes != null && mod.CallActive && mod.View.Call.Video && mod.View.Call.Id == captureCall)
				{
					uint frame = ++sendFrame;
					Queue(null, PhoneMediaRules.Split(mod.View.Session, Guid.NewGuid().ToString("N"), "video", bytes, captureCall, frame));
				}
				if (firstPreview)
				{
					mod.RefreshApp();
				}
			});
		}
		else
		{
			if ((!PhotoPage && !PreviewPage) || !(realtimeSinceStartup >= nextPreview))
			{
				return;
			}
			nextPreview = realtimeSinceStartup + 1f / (float)mod.Sounds.Preferences.PhotoFps;
			Camera.Capture(screen, large: false, FrontCamera, encode: false, delegate
			{
				if (firstPreview)
				{
					mod.RefreshApp();
				}
			}, photoPreview: true);
		}
	}

	public void QueueVoice(byte? target, PhoneMediaChunk[] chunks)
	{
		Queue(target, chunks);
	}

	private void Queue(byte? target, PhoneMediaChunk[] chunks)
	{
		Queue<(byte?, PhoneMediaChunk)> queue = (PhoneMediaRules.Stream(chunks[0].Kind) ? video : reliable);
		if (queue.Count + chunks.Length > (PhoneMediaRules.Stream(chunks[0].Kind) ? 256 : (PhoneMediaRules.Voice(chunks[0].Kind) ? 4096 : 512)))
		{
			if (!PhoneMediaRules.Stream(chunks[0].Kind))
			{
				throw new InvalidOperationException("Transfert occupé, réessayer.");
			}
			return;
		}
		foreach (PhoneMediaChunk phoneMediaChunk in chunks)
		{
			if (!target.HasValue && (phoneMediaChunk.Kind == "upload" || phoneMediaChunk.Kind == "voiceUpload"))
			{
				phoneMediaChunk.Owner = mod.ScreenOwner;
				phoneMediaChunk.Device = mod.ScreenDevice;
				phoneMediaChunk.Access = mod.Access.Token;
			}
			queue.Enqueue((target, phoneMediaChunk));
		}
	}

	private void ForgetGalleryPhoto(string id)
	{
		Photos.UnmarkCaptured(id);
		if (galleryPending == id)
		{
			galleryPending = "";
		}
		(string, ulong)[] array = captured.Where(((string Id, ulong Owner) c) => c.Id != id).ToArray();
		captured.Clear();
		(string, ulong)[] array2 = array;
		foreach ((string, ulong) item in array2)
		{
			captured.Enqueue(item);
		}
		if (uploadOperation == "galleryAdd" && uploadId == id)
		{
			uploadId = "";
		}
	}

	public void DeleteGalleryPhoto(string id)
	{
		ForgetGalleryPhoto(id);
		Photos.Delete(id);
		mod.Request("galleryDelete", "", "", "", "", enabled: false, 0L, id);
	}

	public void CapturePhoto(PhoneSurface screen)
	{
		if (!mod.ScreenUnlocked || screen == null || !screen.IsHeld || !screen.PlacementReady)
		{
			mod.LocalStatus("Tenir le téléphone et attendre l'écran.");
			return;
		}
		if (mod.Gallery.Count >= 100)
		{
			mod.LocalStatus("Galerie pleine (100 photos). Supprimer une photo.");
			return;
		}
		photoRequested = true;
		mod.LocalStatus("Prise de photo…");
	}

	public void SendPhoto(string id, string number, string text)
	{
		StartUpload(id, PhoneService.Normalize(number), text, "sms");
	}

	public void SendWindyPhoto(string id, string text)
	{
		StartUpload(id, "", text, "windyPost");
	}

	private void StartUpload(string id, string number, string text, string operation)
	{
		if (!mod.ScreenUnlocked)
		{
			throw new InvalidOperationException("Déverrouiller le téléphone.");
		}
		if (UploadBusy)
		{
			throw new InvalidOperationException("Une photo est déjà en cours d'envoi.");
		}
		byte[] array = Photos.Read(id);
		if (!PhoneMediaRules.Jpeg(array))
		{
			throw new InvalidOperationException("Photo invalide.");
		}
		string id2 = ((operation == "galleryAdd") ? id : Guid.NewGuid().ToString("N"));
		Photos.Save(id2, mod.ScreenOwner, array);
		Queue(null, PhoneMediaRules.Split(mod.View.Session, id2, "upload", array));
		uploadOwner = mod.ScreenOwner;
		uploadDevice = mod.ScreenDevice;
		uploadAccess = mod.Access.Token;
		uploadId = id2;
		uploadNumber = number;
		uploadText = text;
		uploadOperation = operation;
		uploadDeadline = Time.realtimeSinceStartup + 25f;
		mod.LocalStatus("Envoi de la photo…");
	}

	public bool HandleNotice(PhoneNotice n)
	{
		if (n.Kind == "galleryRemoved")
		{
			if (Guid.TryParseExact(n.Number, "N", out var _))
			{
				ForgetGalleryPhoto(n.Number);
				mod.RefreshApp();
			}
			return true;
		}
		if (n.Kind == "uploadFailed")
		{
			if (n.Number == uploadId)
			{
				uploadId = "";
				mod.LocalStatus(n.Text);
			}
			return true;
		}
		if (n.Kind != "uploadReady")
		{
			return false;
		}
		if (n.Number == uploadId)
		{
			string photo = uploadId;
			uploadId = "";
			if (mod.ScreenUnlocked && mod.ScreenOwner == uploadOwner && mod.ScreenDevice == uploadDevice && mod.Access.Token == uploadAccess)
			{
				if (uploadOperation == "galleryAdd")
				{
					galleryPending = photo;
					galleryOwner = uploadOwner;
					galleryRetry = Time.realtimeSinceStartup + 2f;
				}
				mod.Request(uploadOperation, uploadNumber, "", uploadText, "", enabled: false, 0L, photo);
			}
		}
		return true;
	}

	public Texture2D PictureFor(string id)
	{
		if (!mod.IsPhotoKnown(id))
		{
			return null;
		}
		if (thumbnails.TryGetValue(id, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		try
		{
			byte[] bytes = Photos.Read(id);
			return DecodePhoto(id, bytes);
		}
		catch (IOException)
		{
		}
		catch (InvalidOperationException)
		{
		}
		if (wanted.Add(id))
		{
			fetchQueue.Enqueue(id);
		}
		return null;
	}

	public void ViewPhoto(string id)
	{
		pendingPhoto = id;
		OpenPhoto = PictureFor(id);
	}

	private Texture2D DecodePhoto(string id, byte[] bytes)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		if (!mod.IsPhotoKnown(id) || !PhoneMediaRules.Jpeg(bytes))
		{
			throw new InvalidOperationException("Photo reçue invalide.");
		}
		Texture2D val = new Texture2D(2, 2, (TextureFormat)3, false);
		try
		{
			if (!ImageConversion.LoadImage(val, Il2CppStructArray<byte>.op_Implicit(bytes), false))
			{
				throw new InvalidOperationException("Photo illisible.");
			}
		}
		catch
		{
			Object.Destroy((Object)(object)val);
			throw;
		}
		if (thumbnails.TryGetValue(id, out var value) && (Object)(object)value != (Object)null)
		{
			Object.Destroy((Object)(object)value);
		}
		if (thumbnails.Count >= 18)
		{
			string key = thumbnails.Keys.First();
			Object.Destroy((Object)(object)thumbnails[key]);
			thumbnails.Remove(key);
		}
		thumbnails[id] = val;
		wanted.Remove(id);
		fetchAttempts.Remove(id);
		if (pendingPhoto == id)
		{
			OpenPhoto = val;
		}
		mod.RefreshApp();
		return val;
	}

	public void Fetch(ulong actor, string id)
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (fetches.TryGetValue(actor, out var value) && realtimeSinceStartup - value < 1f)
		{
			throw new InvalidOperationException("Attendre avant de recharger une photo.");
		}
		if (hostStore == null || !mod.CanReadPhoto(actor, id))
		{
			throw new InvalidOperationException("Accès à cette photo refusé.");
		}
		fetches[actor] = realtimeSinceStartup;
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID != null && playerID.IsValid)
		{
			Queue(playerID.SmallID, PhoneMediaRules.Split(mod.ServerSession, id, "download", hostStore.Read(id)));
		}
	}

	private void ReceiveHost(ulong actor, PhoneMediaChunk c, float now)
	{
		if (c.Session != mod.ServerSession || c.Kind == "download" || c.Kind == "voiceDownload" || hostStore == null)
		{
			return;
		}
		PlayerID playerID = PlayerIDManager.GetPlayerID(actor);
		if (playerID == null || !playerID.IsValid)
		{
			return;
		}
		budgets.TryGetValue(actor, out (float, int) value);
		if (now - value.Item1 >= 1f)
		{
			value = (now, 0);
		}
		if (++value.Item2 > 900)
		{
			return;
		}
		budgets[actor] = value;
		if (c.Kind == "voiceUpload")
		{
			mod.UploadOwner(actor, c);
		}
		ulong peer = 0uL;
		if ((c.Kind == "live" && mod.LiveTargets(actor, c.Call).Length == 0) || (c.Kind == "video" && !mod.MediaVideoRoute(actor, c.Call, out peer)) || (PhoneMediaRules.Stream(c.Kind) && frames.TryGetValue(actor, out (string, uint, float) value2) && value2.Item1 == c.Call && c.Frame <= value2.Item2))
		{
			return;
		}
		byte[] array = assembler.Add(actor, c, now);
		if (array == null)
		{
			return;
		}
		if (c.Kind == "voiceUpload")
		{
			mod.Notes.SaveUpload(actor, c, array);
			return;
		}
		if (!PhoneMediaRules.Jpeg(array, PhoneMediaRules.Stream(c.Kind) ? 320 : 640, PhoneMediaRules.Stream(c.Kind) ? 240 : 480))
		{
			throw new InvalidOperationException("Image réseau invalide.");
		}
		if (c.Kind == "upload")
		{
			ulong owner = mod.UploadOwner(actor, c);
			if (!hostStore.Owns(owner, c.Id))
			{
				if (uploads.TryGetValue(actor, out var value3) && now - value3 < 2f)
				{
					throw new InvalidOperationException("Attendre deux secondes entre deux photos envoyées.");
				}
				if (hostStore.OwnedCount(owner) >= 200)
				{
					throw new InvalidOperationException("Quota serveur de photos atteint pour ce joueur.");
				}
				uploads[actor] = now;
			}
			hostStore.Save(c.Id, owner, array);
			mod.MediaUploadReady(playerID, c.Id);
		}
		else
		{
			if (frames.TryGetValue(actor, out (string, uint, float) value4) && value4.Item1 == c.Call && (c.Frame <= value4.Item2 || now - value4.Item3 < 0.035f))
			{
				return;
			}
			frames[actor] = (c.Call, c.Frame, now);
			if (c.Kind == "live")
			{
				ulong[] array2 = mod.LiveTargets(actor, c.Call);
				foreach (ulong num in array2)
				{
					PlayerID playerID2 = PlayerIDManager.GetPlayerID(num);
					if (playerID2 != null && playerID2.IsValid)
					{
						Queue(playerID2.SmallID, PhoneMediaRules.Split(mod.ServerSession, c.Id, "live", array, c.Call, c.Frame));
					}
				}
			}
			else
			{
				PlayerID playerID3 = PlayerIDManager.GetPlayerID(peer);
				if (playerID3 != null && playerID3.IsValid)
				{
					Queue(playerID3.SmallID, PhoneMediaRules.Split(mod.ServerSession, c.Id, "video", array, c.Call, c.Frame));
				}
			}
		}
	}

	private void ReceiveLocal(PhoneMediaChunk c, float now)
	{
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Expected O, but got Unknown
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Expected O, but got Unknown
		if (c.Session != mod.View.Session || c.Kind == "upload" || c.Kind == "voiceUpload" || (c.Kind == "voiceDownload" && !mod.IsVoiceKnown(c.Id)) || (c.Kind == "live" && (!mod.IsWatching || c.Call != mod.Live.Active.Id || c.Frame <= receivedLive)) || (c.Kind == "video" && (!mod.CallActive || !mod.View.Call.PeerVideo || c.Call != mod.View.Call.Id || c.Frame <= receivedFrame)) || (c.Kind == "download" && !mod.IsPhotoKnown(c.Id)))
		{
			return;
		}
		byte[] array = assembler.Add(0uL, c, now);
		if (array == null)
		{
			return;
		}
		if (c.Kind == "voiceDownload")
		{
			mod.Notes.Receive(c.Id, array);
		}
		else
		{
			if (!PhoneMediaRules.Jpeg(array, PhoneMediaRules.Stream(c.Kind) ? 320 : 640, PhoneMediaRules.Stream(c.Kind) ? 240 : 480))
			{
				return;
			}
			if (c.Kind == "download")
			{
				Photos.Save(c.Id, 0uL, array);
				DecodePhoto(c.Id, array);
			}
			else if (c.Kind == "live")
			{
				if (c.Frame <= receivedLive)
				{
					return;
				}
				Texture2D val = RemoteLive;
				bool flag = (Object)(object)val == (Object)null;
				if (flag)
				{
					val = new Texture2D(2, 2, (TextureFormat)3, false);
				}
				if (!ImageConversion.LoadImage(val, Il2CppStructArray<byte>.op_Implicit(array), false))
				{
					if (flag)
					{
						Object.Destroy((Object)(object)val);
					}
					return;
				}
				RemoteLive = val;
				receivedLive = c.Frame;
				liveAt = now;
				if (flag)
				{
					mod.RefreshApp();
				}
			}
			else
			{
				if (c.Frame <= receivedFrame)
				{
					return;
				}
				Texture2D val2 = RemoteVideo;
				bool flag2 = (Object)(object)val2 == (Object)null;
				if (flag2)
				{
					val2 = new Texture2D(2, 2, (TextureFormat)3, false);
				}
				if (!ImageConversion.LoadImage(val2, Il2CppStructArray<byte>.op_Implicit(array), false))
				{
					if (flag2)
					{
						Object.Destroy((Object)(object)val2);
					}
					return;
				}
				RemoteVideo = val2;
				receivedFrame = c.Frame;
				remoteAt = now;
				remoteCall = c.Call;
				if (flag2)
				{
					mod.RefreshApp();
				}
			}
		}
	}

	private void ClearRemote()
	{
		if ((Object)(object)RemoteVideo != (Object)null)
		{
			Object.Destroy((Object)(object)RemoteVideo);
			RemoteVideo = null;
			mod.RefreshApp();
		}
		remoteCall = "";
	}

	private void ClearLive()
	{
		if ((Object)(object)RemoteLive != (Object)null)
		{
			Object.Destroy((Object)(object)RemoteLive);
			RemoteLive = null;
			mod.RefreshApp();
		}
	}

	public void ClearPrivate()
	{
		foreach (Texture2D value in thumbnails.Values)
		{
			if ((Object)(object)value != (Object)null)
			{
				Object.Destroy((Object)(object)value);
			}
		}
		thumbnails.Clear();
		fetchQueue.Clear();
		fetchAttempts.Clear();
		wanted.Clear();
		captured.Clear();
		OpenPhoto = null;
		pendingPhoto = (uploadId = (galleryPending = ""));
		bool photoPage = (PreviewPage = false);
		PhotoPage = photoPage;
		photoRequested = false;
		(byte?, PhoneMediaChunk)[] array = reliable.Where(((byte? Target, PhoneMediaChunk Chunk) x) => x.Target.HasValue).ToArray();
		reliable.Clear();
		if (NetworkInfo.IsHost)
		{
			(byte?, PhoneMediaChunk)[] array2 = array;
			foreach ((byte?, PhoneMediaChunk) item in array2)
			{
				reliable.Enqueue(item);
			}
		}
		(ulong, PhoneMediaChunk, bool)[] array3 = incoming.Where(((ulong Actor, PhoneMediaChunk Chunk, bool Host) x) => x.Host).ToArray();
		incoming.Clear();
		if (NetworkInfo.IsHost)
		{
			(ulong, PhoneMediaChunk, bool)[] array4 = array3;
			foreach ((ulong, PhoneMediaChunk, bool) item2 in array4)
			{
				incoming.Enqueue(item2);
			}
		}
		ClearLive();
	}

	public void Reset()
	{
		PhoneShutter.RestoreMenu();
		ClearPrivate();
		localLive = "";
		liveFrame = (receivedLive = 0u);
		incoming.Clear();
		reliable.Clear();
		video.Clear();
		assembler.Clear();
		budgets.Clear();
		frames.Clear();
		fetches.Clear();
		uploads.Clear();
		photoRequested = false;
		nextCleanup = 0f;
		hostStore = null;
		uploadId = (pendingPhoto = (currentCall = ""));
		sendFrame = (receivedFrame = 0u);
		nextCapture = (nextPreview = (nextSend = 0f));
		PhotoPage = false;
		ClearRemote();
		OpenPhoto = null;
		Camera.Dispose();
		Camera = new PhoneCamera(mod.MediaWarning);
	}

	public void Dispose()
	{
		Reset();
		Camera.Dispose();
	}
}
