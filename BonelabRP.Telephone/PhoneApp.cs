using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LabFusion.Player;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneApp
{
	private sealed class Toast
	{
		public ulong Owner;

		public string Title;

		public string Text;

		public Action Click;

		public float Until;
	}

	private readonly PhoneMod mod;

	private PhoneSurface surface;

	private string page = "home";

	private string number = "";

	private string name = "";

	private string body = "";

	private string editField = "";

	private string returnPage = "";

	private string attached = "";

	private string viewing = "";

	private string viewerBack = "gallery";

	private string attachDestination = "compose";

	private string windyName = "";

	private string windyBody = "";

	private string windyPhoto = "";

	private WindyPostView windySelected;

	private int offset;

	private string amount = "";

	private string clock = "";

	private bool callTone;

	private string lastCall = "";

	private string timedCall = "";

	private float callBegan;

	private int callSecond = -1;

	private bool dirty = true;

	private bool capitals;

	private bool bankClosing;

	private static readonly string[] FrenchDays = new string[7] { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };

	private static readonly string[] FrenchMonths = new string[12]
	{
		"janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre",
		"novembre", "décembre"
	};

	private string contactSearch = "";

	private static readonly Color ContactGreen = new Color(0.06f, 0.6f, 0.37f, 1f);

	private static readonly Color ContactRed = new Color(0.85f, 0.16f, 0.23f, 1f);

	private static readonly Color Blue = new Color(0.06f, 0.44f, 0.9f, 1f);

	private static readonly Color Navy = new Color(0.06f, 0.16f, 0.35f, 1f);

	private string pin = "";

	private string pinFirst = "";

	private string pinError = "";

	private string liveText = "";

	private bool pinConfirm;

	private bool wasUnlocked;

	private string liveUiId = "";

	private readonly List<Toast> notifications = new List<Toast>();

	private Toast banner;

	private float nextMessagesUi;

	private PhoneSms selectedMessage;

	private string Value => (editField == "dialNumber") ? number : ((editField == "contactSearch") ? contactSearch : ((editField == "number") ? number : ((editField == "liveText") ? liveText : ((editField == "windyName") ? windyName : ((editField == "windyBody") ? windyBody : ((editField == "name") ? name : ((editField == "amount") ? amount : body)))))));

	private PhoneContact SelectedContact => mod.ScreenProfile.Contacts.FirstOrDefault((PhoneContact c) => c.Number == number);

	private bool Dark => mod.Sounds.Preferences.Theme == "dark";

	private Color Ink => Dark ? new Color(0.93f, 0.95f, 0.98f, 1f) : new Color(0.06f, 0.1f, 0.17f, 1f);

	private Color Muted => Dark ? new Color(0.65f, 0.7f, 0.78f, 1f) : new Color(0.4f, 0.46f, 0.54f, 1f);

	private Color Card => (Color)(Dark ? new Color(0.1f, 0.14f, 0.2f, 1f) : Color.white);

	private Color Background => Dark ? new Color(0.025f, 0.04f, 0.065f, 1f) : new Color(0.95f, 0.97f, 0.99f, 1f);

	public void WindyPosted()
	{
		windyBody = (windyPhoto = "");
		if (page == "windyCompose")
		{
			Go("windy");
		}
		Refresh();
	}

	public void OpenShared(PhoneNotice n)
	{
		number = n.Number;
		name = n.Name;
		Go("contact");
	}

	private string S(string text)
	{
		return PhoneLanguage.Status(mod.Sounds.Preferences.Language, text);
	}

	private string T(string text)
	{
		return PhoneLanguage.Text(mod.Sounds.Preferences.Language, text);
	}

	public PhoneApp(PhoneMod mod)
	{
		this.mod = mod;
	}

	public void Bind(PhoneSurface screen)
	{
		surface = screen;
		dirty = true;
	}

	public void Refresh()
	{
		dirty = true;
	}

	public void VoiceSent()
	{
		Go("thread");
	}

	private void Go(string destination)
	{
		if (BankPage(page) && !BankPage(destination) && mod.Access.BankUnlocked)
		{
			bankClosing = true;
			mod.Request("bankLock", "", "", "", "", enabled: false, 0L);
		}
		if (BankPage(destination) && destination != "keyboard" && (!mod.Access.BankUnlocked || bankClosing))
		{
			destination = "bankGate";
		}
		if (destination == "bankGate" && page != "bankGate")
		{
			pin = (pinFirst = (pinError = ""));
			pinConfirm = false;
		}
		if (page == "voiceRecord" && destination != "voiceRecord")
		{
			mod.Notes.Clear();
		}
		page = destination;
		offset = 0;
		dirty = true;
	}

	private void Edit(string target, string back)
	{
		editField = target;
		returnPage = back;
		Go("keyboard");
	}

	private void ValueSet(string value)
	{
		if (editField == "dialNumber")
		{
			number = value;
		}
		else if (editField == "contactSearch")
		{
			contactSearch = value;
		}
		else if (editField == "number")
		{
			number = value;
		}
		else if (editField == "name")
		{
			name = value;
		}
		else if (editField == "liveText")
		{
			liveText = value;
		}
		else if (editField == "windyName")
		{
			windyName = value;
		}
		else if (editField == "windyBody")
		{
			windyBody = value;
		}
		else if (editField == "amount")
		{
			amount = value;
		}
		else
		{
			body = value;
		}
		dirty = true;
	}

	private void Add(string value)
	{
		int num = ((editField == "dialNumber") ? 11 : ((editField == "contactSearch") ? 32 : ((editField == "liveText") ? 140 : ((editField == "windyName") ? 20 : ((editField == "windyBody") ? 280 : ((editField == "amount") ? 12 : ((editField == "number") ? 10 : ((editField == "name") ? 32 : 320))))))));
		if (Value.Length + value.Length <= num)
		{
			ValueSet(Value + value);
		}
	}

	private void Row(string text, float y, Action action)
	{
		surface.Button(text, 0f, y, 540f, 75f, action);
	}

	public void Tick()
	{
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
		//IL_063f: Unknown result type (might be due to invalid IL or missing references)
		//IL_068a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0683: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0800: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f82: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fd1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1026: Unknown result type (might be due to invalid IL or missing references)
		//IL_107b: Unknown result type (might be due to invalid IL or missing references)
		//IL_10d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1125: Unknown result type (might be due to invalid IL or missing references)
		//IL_1174: Unknown result type (might be due to invalid IL or missing references)
		//IL_11c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1218: Unknown result type (might be due to invalid IL or missing references)
		string text = DateTime.Now.ToString("HH:mm");
		if (clock != text)
		{
			clock = text;
			dirty = true;
		}
		string text2 = mod.View.Call.Id + "/" + mod.View.Call.State;
		if (text2 != lastCall)
		{
			lastCall = text2;
			if (mod.View.Call.State != "Idle")
			{
				Go("call");
			}
			else if (page == "call")
			{
				Go("recentCalls");
			}
		}
		if (mod.View.Call.State == "Active")
		{
			if (timedCall != mod.View.Call.Id)
			{
				timedCall = mod.View.Call.Id;
				callBegan = Time.realtimeSinceStartup;
				callSecond = -1;
			}
			int num = (int)(Time.realtimeSinceStartup - callBegan);
			if (page == "call" && num != callSecond)
			{
				callSecond = num;
				dirty = true;
			}
		}
		else
		{
			timedCall = "";
			callSecond = -1;
		}
		mod.Media.PhotoPage = mod.ScreenUnlocked && page == "photo";
		mod.Media.PreviewPage = mod.ScreenUnlocked && page == "liveSetup";
		if ((page == "nearby" || page == "voiceRecord") && Time.realtimeSinceStartup >= nextMessagesUi)
		{
			nextMessagesUi = Time.realtimeSinceStartup + 0.5f;
			dirty = true;
		}
		TickExperienceUi();
		if (bankClosing && !mod.Access.BankUnlocked)
		{
			bankClosing = false;
		}
		if (page == "bankGate" && mod.Access.BankUnlocked && !bankClosing)
		{
			pin = (pinFirst = (pinError = ""));
			pinConfirm = false;
			Go("bank");
		}
		if (BankPage(page) && page != "bankGate" && !mod.Access.BankUnlocked)
		{
			Go("bankGate");
		}
		if (surface == null || !dirty)
		{
			return;
		}
		dirty = false;
		if (!mod.ScreenUnlocked)
		{
			surface.Clear(Background);
			surface.TextColor = Ink;
			DrawWallpaper();
			DrawLock();
			surface.Finish();
			return;
		}
		bool flag = page == "messageDetail" || page == "inbox" || page == "thread" || page == "voiceRecord" || page == "nearby" || page == "appearance" || page == "wallpaperPicker" || page == "dial" || page == "recentCalls" || page == "settings" || page == "language" || page == "contacts" || page == "contact" || page == "contactEdit" || page == "contactPhoto" || page == "compose" || page == "deleteContact" || page == "bank" || page == "bankTransfer" || page == "windy" || page == "windyCompose" || page == "windyRead" || page == "confirmBank" || page == "gallery" || page == "attach" || page == "liveList" || page == "liveSetup" || page == "live" || page == "transactions";
		surface.Clear((Color)((page == "call") ? new Color(0.025f, 0.04f, 0.065f, 1f) : Background));
		surface.TextColor = ((page == "call") ? Color.white : Ink);
		if (page == "home")
		{
			DrawWallpaper();
		}
		PhoneView screenProfile = mod.ScreenProfile;
		if (flag)
		{
			surface.Label(clock + "                     WindyPhone   •   4G", 0f, 455f, 530f, 40f, 21f, Ink);
		}
		if (!flag)
		{
			surface.Text((page == "home") ? ("WindyPhone • " + PhoneService.Display(screenProfile.Number)) : ("WindyPhone • " + T("Téléphone")), 455f, 45f, 23f);
		}
		if (page != "home" && page != "call" && !flag)
		{
			surface.Text(S(mod.Status), 370f, 75f, 22f);
		}
		if (flag)
		{
			surface.Label(Short(S(mod.Status), 80), 0f, -412f, 530f, 38f, 18f, Muted);
		}
		if (page != "keyboard")
		{
			surface.Button(T("Accueil"), 0f, -455f, 180f, 45f, delegate
			{
				Go("home");
			});
		}
		switch (page)
		{
		case "home":
			surface.Text(clock, 330f, 85f, 70f);
			surface.Text(PhoneLanguage.Date(mod.Sounds.Preferences.Language, DateTime.Now), 265f, 40f, 22f);
			surface.Tile("contacts", "Contacts", -180f, 155f, new Color(0.13f, 0.44f, 0.88f, 1f), delegate
			{
				Go("contacts");
			});
			surface.Tile("messages", "Messages", 0f, 155f, new Color(0.15f, 0.65f, 0.36f, 1f), delegate
			{
				Go("inbox");
			});
			surface.Tile("phone", T("Téléphone"), 180f, 155f, new Color(0.09f, 0.64f, 0.53f, 1f), delegate
			{
				if (mod.View.Call.State != "Idle")
				{
					Go("call");
				}
				else
				{
					number = "";
					Go("dial");
				}
			});
			surface.Tile("camera", T("Photo"), -180f, -55f, new Color(0.3f, 0.33f, 0.4f, 1f), delegate
			{
				Go("photo");
			});
			surface.Tile("gallery", T("Galerie"), 0f, -55f, new Color(0.9f, 0.57f, 0.12f, 1f), delegate
			{
				Go("gallery");
			});
			surface.Tile("bank", T("Banque"), 180f, -55f, new Color(0.44f, 0.29f, 0.82f, 1f), delegate
			{
				Go("bank");
			});
			surface.Tile("nearby", "AirDrop", -180f, -265f, new Color(0.12f, 0.56f, 0.89f, 1f), delegate
			{
				Go("nearby");
			});
			surface.Tile("windy", "Windy", 0f, -265f, new Color(0.1f, 0.58f, 0.83f, 1f), delegate
			{
				Go("windy");
				mod.Request("windySync", "", "", "", "", enabled: false, 0L);
			});
			surface.Tile("settings", T("Réglages"), 180f, -265f, new Color(0.35f, 0.4f, 0.49f, 1f), delegate
			{
				Go("settings");
			});
			surface.Text(S(mod.Status), -408f, 38f, 18f);
			break;
		case "photo":
			surface.Picture((Texture)(object)mod.Media.Camera.Preview, 0f, 80f, 520f, 390f);
			if ((Object)(object)mod.Media.Camera.Preview == (Object)null)
			{
				surface.Text(T("Préparation de la caméra…"), 80f, 100f, 26f);
			}
			Row(mod.Media.FrontCamera ? T("Caméra avant") : T("Caméra arrière"), -175f, delegate
			{
				mod.Media.FrontCamera = !mod.Media.FrontCamera;
				Refresh();
			});
			surface.Text((surface.HoldsLeft && !surface.HoldsRight) ? T("Appuyer sur Y de la main porteuse") : T("Appuyer sur B de la main porteuse"), -280f, 100f, 25f);
			break;
		case "gallery":
		case "attach":
			DrawGallery();
			break;
		case "viewer":
			surface.Picture((Texture)(object)mod.Media.OpenPhoto, 0f, 90f, 520f, 390f);
			if ((Object)(object)mod.Media.OpenPhoto == (Object)null)
			{
				surface.Text(T("Chargement…"), 90f, 100f, 26f);
			}
			Row(T("Envoyer cette photo"), -165f, delegate
			{
				attached = viewing;
				number = (body = "");
				Go("compose");
			});
			if (mod.Gallery.Contains(viewing))
			{
				Row(T("Supprimer de Galerie"), -270f, delegate
				{
					Go("deletePhoto");
				});
			}
			surface.Button(T("Retour"), 0f, -370f, 250f, 50f, delegate
			{
				Go(viewerBack);
			});
			break;
		case "deletePhoto":
			surface.Text(T("Supprimer cette photo de Galerie ?\nLes copies déjà envoyées restent chez leurs destinataires."), 130f, 230f, 28f);
			Row(T("Confirmer la suppression"), -75f, delegate
			{
				mod.Media.DeleteGalleryPhoto(viewing);
				Go("gallery");
			});
			Row(T("Annuler"), -185f, delegate
			{
				Go("viewer");
			});
			break;
		case "bankGate":
			DrawBankGate();
			break;
		case "bank":
			DrawBank();
			break;
		case "bankTransfer":
			DrawBankTransfer();
			break;
		case "transactions":
			DrawTransactions();
			break;
		case "confirmBank":
			DrawBankConfirm();
			break;
		case "nearby":
			DrawAirDrop();
			break;
		case "settings":
			DrawSettings();
			break;
		case "appearance":
			DrawAppearance();
			break;
		case "wallpaperPicker":
			DrawGallery();
			break;
		case "language":
			DrawLanguage();
			break;
		case "cameraSettings":
		{
			surface.Text(T("Visio : ") + mod.Sounds.Preferences.VideoFps + T(" images/s"), 265f, 60f, 27f);
			int[] array2 = new int[3] { 8, 15, 24 };
			for (int num4 = 0; num4 < array2.Length; num4++)
			{
				int fps = array2[num4];
				surface.Button(fps + T(" i/s"), (num4 - 1) * 180, 170f, 160f, 75f, delegate
				{
					Fps(video: true, fps);
				});
			}
			surface.Text(T("Aperçu photo : ") + mod.Sounds.Preferences.PhotoFps + T(" images/s"), 35f, 70f, 27f);
			int[] array3 = new int[3] { 15, 30, 45 };
			for (int num5 = 0; num5 < array3.Length; num5++)
			{
				int fps2 = array3[num5];
				surface.Button(fps2 + T(" i/s"), (num5 - 1) * 180, -70f, 160f, 75f, delegate
				{
					Fps(video: false, fps2);
				});
			}
			surface.Text(T("Cadence cible : le PC et le réseau déterminent la fluidité réelle."), -245f, 130f, 23f);
			break;
		}
		case "windy":
			DrawWindy();
			break;
		case "liveList":
			DrawLiveList();
			break;
		case "liveSetup":
			DrawLiveSetup();
			break;
		case "live":
			DrawLive();
			break;
		case "liveComment":
			DrawLiveComment();
			break;
		case "notifications":
			DrawNotifications();
			break;
		case "pinChange":
			DrawPinChange();
			break;
		case "windyCompose":
			DrawWindyCompose();
			break;
		case "windyRead":
			DrawWindyRead();
			break;
		case "tones":
		{
			string[] array = (callTone ? PhoneSounds.CallNames : PhoneSounds.MessageNames);
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				int choice = num2;
				int num3 = (callTone ? mod.Sounds.Preferences.CallTone : mod.Sounds.Preferences.MessageTone);
				Row(((num3 == num2) ? "✓ " : "") + T(array[num2]), 230 - num2 * 110, delegate
				{
					try
					{
						mod.Sounds.Select(callTone, choice);
						Refresh();
					}
					catch (Exception ex)
					{
						mod.LocalStatus(ex.Message);
					}
				});
			}
			break;
		}
		case "contacts":
			DrawContacts();
			break;
		case "contact":
			DrawContact();
			break;
		case "contactEdit":
			DrawContactEdit();
			break;
		case "contactPhoto":
			DrawGallery();
			break;
		case "deleteContact":
			DrawDeleteContact();
			break;
		case "compose":
			DrawCompose();
			break;
		case "inbox":
			DrawInbox();
			break;
		case "thread":
			DrawThread();
			break;
		case "messageDetail":
			DrawMessageDetail();
			break;
		case "voiceRecord":
			DrawVoiceRecorder();
			break;
		case "dial":
			DrawDialer();
			break;
		case "recentCalls":
			DrawRecentCalls();
			break;
		case "call":
			DrawCall();
			break;
		case "keyboard":
			Keyboard();
			break;
		}
		DrawBanner();
		surface.Finish();
	}

	private static string FrenchDate(DateTime date)
	{
		return FrenchDays[(int)date.DayOfWeek] + " " + date.Day.ToString(CultureInfo.InvariantCulture) + " " + FrenchMonths[date.Month - 1];
	}

	private void Fps(bool video, int fps)
	{
		try
		{
			mod.Sounds.SetFps(video, fps);
			Refresh();
		}
		catch (Exception ex)
		{
			mod.LocalStatus(ex.Message);
		}
	}

	private void Volume(float delta)
	{
		try
		{
			mod.Sounds.SetVolume(mod.Sounds.Preferences.RingVolume + delta);
			Refresh();
		}
		catch (Exception ex)
		{
			mod.LocalStatus(ex.Message);
		}
	}

	private void Pages(int count, int amount)
	{
		surface.Button("◀", -180f, -355f, 120f, 55f, delegate
		{
			offset = Math.Max(0, offset - amount);
			dirty = true;
		});
		surface.Button("▶", 180f, -355f, 120f, 55f, delegate
		{
			if (offset + amount < count)
			{
				offset += amount;
			}
			dirty = true;
		});
	}

	private void Keyboard()
	{
		surface.Text(Value, 205f, 235f, 25f);
		string[] array = ((!(editField == "number") && !(editField == "amount") && !(editField == "dialNumber")) ? ((!(mod.Sounds.Preferences.Language == "en")) ? new string[4] { "1234567890", "azertyuiop", "qsdfghjkl", "wxcvbnm._" } : new string[4] { "1234567890", "qwertyuiop", "asdfghjkl", "zxcvbnm._" }) : ((!(editField == "dialNumber")) ? new string[4] { "123", "456", "789", "0" } : new string[4] { "123", "456", "789", "#0" }));
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			float num = Math.Min(165f, 540f / (float)text.Length);
			for (int j = 0; j < text.Length; j++)
			{
				string text2 = text[j].ToString();
				if (capitals)
				{
					text2 = text2.ToUpperInvariant();
				}
				string key = text2;
				surface.Button(key, ((float)j - (float)(text.Length - 1) * 0.5f) * num, 30 - i * 82, num - 6f, 70f, delegate
				{
					Add(key);
				});
			}
		}
		surface.Button(T("Effacer"), -180f, -310f, 165f, 65f, delegate
		{
			if (Value.Length > 0)
			{
				ValueSet(Value.Substring(0, Value.Length - 1));
			}
		});
		surface.Button(T("Espace"), 0f, -310f, 165f, 65f, delegate
		{
			if (editField != "number" && editField != "amount")
			{
				Add(" ");
			}
		});
		surface.Button(T("Maj"), 180f, -310f, 165f, 65f, delegate
		{
			capitals = !capitals;
			dirty = true;
		});
		Row(T("Terminé"), -420f, delegate
		{
			Go(returnPage);
		});
	}

	private void DrawAirDrop()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		Header("AirDrop", T("Partager ton numéro à proximité"));
		surface.Panel(0f, 260f, 540f, 70f, Navy);
		surface.Label(PhoneService.Display(mod.ScreenProfile.Number), 0f, 260f, 510f, 55f, 29f, Color.white);
		(ulong, string, float)[] array = mod.NearbyPeople().Take(3).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			(ulong Id, string Name, float Distance) person = array[i];
			float x = ((float)i - (float)(array.Length - 1) * 0.5f) * 180f;
			surface.Tile("contacts", Short(person.Name, 15), x, 125f, Blue, delegate
			{
				mod.Request("share", "", "", person.Id.ToString(CultureInfo.InvariantCulture), "", enabled: false, 0L);
			}, Ink);
			surface.Label(person.Distance.ToString("F1", CultureInfo.InvariantCulture) + " m", x, 0f, 170f, 30f, 17f, Muted);
		}
		if (array.Length == 0)
		{
			surface.Label(T("Aucun joueur à moins de 3 m"), 0f, 125f, 510f, 90f, 25f, Muted);
		}
		surface.Label(T("Numéros reçus"), 0f, -50f, 510f, 40f, 22f, Ink, left: true);
		PhoneNotice[] array2 = mod.SharedNumbers.Where((PhoneNotice n) => n.Owner == mod.ScreenOwner).Skip(offset).Take(2)
			.ToArray();
		for (int num = 0; num < array2.Length; num++)
		{
			PhoneNotice offer = array2[num];
			float num2 = -120 - num * 110;
			surface.Button("", 0f, num2, 540f, 95f, delegate
			{
				OpenShared(offer);
			}, Card);
			surface.Label(offer.Name, -55f, num2 + 18f, 390f, 38f, 25f, Ink, left: true);
			surface.Label(PhoneService.Display(offer.Number), -55f, num2 - 22f, 390f, 32f, 21f, Muted, left: true);
			surface.Label("+", 230f, num2, 45f, 60f, 40f, Blue);
		}
		if (array2.Length == 0)
		{
			surface.Label(T("Aucun numéro reçu"), 0f, -130f, 510f, 75f, 24f, Muted);
		}
		surface.Button("◀", -230f, -335f, 80f, 65f, delegate
		{
			offset = Math.Max(0, offset - 2);
			Refresh();
		}, Navy);
		surface.Button("▶", 230f, -335f, 80f, 65f, delegate
		{
			if (offset + 2 < mod.SharedNumbers.Count((PhoneNotice n) => n.Owner == mod.ScreenOwner))
			{
				offset += 2;
			}
			Refresh();
		}, Navy);
		surface.Button(T("Partager à tous • 3 m"), 0f, -335f, 340f, 65f, delegate
		{
			mod.Request("share", "", "", "", "", enabled: false, 0L);
		}, Blue);
	}

	private void DrawWallpaper()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		string visibleWallpaper = mod.VisibleWallpaper;
		if (!(visibleWallpaper == ""))
		{
			Texture2D val = mod.Media.PictureFor(visibleWallpaper);
			if (!((Object)(object)val == (Object)null))
			{
				surface.Picture((Texture)(object)val, 0f, 0f, 600f, 1000f, crop: true);
				surface.Panel(0f, 0f, 600f, 1000f, new Color(0f, 0f, 0f, Dark ? 0.5f : 0.38f));
				surface.TextColor = Color.white;
			}
		}
	}

	private void DrawAppearance()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Apparence"), "WindyPhone");
		surface.Label(T("Thème"), 0f, 270f, 510f, 50f, 28f, Ink, left: true);
		surface.Button((Dark ? "" : "✓ ") + T("Clair"), -140f, 185f, 250f, 80f, delegate
		{
			Theme("light");
		}, Dark ? Navy : Blue);
		surface.Button((Dark ? "✓ " : "") + T("Sombre"), 140f, 185f, 250f, 80f, delegate
		{
			Theme("dark");
		}, Dark ? Blue : Navy);
		surface.Label(T("Fond d’écran"), 0f, 75f, 510f, 50f, 28f, Ink, left: true);
		surface.Button(T("Choisir dans la Galerie"), 0f, -5f, 530f, 75f, delegate
		{
			Go("wallpaperPicker");
		}, Blue);
		surface.Label(T("Même image sur le verrouillage et l’accueil."), 0f, -95f, 510f, 75f, 23f, Muted);
		if (mod.VisibleWallpaper != "")
		{
			surface.Button(T("Retirer le fond d’écran"), 0f, -205f, 530f, 65f, delegate
			{
				mod.Request("wallpaper", "", "", "", "", enabled: false, 0L);
			}, Navy);
		}
		surface.Button(T("Retour"), 0f, -330f, 380f, 60f, delegate
		{
			Go("settings");
		}, Navy);
	}

	private void Theme(string theme)
	{
		try
		{
			mod.Sounds.SetTheme(theme);
			Refresh();
		}
		catch (Exception ex)
		{
			mod.LocalStatus(S(ex.Message));
		}
	}

	private bool BankPage(string value)
	{
		int result;
		switch (value)
		{
		case "keyboard":
			result = ((returnPage == "bankTransfer") ? 1 : 0);
			break;
		default:
			result = 0;
			break;
		case "bank":
		case "bankTransfer":
		case "confirmBank":
		case "transactions":
		case "bankGate":
			result = 1;
			break;
		}
		return (byte)result != 0;
	}

	private string BankText(string fr, string en)
	{
		return (mod.Sounds.Preferences.Language == "en") ? en : fr;
	}

	private void DrawBankGate()
	{
		bool setup = !mod.Access.BankPinSet;
		surface.Text("LabBank", 370f, 65f, 38f);
		surface.Text(setup ? BankText(pinConfirm ? "Confirme ton code bancaire" : "Crée ton code bancaire à 4 chiffres", pinConfirm ? "Confirm your bank PIN" : "Create your 4-digit bank PIN") : BankText("Saisis ton code bancaire", "Enter your bank PIN"), 290f, 85f, 27f);
		if (setup && mod.ScreenOwner != PlayerIDManager.LocalPlatformID)
		{
			surface.Text(BankText("Le propriétaire doit créer son code bancaire.", "The owner must set up their bank PIN."), 80f, 150f, 27f);
			return;
		}
		PinKeys(delegate
		{
			if (!PhonePin.Valid(pin))
			{
				pinError = BankText("Saisis les quatre chiffres du code.", "Enter the four digits.");
				Refresh();
			}
			else if (setup && !pinConfirm)
			{
				pinFirst = pin;
				pin = "";
				pinConfirm = true;
				Refresh();
			}
			else if (setup && pin != pinFirst)
			{
				pinError = BankText("Les codes ne correspondent pas.", "PINs do not match.");
				pin = (pinFirst = "");
				pinConfirm = false;
				Refresh();
			}
			else
			{
				mod.Request(setup ? "bankPin" : "bankUnlock", "", "", pin, "", enabled: false, 0L);
				pin = (pinFirst = "");
				pinConfirm = false;
				Refresh();
			}
		});
	}

	private void ContactAvatar(PhoneContact contact, string title, float x, float y, float size, Color text)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = ((contact != null && contact.Photo != "") ? mod.Media.PictureFor(contact.Photo) : null);
		surface.Panel(x, y, size, size, new Color(0.18f, 0.39f, 0.64f, 1f));
		if ((Object)(object)val != (Object)null)
		{
			surface.Picture((Texture)(object)val, x, y, size - 4f, size - 4f, crop: true);
		}
		else
		{
			surface.Label(string.IsNullOrWhiteSpace(title) ? "?" : title.Trim().Substring(0, 1).ToUpperInvariant(), x, y, size, size, size * 0.48f, text);
		}
	}

	private void DrawContacts()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		Header("Contacts", mod.ScreenProfile.Contacts.Count + T(" contacts   •   Carnet personnel"));
		surface.Button((contactSearch == "") ? T("Rechercher") : (T("Recherche : ") + Short(contactSearch, 20)), -90f, 270f, 350f, 55f, delegate
		{
			Edit("contactSearch", "contacts");
		}, Navy);
		surface.Button(T("+ Ajouter"), 190f, 270f, 165f, 55f, delegate
		{
			number = (name = "");
			Go("contactEdit");
		}, Blue);
		PhoneContact[] array = mod.ScreenProfile.Contacts.Where((PhoneContact phoneContact) => contactSearch == "" || phoneContact.Name.Contains(contactSearch, StringComparison.OrdinalIgnoreCase) || phoneContact.Number.Contains(contactSearch.Replace(" ", ""))).OrderBy<PhoneContact, string>((PhoneContact phoneContact) => phoneContact.Name, StringComparer.OrdinalIgnoreCase).ToArray();
		offset = Math.Min(offset, (array.Length != 0) ? ((array.Length - 1) / 5 * 5) : 0);
		PhoneContact[] array2 = array.Skip(offset).Take(5).ToArray();
		for (int num = 0; num < array2.Length; num++)
		{
			PhoneContact c = array2[num];
			float num2 = 185 - num * 102;
			surface.Button("", 0f, num2, 540f, 92f, delegate
			{
				number = c.Number;
				name = c.Name;
				Go("contact");
			}, Card);
			ContactAvatar(c, c.Name, -215f, num2, 68f, Color.white);
			surface.Label(c.Name, 30f, num2 + 18f, 360f, 38f, 27f, Ink, left: true);
			surface.Label(PhoneService.Display(c.Number), 30f, num2 - 20f, 360f, 32f, 21f, Muted, left: true);
			surface.Label("›", 240f, num2, 35f, 50f, 35f, Blue);
		}
		if (array2.Length == 0)
		{
			surface.Label((contactSearch == "") ? T("Ton carnet est vide.\nAjoute ton premier contact.") : T("Aucun contact trouvé"), 0f, 30f, 520f, 140f, 27f, Muted);
		}
		Pages(array.Length, 5);
		surface.Label((array.Length == 0) ? T("0 contact") : (offset / 5 + 1 + " / " + (array.Length + 4) / 5), 0f, -355f, 150f, 45f, 21f, Muted);
	}

	private void DrawContact()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		PhoneContact c = SelectedContact;
		if (c == null)
		{
			DrawContactEdit();
			return;
		}
		Header(c.Name, PhoneService.Display(c.Number));
		ContactAvatar(c, c.Name, 0f, 200f, 145f, Color.white);
		surface.Tile("phone", T("Appeler"), -180f, -5f, ContactGreen, delegate
		{
			mod.Request("call", number, "", "", "", enabled: false, 0L);
		}, Ink);
		surface.Tile("messages", "SMS", 0f, -5f, Blue, delegate
		{
			body = (attached = "");
			Go("compose");
		}, Ink);
		surface.Tile("camera", T("Visio"), 180f, -5f, Navy, delegate
		{
			mod.Request("call", number, "", "", "", enabled: true, 0L);
		}, Ink);
		surface.Button((c.Photo == "") ? T("Ajouter une photo") : T("Changer la photo"), -65f, -170f, 395f, 60f, delegate
		{
			Go("contactPhoto");
		}, Navy);
		if (c.Photo != "")
		{
			surface.Button(T("Retirer"), 220f, -170f, 110f, 60f, delegate
			{
				mod.Request("contactPhoto", number, "", "", "", enabled: false, 0L);
			}, ContactRed);
		}
		surface.Button(T("Modifier le contact"), 0f, -250f, 530f, 65f, delegate
		{
			name = c.Name;
			Go("contactEdit");
		}, Blue);
		surface.Button(T("Supprimer le contact"), 0f, -335f, 530f, 60f, delegate
		{
			Go("deleteContact");
		}, ContactRed);
		surface.Button(T("Retour aux contacts"), 0f, -410f, 380f, 45f, delegate
		{
			Go("contacts");
		}, Navy);
	}

	private void DrawContactEdit()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		PhoneContact c = SelectedContact;
		Header((c == null) ? T("Nouveau contact") : T("Modifier le contact"), T("Nom et numéro de téléphone"));
		surface.Label(T("NOM"), -15f, 250f, 500f, 40f, 20f, Muted, left: true);
		surface.Button((name == "") ? T("Saisir le nom") : name, 0f, 190f, 530f, 80f, delegate
		{
			Edit("name", "contactEdit");
		}, Navy);
		surface.Label(T("NUMÉRO"), -15f, 110f, 500f, 40f, 20f, Muted, left: true);
		if (c == null)
		{
			surface.Button((number == "") ? T("Saisir le numéro") : PhoneService.Display(number), 0f, 50f, 530f, 80f, delegate
			{
				Edit("number", "contactEdit");
			}, Navy);
		}
		else
		{
			surface.Label(PhoneService.Display(number), 0f, 50f, 530f, 80f, 32f, Ink);
		}
		surface.Button(T("Enregistrer"), 0f, -90f, 530f, 75f, delegate
		{
			mod.Request("contact", number, name, "", "", enabled: false, 0L);
			Go("contact");
		}, ContactGreen);
		surface.Label(T("Tu pourras choisir une photo après\nl’enregistrement du contact."), 0f, -200f, 510f, 95f, 23f, Muted);
		surface.Button(T("Annuler"), 0f, -335f, 400f, 60f, delegate
		{
			Go((c == null) ? "contacts" : "contact");
		}, Navy);
	}

	private void DrawDeleteContact()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Supprimer le contact"), SelectedContact?.Name ?? PhoneService.Display(number));
		surface.Label(T("Retirer ce contact de ton carnet ?\nLes conversations resteront disponibles."), 0f, 135f, 510f, 170f, 28f, Ink);
		surface.Button(T("Supprimer"), 0f, -80f, 530f, 75f, delegate
		{
			mod.Request("deleteContact", number, "", "", "", enabled: false, 0L);
			Go("contacts");
		}, ContactRed);
		surface.Button(T("Annuler"), 0f, -195f, 530f, 75f, delegate
		{
			Go("contact");
		}, Navy);
	}

	private void DrawCompose()
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		PhoneContact c = SelectedContact;
		Header(T("Nouveau message"), c?.Name ?? "SMS / photo");
		ContactAvatar(c, c?.Name ?? number, -220f, 245f, 68f, Color.white);
		surface.Button((PhoneService.Display(number) == "") ? T("Choisir le destinataire") : PhoneService.Display(number), 60f, 245f, 400f, 65f, delegate
		{
			Edit("number", "compose");
		}, Navy);
		surface.Panel(0f, 45f, 540f, 270f, Card);
		surface.Label((body == "") ? T("Écris ton message…") : Short(body, 320), 0f, 45f, 500f, 235f, 25f, (body == "") ? Muted : Ink, left: true);
		surface.Button(T("Écrire / modifier"), 0f, -135f, 530f, 60f, delegate
		{
			Edit("body", "compose");
		}, Navy);
		surface.Button((attached == "") ? T("Joindre une photo") : T("Changer la photo jointe"), (!(attached == "")) ? (-55) : 0, -220f, (attached == "") ? 530 : 420, 60f, delegate
		{
			attachDestination = "compose";
			Go("attach");
		}, Navy);
		surface.MarkIcon("clip", (attached == "") ? (-225) : (-225), -220f, 32f);
		if (attached != "")
		{
			surface.Button(T("Retirer"), 225f, -220f, 100f, 60f, delegate
			{
				attached = "";
				Refresh();
			}, ContactRed);
		}
		surface.Button(mod.Media.UploadBusy ? T("Envoi de la photo…") : T("Envoyer le message"), 0f, -325f, 530f, 80f, delegate
		{
			try
			{
				if (body.Trim() == "" && attached == "")
				{
					throw new InvalidOperationException(T("Écrire un message ou joindre une photo."));
				}
				if (attached != "")
				{
					mod.Media.SendPhoto(attached, number, body);
				}
				else
				{
					mod.Request("sms", number, "", body, "", enabled: false, 0L);
				}
			}
			catch (Exception ex)
			{
				mod.LocalStatus(ex.Message);
			}
		}, Blue);
		surface.MarkIcon("send", -225f, -325f, 36f);
		surface.Button(T("Retour"), -140f, -410f, 250f, 45f, delegate
		{
			Go((c != null) ? "contact" : "inbox");
		}, Navy);
		surface.Button(T("Vocal"), 140f, -410f, 250f, 45f, delegate
		{
			Go("voiceRecord");
		}, Blue);
	}

	private void PhoneTabs(string selected)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		surface.Button(T("Clavier"), -180f, -370f, 170f, 55f, delegate
		{
			Go("dial");
		}, (selected == "dial") ? Blue : Navy);
		surface.Button(T("Récents"), 0f, -370f, 170f, 55f, delegate
		{
			Go("recentCalls");
		}, (selected == "recentCalls") ? Blue : Navy);
		surface.Button("Contacts", 180f, -370f, 170f, 55f, delegate
		{
			Go("contacts");
		}, Navy);
	}

	private void DrawDialer()
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Téléphone"), "WindyPhone   •   " + PhoneService.Display(mod.ScreenProfile.Number));
		surface.Label((number == "") ? T("Saisir le numéro") : PhoneDial.Display(number), -25f, 240f, 455f, 70f, 34f, Ink);
		surface.Button("⌫", 240f, 240f, 65f, 65f, delegate
		{
			if (number.Length > 0)
			{
				number = number.Substring(0, number.Length - 1);
			}
			Refresh();
		}, Navy);
		bool flag = number.StartsWith("#");
		surface.Label(flag ? T("Appel masqué") : T("Ajoute # avant le numéro pour appeler en masqué"), 0f, 190f, 530f, 38f, 19f, flag ? Blue : Muted);
		string[] array = new string[12]
		{
			"1", "2", "3", "4", "5", "6", "7", "8", "9", "✎",
			"0", "#"
		};
		string[] array2 = new string[12]
		{
			"",
			"ABC",
			"DEF",
			"GHI",
			"JKL",
			"MNO",
			"PQRS",
			"TUV",
			"WXYZ",
			T("Modifier"),
			"",
			T("Masqué")
		};
		for (int num = 0; num < array.Length; num++)
		{
			string key = array[num];
			float x = (num % 3 - 1) * 175;
			float num2 = 115 - num / 3 * 95;
			surface.Button("", x, num2, 145f, 85f, delegate
			{
				DialKey(key);
			}, (key == "#" && flag) ? Blue : Card);
			surface.Label(key, x, num2 + 9f, 135f, 47f, 35f, (key == "#" && flag) ? Color.white : Ink);
			surface.Label(array2[num], x, num2 - 27f, 135f, 25f, 15f, (key == "#" && flag) ? Color.white : Muted);
		}
		surface.Button(T("Appeler"), -85f, -280f, 350f, 70f, delegate
		{
			DialCall(video: false);
		}, ContactGreen);
		surface.MarkIcon("phone", -225f, -280f, 32f);
		surface.Button(T("Visio"), 195f, -280f, 145f, 70f, delegate
		{
			DialCall(video: true);
		}, Blue);
		PhoneTabs("dial");
	}

	private void DialKey(string key)
	{
		if (key == "#")
		{
			number = (number.StartsWith("#") ? number.Substring(1) : ("#" + number));
			Refresh();
			return;
		}
		if (key == "✎")
		{
			Edit("dialNumber", "dial");
			return;
		}
		if (number.TrimStart('#').Length < 10)
		{
			number += key;
		}
		Refresh();
	}

	private void DialCall(bool video)
	{
		try
		{
			PhoneDial.Parse(number, out var _);
			mod.Request("call", number, "", "", "", video, 0L);
		}
		catch (Exception ex)
		{
			mod.LocalStatus(S(ex.Message));
		}
	}

	private void DrawRecentCalls()
	{
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		List<PhoneCallEntry> all = mod.ScreenProfile.Calls;
		Header(T("Récents"), T("Historique des appels"));
		PhoneCallEntry[] array = all.Skip(offset).Take(4).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			PhoneCallEntry entry = array[i];
			float num = 210 - i * 120;
			bool privateIncoming = entry.Incoming && entry.Masked;
			PhoneContact phoneContact = (privateIncoming ? null : mod.ScreenProfile.Contacts.FirstOrDefault((PhoneContact c) => c.Number == entry.Number));
			string text = (privateIncoming ? T("Numéro masqué") : (phoneContact?.Name ?? PhoneService.Display(entry.Number)));
			bool flag = entry.Incoming && (entry.Outcome == "missed" || entry.Outcome == "declined" || entry.Outcome == "cancelled");
			surface.Button("", 0f, num, 540f, 110f, delegate
			{
				if (privateIncoming)
				{
					mod.LocalStatus(T("Impossible de rappeler un numéro masqué."));
				}
				else
				{
					number = ((entry.Masked && !entry.Incoming) ? ("#" + entry.Number) : entry.Number);
					Go("dial");
				}
			}, Card);
			ContactAvatar(phoneContact, text, -215f, num, 68f, Color.white);
			surface.Label(text, 30f, num + 24f, 365f, 38f, 26f, flag ? ContactRed : Ink, left: true);
			string text2 = (flag ? T("Manqué") : (entry.Incoming ? T("Entrant") : T("Sortant")));
			if (entry.Outcome == "declined")
			{
				text2 = T("Refusé");
			}
			else if (entry.Outcome == "cancelled" && !entry.Incoming)
			{
				text2 = T("Annulé");
			}
			if (entry.Masked && !entry.Incoming)
			{
				text2 = text2 + " • " + T("Masqué");
			}
			surface.Label(text2 + " • " + entry.Utc.ToLocalTime().ToString("dd/MM HH:mm"), 30f, num - 14f, 365f, 30f, 19f, Muted, left: true);
			if (entry.Outcome == "completed")
			{
				surface.Label(TimeSpan.FromSeconds(entry.Seconds).ToString("mm\\:ss"), 30f, num - 40f, 365f, 25f, 18f, Muted, left: true);
			}
		}
		if (array.Length == 0)
		{
			surface.Label(T("Aucun appel récent"), 0f, 80f, 510f, 100f, 28f, Muted);
		}
		surface.Button("◀", -220f, -285f, 80f, 45f, delegate
		{
			offset = Math.Max(0, offset - 4);
			Refresh();
		}, Navy);
		surface.Button("▶", 220f, -285f, 80f, 45f, delegate
		{
			if (offset + 4 < all.Count)
			{
				offset += 4;
			}
			Refresh();
		}, Navy);
		PhoneTabs("recentCalls");
	}

	public void PushNotification(string title, string text, Action click = null, ulong owner = 0uL)
	{
		Toast item = new Toast
		{
			Owner = owner,
			Title = title,
			Text = text,
			Click = (click ?? ((Action)delegate
			{
				Go((title == "Messages") ? "inbox" : ((title == "Banque") ? "bank" : "nearby"));
			})),
			Until = Time.realtimeSinceStartup + 6f
		};
		notifications.Insert(0, item);
		while (notifications.Count > 20)
		{
			notifications.RemoveAt(20);
		}
		banner = item;
		dirty = true;
	}

	public void OpenWindyNotification(bool live)
	{
		Go(live ? "liveList" : "windy");
		mod.Request(live ? "sync" : "windySync", "", "", "", "", enabled: false, 0L);
	}

	public void LockStatus(string value)
	{
		pinError = value;
		dirty = true;
	}

	public void ForgetPrivate()
	{
		selectedMessage = null;
		mod.Notes?.Clear();
		contactSearch = (pin = (pinFirst = (pinError = (number = (name = (body = (attached = (viewing = (windyName = (windyBody = (windyPhoto = (liveText = ""))))))))))));
		windySelected = null;
		amount = (editField = (returnPage = ""));
		pinConfirm = false;
		wasUnlocked = false;
		bankClosing = false;
		Go("home");
	}

	private void TickExperienceUi()
	{
		bool screenUnlocked = mod.ScreenUnlocked;
		if (screenUnlocked && !wasUnlocked)
		{
			pin = (pinFirst = (pinError = ""));
			pinConfirm = false;
			Go((mod.View.Call.State != "Idle") ? "call" : "home");
		}
		if (!screenUnlocked && wasUnlocked)
		{
			ForgetPrivate();
		}
		wasUnlocked = screenUnlocked;
		string text = mod.Live.Active?.Id ?? "";
		if (screenUnlocked && text != "" && (text != liveUiId || (mod.IsCasting && surface != null && !surface.IsHeld && page != "live")))
		{
			Go("live");
		}
		if (text == "" && liveUiId != "" && page == "live")
		{
			Go("liveList");
		}
		liveUiId = text;
		if (banner != null && Time.realtimeSinceStartup >= banner.Until)
		{
			banner = null;
			dirty = true;
		}
	}

	private static string Short(string value, int max)
	{
		return (value.Length > max) ? (value.Substring(0, max - 1) + "…") : value;
	}

	private void Header(string title, string subtitle)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		surface.Label(title, 0f, 380f, 530f, 65f, 40f, Ink, left: true);
		surface.Label(subtitle, 0f, 325f, 530f, 45f, 21f, Muted, left: true);
	}

	private void DrawBank()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		PhoneView screenProfile = mod.ScreenProfile;
		Header("LabBank", T("Mes comptes   /   Banque personnelle"));
		surface.Panel(0f, 180f, 540f, 225f, Navy);
		surface.Label(T("COMPTE COURANT"), 0f, 250f, 490f, 38f, 21f, new Color(0.63f, 0.75f, 0.94f, 1f), left: true);
		surface.Label(screenProfile.BankReady ? (screenProfile.Balance.ToString("N0", CultureInfo.InvariantCulture) + " $") : T("Indisponible"), 0f, 175f, 490f, 80f, 50f, Color.white, left: true);
		surface.Label("LABRP     •••• " + screenProfile.Number.Substring(Math.Max(0, screenProfile.Number.Length - 4)), 0f, 100f, 490f, 40f, 23f, Color.white, left: true);
		surface.Button(T("Faire un virement"), 0f, 5f, 540f, 75f, delegate
		{
			Go("bankTransfer");
		}, Blue);
		surface.Label(T("DERNIÈRES OPÉRATIONS"), 0f, -75f, 530f, 40f, 22f, Muted, left: true);
		PhoneBankEntry[] array = screenProfile.Transactions.Take(2).ToArray();
		if (array.Length == 0)
		{
			surface.Label(T("Aucun virement enregistré"), 0f, -165f, 510f, 65f, 25f, Muted);
		}
		for (int num = 0; num < array.Length; num++)
		{
			BankEntry(array[num], -150 - num * 90);
		}
		surface.Button(T("Tout l’historique"), -135f, -345f, 255f, 60f, delegate
		{
			Go("transactions");
		}, Navy);
		surface.Button(T("Actualiser"), 145f, -345f, 245f, 60f, delegate
		{
			mod.Request("sync", "", "", "", "", enabled: false, 0L);
		}, Blue);
	}

	private void DrawBankTransfer()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Virement"), T("Un transfert entre comptes LabRP"));
		surface.Panel(0f, 155f, 540f, 240f, Card);
		surface.Label(T("BÉNÉFICIAIRE"), 0f, 245f, 490f, 35f, 20f, Muted, left: true);
		surface.Button((PhoneService.Display(number) == "") ? T("Choisir un numéro") : PhoneService.Display(number), 0f, 180f, 490f, 70f, delegate
		{
			Edit("number", "bankTransfer");
		}, Navy);
		surface.Label(T("MONTANT"), 0f, 100f, 490f, 35f, 20f, Muted, left: true);
		surface.Button((amount == "") ? T("Saisir un montant") : (amount + " $"), 0f, 30f, 490f, 70f, delegate
		{
			Edit("amount", "bankTransfer");
		}, Navy);
		surface.Button(T("Continuer"), 0f, -115f, 540f, 75f, delegate
		{
			if (PhoneBankRules.Amount(amount, out var _))
			{
				Go("confirmBank");
			}
			else
			{
				mod.LocalStatus(T("Saisir un montant entier positif."));
			}
		}, Blue);
		surface.Label(T("Solde disponible : ") + mod.ScreenProfile.Balance.ToString("N0", CultureInfo.InvariantCulture) + " $", 0f, -230f, 520f, 65f, 25f, Muted);
		surface.Button(T("Retour aux comptes"), 0f, -345f, 400f, 60f, delegate
		{
			Go("bank");
		}, Navy);
	}

	private void BankEntry(PhoneBankEntry t, float y)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		surface.Panel(0f, y, 540f, 80f, Card);
		surface.Label(t.Label + "\n" + t.Utc.ToLocalTime().ToString("dd/MM HH:mm"), -70f, y, 355f, 70f, 22f, Ink, left: true);
		surface.Label(((t.Amount > 0) ? "+" : "") + t.Amount + " $", 195f, y, 135f, 65f, 24f, (Color)((t.Amount > 0) ? new Color(0.04f, 0.5f, 0.32f, 1f) : Ink));
	}

	private void DrawBankConfirm()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Confirmer le virement"), T("Vérifie le bénéficiaire et le montant"));
		surface.Panel(0f, 125f, 540f, 240f, Card);
		surface.Label(amount + " $", 0f, 175f, 500f, 80f, 48f, Ink);
		surface.Label(T("Vers ") + PhoneService.Display(number), 0f, 65f, 500f, 70f, 28f, Muted);
		surface.Button(T("Confirmer"), 0f, -100f, 530f, 75f, delegate
		{
			if (PhoneBankRules.Amount(amount, out var value))
			{
				mod.Request("transfer", number, "", "", "", enabled: false, value);
			}
			Go("bank");
		}, Blue);
		surface.Button(T("Modifier"), 0f, -235f, 420f, 65f, delegate
		{
			Go("bankTransfer");
		}, Navy);
	}

	private void DrawTransactions()
	{
		Header(T("Opérations"), T("Historique des virements"));
		PhoneBankEntry[] array = mod.ScreenProfile.Transactions.Skip(offset).Take(5).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			BankEntry(array[i], 235 - i * 105);
		}
		Pages(mod.ScreenProfile.Transactions.Count, 5);
	}

	private void DrawGallery()
	{
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		bool wallpaperPicker = page == "wallpaperPicker";
		bool contactPicker = page == "contactPhoto";
		bool attach = page == "attach";
		List<string> gallery = mod.Gallery;
		Header(wallpaperPicker ? T("Fond d’écran") : (contactPicker ? T("Photo du contact") : (attach ? T("Joindre une photo") : T("Galerie"))), gallery.Count + T(" photos   •   Photos de ce téléphone"));
		string[] array = gallery.Skip(offset).Take(9).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			string id = array[i];
			float x = (i % 3 - 1) * 180;
			float y = 225 - i / 3 * 170;
			surface.PhotoTile((Texture)(object)mod.Media.PictureFor(id), x, y, 170f, 128f, delegate
			{
				if (wallpaperPicker)
				{
					if (!mod.Access.Gallery.Contains(id))
					{
						mod.LocalStatus(T("Photo en cours de synchronisation. Réessayer dans quelques secondes."));
					}
					else
					{
						mod.Request("wallpaper", "", "", "", "", enabled: false, 0L, id);
						Go("appearance");
					}
				}
				else if (contactPicker)
				{
					if (!mod.Access.Gallery.Contains(id))
					{
						mod.LocalStatus(T("Photo en cours de synchronisation. Réessayer dans quelques secondes."));
					}
					else
					{
						mod.Request("contactPhoto", number, "", "", "", enabled: false, 0L, id);
						Go("contact");
					}
				}
				else if (attach)
				{
					if ((Object)(object)mod.Media.PictureFor(id) == (Object)null)
					{
						mod.LocalStatus(T("Attendre le chargement de la photo."));
					}
					else
					{
						if (attachDestination == "windyCompose")
						{
							windyPhoto = id;
						}
						else
						{
							attached = id;
						}
						Go(attachDestination);
					}
				}
				else
				{
					viewing = id;
					viewerBack = "gallery";
					mod.Media.ViewPhoto(id);
					Go("viewer");
				}
			});
		}
		if (gallery.Count == 0)
		{
			surface.Label(T("Ta Galerie est vide.\nPrends ta première photo dans Photo."), 0f, 40f, 510f, 140f, 27f, Muted);
		}
		Pages(gallery.Count, 9);
		if (wallpaperPicker)
		{
			surface.Button(T("Retour"), 0f, -410f, 360f, 45f, delegate
			{
				Go("appearance");
			}, Navy);
		}
		if (contactPicker)
		{
			surface.Button(T("Retour au contact"), 0f, -410f, 360f, 45f, delegate
			{
				Go("contact");
			}, Navy);
		}
	}

	private void DrawWindy()
	{
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		Header("Windy", T("Pour vous   /   @") + mod.Windy.Handle);
		if (mod.Windy.Handle == "")
		{
			surface.Label(T("Rejoins la conversation"), 0f, 195f, 500f, 100f, 30f, Ink);
			surface.Button((windyName == "") ? T("Choisir un pseudo") : ("@" + windyName), 0f, 60f, 520f, 75f, delegate
			{
				Edit("windyName", "windy");
			}, Navy);
			surface.Button(T("Créer mon compte"), 0f, -65f, 520f, 75f, delegate
			{
				mod.Request("windyJoin", "", windyName, "", "", enabled: false, 0L);
			}, Blue);
			return;
		}
		surface.Button(T("Publier"), -180f, 272f, 165f, 50f, delegate
		{
			Go("windyCompose");
		}, Blue);
		surface.Button(T("Lives • ") + mod.Live.Available.Count, 0f, 272f, 165f, 50f, (Action)delegate
		{
			Go("liveList");
		}, (Color?)new Color(0.84f, 0.12f, 0.31f, 1f));
		surface.Button(T("Actualiser"), 180f, 272f, 165f, 50f, delegate
		{
			mod.Request("windySync", "", "", "", "", enabled: false, 0L, "", mod.Windy.Offset);
		}, Navy);
		WindyPostView post = mod.Windy.Posts.FirstOrDefault();
		if (post != null)
		{
			surface.Panel(0f, -35f, 550f, 545f, Card);
			surface.Panel(-230f, 198f, 48f, 48f, Blue);
			surface.Label(post.Handle.Substring(0, 1).ToUpperInvariant(), -230f, 198f, 45f, 45f, 28f, Color.white);
			surface.Label("@" + post.Handle, -5f, 198f, 395f, 50f, 26f, Ink, left: true);
			surface.Label(Short(post.Text, (post.Photo != "") ? 110 : 280), 0f, (post.Photo != "") ? 120 : 40, 510f, (post.Photo != "") ? 100 : 280, 25f, Ink, left: true);
			if (post.Photo != "")
			{
				surface.PhotoTile((Texture)(object)mod.Media.PictureFor(post.Photo), 0f, -90f, 420f, 315f, delegate
				{
					windySelected = post;
					mod.Media.ViewPhoto(post.Photo);
					Go("windyRead");
				});
			}
			surface.Label(post.Utc.ToLocalTime().ToString("dd/MM  HH:mm"), 0f, -280f, 510f, 35f, 20f, Muted, left: true);
		}
		else
		{
			surface.Label(T("Le fil est calme.\nPublie le premier message !"), 0f, 40f, 510f, 160f, 28f, Muted);
		}
		surface.Button(T("Plus récent"), -140f, -365f, 250f, 55f, delegate
		{
			mod.Request("windySync", "", "", "", "", enabled: false, 0L, "", Math.Max(0, mod.Windy.Offset - 1));
		}, Navy);
		surface.Button(T("Plus ancien"), 140f, -365f, 250f, 55f, delegate
		{
			if (mod.Windy.Offset + 1 < mod.Windy.Total)
			{
				mod.Request("windySync", "", "", "", "", enabled: false, 0L, "", mod.Windy.Offset + 1);
			}
		}, Navy);
	}

	private void DrawWindyCompose()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Nouvelle publication"), "@" + mod.Windy.Handle);
		surface.Panel(0f, 150f, 540f, 240f, Card);
		surface.Label((windyBody == "") ? T("Quoi de neuf ?") : Short(windyBody, 200), 0f, 150f, 500f, 200f, 26f, Ink, left: true);
		surface.Button(T("Écrire le texte"), 0f, -30f, 530f, 65f, delegate
		{
			Edit("windyBody", "windyCompose");
		}, Navy);
		surface.Button((windyPhoto == "") ? T("Joindre une photo") : T("Changer la photo"), 0f, -125f, 530f, 65f, delegate
		{
			attachDestination = "windyCompose";
			Go("attach");
		}, Navy);
		surface.Button(mod.Media.UploadBusy ? T("Envoi…") : T("Publier"), 0f, -235f, 530f, 75f, delegate
		{
			try
			{
				if (windyBody.Trim() == "" && windyPhoto == "")
				{
					throw new InvalidOperationException(T("Écrire un message ou joindre une photo."));
				}
				if (windyPhoto != "")
				{
					mod.Media.SendWindyPhoto(windyPhoto, windyBody);
				}
				else
				{
					mod.Request("windyPost", "", "", windyBody, "", enabled: false, 0L);
				}
			}
			catch (Exception ex)
			{
				mod.LocalStatus(ex.Message);
			}
		}, Blue);
		if (windyPhoto != "")
		{
			surface.Button(T("Retirer la photo"), 0f, -345f, 350f, 50f, delegate
			{
				windyPhoto = "";
				Refresh();
			}, Navy);
		}
	}

	private void DrawWindyRead()
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		if (windySelected == null)
		{
			Go("windy");
			return;
		}
		Header("@" + windySelected.Handle, windySelected.Utc.ToLocalTime().ToString("dd/MM HH:mm"));
		surface.Label(windySelected.Text, 0f, (windySelected.Photo != "") ? 195 : 95, 520f, (windySelected.Photo != "") ? 180 : 370, 24f, Ink, left: true);
		if (windySelected.Photo != "")
		{
			surface.PhotoTile((Texture)(object)mod.Media.PictureFor(windySelected.Photo), 0f, -115f, 400f, 300f, delegate
			{
			});
		}
		surface.Button(T("Retour au fil"), 0f, -345f, 400f, 60f, delegate
		{
			Go("windy");
		}, Navy);
	}

	private void DrawLiveList()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		Header("Windy Live", T("Regarde, commente et partage le moment"));
		surface.Button(mod.IsCasting ? T("Ton live") : T("Passer en live"), 0f, 255f, 530f, 65f, (Action)delegate
		{
			Go(mod.IsCasting ? "live" : "liveSetup");
		}, (Color?)new Color(0.85f, 0.11f, 0.3f, 1f));
		for (int num = 0; num < mod.Live.Available.Count; num++)
		{
			WindyLiveSummary live = mod.Live.Available[num];
			surface.Button("LIVE   @" + live.Handle + "\n" + live.Viewers + T(" spectateurs   •   ") + live.Likes + " likes", 0f, 140 - num * 125, 530f, 105f, delegate
			{
				if (mod.Live.Casting && mod.Live.Active?.Id == live.Id)
				{
					Go("live");
				}
				else
				{
					mod.Request("liveJoin", "", "", "", live.Id, enabled: false, 0L);
				}
			}, Navy);
		}
		if (mod.Live.Available.Count == 0)
		{
			surface.Label(T("Aucun live en cours"), 0f, 65f, 500f, 100f, 30f, Muted);
		}
	}

	private void DrawLiveSetup()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		if (mod.IsCasting)
		{
			DrawLive();
			return;
		}
		Header(T("Ton live"), T("La caméra et le micro sont diffusés sur Windy"));
		surface.Picture((Texture)(object)mod.Media.Camera.Preview, 0f, 95f, 440f, 330f);
		surface.Button(mod.Media.FrontCamera ? T("Caméra avant") : T("Caméra arrière"), 0f, -140f, 520f, 65f, delegate
		{
			mod.Media.FrontCamera = !mod.Media.FrontCamera;
			Refresh();
		}, Navy);
		surface.Button(T("Commencer le live"), 0f, -245f, 520f, 75f, (Action)delegate
		{
			mod.Request("liveStart", "", "", "", "", mod.Media.FrontCamera, 0L);
		}, (Color?)new Color(0.85f, 0.11f, 0.3f, 1f));
		surface.Label(T("Tu peux poser le téléphone pendant le live."), 0f, -350f, 510f, 70f, 22f, Muted);
	}

	private void DrawLive()
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		WindyLiveSummary live = mod.Live.Active;
		if (live == null)
		{
			Go("liveList");
			return;
		}
		surface.Label("LIVE   @" + live.Handle, 0f, 385f, 520f, 60f, 32f, new Color(0.85f, 0.11f, 0.3f, 1f), left: true);
		surface.Label(live.Viewers + T(" spectateurs   •   ") + live.Likes + " likes", 0f, 330f, 520f, 40f, 23f, Muted, left: true);
		Texture val = (Texture)(mod.Live.Casting ? ((object)mod.Media.Camera.Live) : ((object)mod.Media.RemoteLive));
		surface.Panel(0f, 105f, 530f, 400f, Navy);
		surface.Picture(val, 0f, 105f, 520f, 390f);
		if ((Object)(object)val == (Object)null)
		{
			surface.Label(T("Connexion au direct…"), 0f, 105f, 490f, 90f, 28f, Color.white);
		}
		WindyLiveComment[] array = mod.Live.Comments.TakeLast(3).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			surface.Label("@" + array[i].Handle + "   " + Short(array[i].Text, 75), 0f, -140 - i * 63, 525f, 58f, 21f, Ink, left: true);
		}
		if (mod.Live.Casting)
		{
			surface.Button(live.Front ? T("Caméra arrière") : T("Caméra avant"), -140f, -355f, 260f, 65f, delegate
			{
				mod.Request("liveCamera", "", "", "", live.Id, !live.Front, 0L);
			}, Blue);
			surface.Button(T("Terminer"), 140f, -355f, 250f, 65f, (Action)delegate
			{
				mod.Request("liveStop", "", "", "", live.Id, enabled: false, 0L);
			}, (Color?)new Color(0.85f, 0.11f, 0.3f, 1f));
			return;
		}
		surface.Button(mod.Live.Liked ? T("Aimé") : T("J’aime"), -185f, -355f, 160f, 65f, (Action)delegate
		{
			mod.Request("liveLike", "", "", "", live.Id, enabled: false, 0L);
		}, (Color?)new Color(0.85f, 0.11f, 0.3f, 1f));
		surface.Button(T("Commenter"), 0f, -355f, 190f, 65f, delegate
		{
			liveText = "";
			Edit("liveText", "liveComment");
		}, Blue);
		surface.Button(T("Quitter"), 190f, -355f, 150f, 65f, delegate
		{
			mod.Request("liveLeave", "", "", "", live.Id, enabled: false, 0L);
		}, Navy);
	}

	private void DrawLiveComment()
	{
		surface.Text(liveText, 145f, 240f, 28f);
		Row(T("Envoyer le commentaire"), -60f, delegate
		{
			if (mod.Live.Active != null)
			{
				mod.Request("liveComment", "", "", call: mod.Live.Active.Id, text: liveText, enabled: false, amount: 0L);
			}
			liveText = "";
			Go("live");
		});
		Row(T("Modifier"), -175f, delegate
		{
			Edit("liveText", "liveComment");
		});
	}

	private void DrawNotifications()
	{
		surface.Text("Notifications", 300f, 70f, 35f);
		Toast[] array = notifications.Where((Toast toast) => toast.Owner == mod.ScreenOwner).Skip(offset).Take(4)
			.ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			Toast n = array[num];
			surface.Button(T(n.Title) + "\n" + Short((n.Title == "Banque") ? S(n.Text) : n.Text, 70), 0f, 180 - num * 130, 540f, 115f, delegate
			{
				n.Click?.Invoke();
			});
		}
		Pages(notifications.Count((Toast toast) => toast.Owner == mod.ScreenOwner), 4);
	}

	private void DrawBanner()
	{
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		if (page == "keyboard" || page == "pinChange")
		{
			return;
		}
		if (banner != null && banner.Owner == mod.ScreenOwner)
		{
			surface.Button(T(banner.Title) + "  •  " + Short((banner.Title == "Banque") ? S(banner.Text) : banner.Text, 60), 0f, 410f, 555f, 95f, (Action)delegate
			{
				Toast toast = banner;
				banner = null;
				toast?.Click?.Invoke();
				Refresh();
			}, (Color?)new Color(0.12f, 0.24f, 0.4f, 1f));
		}
		if (page == "home")
		{
			surface.Button("Notifications • " + notifications.Count((Toast n) => n.Owner == mod.ScreenOwner), 0f, -455f, 300f, 45f, delegate
			{
				Go("notifications");
			});
		}
	}

	private void DrawCall()
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0539: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_061b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0617: Unknown result type (might be due to invalid IL or missing references)
		//IL_066a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0666: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0715: Unknown result type (might be due to invalid IL or missing references)
		PhoneCallView call = mod.View.Call;
		PhoneContact phoneContact = ((call.Masked && call.Incoming) ? null : mod.ScreenProfile.Contacts.FirstOrDefault((PhoneContact c) => c.Number == call.Number));
		string text = ((call.Masked && call.Incoming) ? T("Numéro masqué") : (phoneContact?.Name ?? PhoneService.Display(call.Number)));
		Color white = Color.white;
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.08f, 0.69f, 0.38f, 1f);
		Color val2 = default(Color);
		((Color)(ref val2))._002Ector(0.89f, 0.16f, 0.23f, 1f);
		Color val3 = default(Color);
		((Color)(ref val3))._002Ector(0.16f, 0.21f, 0.29f, 1f);
		surface.Label(call.VideoRequested ? T("APPEL VIDÉO") : T("APPEL TÉLÉPHONIQUE"), 0f, 365f, 520f, 45f, 20f, new Color(0.64f, 0.73f, 0.85f, 1f));
		surface.Label(text, 0f, 300f, 520f, 70f, 38f, white);
		if (call.Masked && !call.Incoming)
		{
			surface.Label(T("Appel masqué"), 0f, 240f, 520f, 40f, 23f, Blue);
		}
		else if (phoneContact != null)
		{
			surface.Label(PhoneService.Display(call.Number), 0f, 240f, 520f, 40f, 23f, new Color(0.64f, 0.73f, 0.85f, 1f));
		}
		bool flag = call.State == "Active" && (call.Video || call.PeerVideo || call.VideoRequested);
		if (flag)
		{
			surface.Picture((Texture)(object)mod.Media.RemoteVideo, 0f, 75f, 470f, 280f);
			if ((Object)(object)mod.Media.RemoteVideo == (Object)null)
			{
				surface.Label(call.PeerVideo ? T("Connexion vidéo…") : T("Caméra du correspondant coupée"), 0f, 75f, 440f, 100f, 24f, white);
			}
		}
		else
		{
			ContactAvatar(phoneContact, text, 0f, 95f, 155f, white);
		}
		string text2 = ((call.State == "Active") ? TimeSpan.FromSeconds(Math.Max(0, callSecond)).ToString("mm\\:ss") : ((!(call.State == "Ringing")) ? T("Appel terminé") : (call.Incoming ? T("Appel entrant") : T("Appel en cours…"))));
		surface.Label(text2, 0f, flag ? (-100) : (-40), 520f, 45f, 25f, white);
		if (call.State == "Ringing")
		{
			if (call.Incoming)
			{
				surface.Tile("phone", T("Refuser"), -150f, -270f, val2, delegate
				{
					mod.Request("reject", "", "", "", call.Id, enabled: false, 0L);
				});
				surface.Tile("phone", call.VideoRequested ? T("Avec caméra") : T("Répondre"), 150f, -270f, val, delegate
				{
					mod.Request("answer", "", "", "", call.Id, call.VideoRequested, 0L);
				});
				if (call.VideoRequested)
				{
					surface.Button(T("Répondre en audio"), 0f, -135f, 430f, 60f, delegate
					{
						mod.Request("answer", "", "", "", call.Id, enabled: false, 0L);
					}, val3);
				}
			}
			else
			{
				surface.Tile("phone", T("Annuler"), 0f, -270f, val2, delegate
				{
					mod.Request("hangup", "", "", "", call.Id, enabled: false, 0L);
				});
			}
		}
		else if (call.State == "Active")
		{
			surface.Tile("microphone", mod.CallMicMuted ? T("Micro coupé") : T("Micro"), -180f, -195f, mod.CallMicMuted ? val : val3, mod.ToggleCallMicrophone);
			surface.Tile("speaker", T("Haut-parleur"), 0f, -195f, call.Speaker ? val : val3, delegate
			{
				mod.Request("speaker", "", "", "", call.Id, !call.Speaker, 0L);
			});
			surface.Tile("camera", T("Caméra"), 180f, -195f, call.Video ? val : val3, delegate
			{
				mod.Request("video", "", "", "", call.Id, !call.Video, 0L);
			});
			surface.Button(T("Raccrocher"), 0f, -380f, 290f, 80f, delegate
			{
				mod.Request("hangup", "", "", "", call.Id, enabled: false, 0L);
			}, val2);
			if (call.PeerSpeaker)
			{
				surface.Label(T("Correspondant sur haut-parleur"), 0f, -108f, 530f, 35f, 19f, new Color(0.64f, 0.73f, 0.85f, 1f));
			}
		}
	}

	private void PinKeys(Action complete)
	{
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		surface.Text(string.Join("  ", from i in Enumerable.Range(0, 4)
			select (i < pin.Length) ? "●" : "○"), 205f, 65f, 34f);
		string[] array = new string[4] { "123", "456", "789", "0" };
		for (int num = 0; num < array.Length; num++)
		{
			for (int num2 = 0; num2 < array[num].Length; num2++)
			{
				string key = array[num][num2].ToString();
				surface.Button(key, ((float)num2 - (float)(array[num].Length - 1) * 0.5f) * 155f, 100 - num * 100, 120f, 85f, delegate
				{
					if (pin.Length < 4)
					{
						pin += key;
						pinError = "";
						Refresh();
					}
				});
			}
		}
		surface.Button(T("Effacer"), -145f, -310f, 250f, 70f, delegate
		{
			if (pin.Length > 0)
			{
				pin = pin.Substring(0, pin.Length - 1);
			}
			Refresh();
		});
		surface.Button(T("Valider"), 145f, -310f, 250f, 70f, complete, Blue);
		if (pinError != "")
		{
			surface.Text(Short(pinError, 90), -405f, 95f, 22f);
		}
	}

	private void SubmitPin(bool setup)
	{
		if (!PhonePin.Valid(pin))
		{
			pinError = T("Saisis les quatre chiffres du code.");
			Refresh();
		}
		else if (setup && !pinConfirm)
		{
			pinFirst = pin;
			pin = "";
			pinConfirm = true;
			Refresh();
		}
		else if (setup && pin != pinFirst)
		{
			pinError = T("Les codes ne correspondent pas.");
			pin = (pinFirst = "");
			pinConfirm = false;
			Refresh();
		}
		else
		{
			mod.Request(setup ? "pin" : "unlock", "", "", pin, "", enabled: false, 0L);
			pin = (pinFirst = "");
			pinConfirm = false;
			Refresh();
		}
	}

	private void DrawLock()
	{
		surface.Text(clock, 385f, 110f, 75f);
		surface.Text(PhoneLanguage.Date(mod.Sounds.Preferences.Language, DateTime.Now), 310f, 50f, 24f);
		bool flag = mod.ScreenOwner == PlayerIDManager.LocalPlatformID;
		bool setup = !mod.ScreenPinSet;
		if (setup && !flag)
		{
			surface.Text(T("Téléphone verrouillé\nLe propriétaire doit créer son code."), 70f, 200f);
			return;
		}
		surface.Text((!setup) ? T("Saisis le code du téléphone") : (pinConfirm ? T("Confirme ton code") : T("Crée ton code à quatre chiffres")), 265f, 65f, 27f);
		PinKeys(delegate
		{
			SubmitPin(setup);
		});
		int num = notifications.Count((Toast n) => n.Owner == mod.ScreenOwner);
		if (num > 0)
		{
			surface.Text(num + T(" notification(s)"), -475f, 35f, 19f);
		}
	}

	private void DrawPinChange()
	{
		if (mod.ScreenOwner != PlayerIDManager.LocalPlatformID)
		{
			surface.Text(T("Le code est réservé au propriétaire."), 100f, 150f);
			return;
		}
		surface.Text(pinConfirm ? T("Confirme le nouveau code") : T("Nouveau code à quatre chiffres"), 300f, 75f, 29f);
		PinKeys(delegate
		{
			SubmitPin(setup: true);
		});
	}

	private string MessagePreview(PhoneSms message)
	{
		return (message.Voice != "") ? (T("Message vocal") + " • " + message.VoiceSeconds + " s") : ((message.Photo != "") ? (T("Photo") + ((message.Text != "") ? (" • " + message.Text) : "")) : message.Text);
	}

	private void DrawInbox()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		Header("Messages", T("Tes conversations"));
		surface.Button(T("Nouveau message"), 0f, 275f, 530f, 55f, delegate
		{
			number = (body = (attached = ""));
			Go("compose");
		}, Blue);
		PhoneView view = mod.ScreenProfile;
		string[] array = (from m in view.Messages
			orderby m.Utc descending
			select (m.From == view.Number) ? m.To : m.From).Distinct().ToArray();
		string[] array2 = array.Skip(offset).Take(5).ToArray();
		for (int num = 0; num < array2.Length; num++)
		{
			string n = array2[num];
			PhoneContact phoneContact = view.Contacts.FirstOrDefault((PhoneContact c) => c.Number == n);
			PhoneSms phoneSms = (from m in view.Messages
				where m.From == n || m.To == n
				orderby m.Utc descending
				select m).First();
			bool flag = view.Messages.Any((PhoneSms m) => m.From == n && !m.Read);
			float num2 = 185 - num * 98;
			surface.Button("", 0f, num2, 540f, 90f, delegate
			{
				number = n;
				Go("thread");
				mod.Request("read", n, "", "", "", enabled: false, 0L);
			}, Card);
			ContactAvatar(phoneContact, phoneContact?.Name ?? n, -218f, num2, 62f, Color.white);
			surface.Label((flag ? "● " : "") + (phoneContact?.Name ?? PhoneService.Display(n)), 10f, num2 + 22f, 350f, 34f, 24f, flag ? Blue : Ink, left: true);
			surface.Label(Short(MessagePreview(phoneSms), 42), 10f, num2 - 16f, 350f, 37f, 19f, Muted, left: true);
			surface.Label(phoneSms.Utc.ToLocalTime().ToString("HH:mm"), 225f, num2 + 24f, 80f, 30f, 16f, Muted);
		}
		if (array2.Length == 0)
		{
			surface.Label(T("Aucun message"), 0f, 70f, 510f, 100f, 28f, Muted);
		}
		Pages(array.Length, 5);
	}

	private void DrawThread()
	{
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0512: Unknown result type (might be due to invalid IL or missing references)
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		Header(SelectedContact?.Name ?? PhoneService.Display(number), T("Conversation"));
		PhoneSms[] array = (from m in mod.ScreenProfile.Messages
			where m.From == number || m.To == number
			orderby m.Utc descending
			select m).ToArray();
		PhoneSms[] array2 = array.Skip(offset).Take(2).Reverse()
			.ToArray();
		for (int num = 0; num < array2.Length; num++)
		{
			PhoneSms message = array2[num];
			bool flag = message.From == mod.ScreenProfile.Number;
			float x = (flag ? 35 : (-35));
			float num2 = 160 - num * 230;
			Color val = (flag ? Blue : Card);
			Color color = (flag ? Color.white : Ink);
			surface.Panel(x, num2, 475f, 210f, val);
			if (message.Photo != "")
			{
				surface.PhotoTile((Texture)(object)mod.Media.PictureFor(message.Photo), x, num2 + 22f, 220f, 125f, delegate
				{
					viewing = message.Photo;
					viewerBack = "thread";
					mod.Media.ViewPhoto(message.Photo);
					Go("viewer");
				});
				if (message.Text != "")
				{
					surface.Label(Short(message.Text, 100), x, num2 - 62f, 435f, 52f, 19f, color, left: true);
				}
			}
			else if (message.Voice != "")
			{
				string id = message.Voice;
				surface.Button(T((mod.Notes.DownloadingId == id) ? "Chargement…" : ((mod.Notes.PlayingId == id) ? "Arrêter" : "Écouter")) + "  •  " + T("Vocal") + "  •  " + message.VoiceSeconds + " s", x, num2 + 5f, 410f, 70f, delegate
				{
					try
					{
						if (mod.Notes.PlayingId == id || mod.Notes.DownloadingId == id)
						{
							mod.Notes.StopPlayback();
						}
						else
						{
							mod.Notes.Listen(id);
						}
						Refresh();
					}
					catch (Exception ex)
					{
						mod.LocalStatus(S(ex.Message));
					}
				}, flag ? Navy : Blue);
				DrawVoiceBars(x, num2 - 58f, color);
			}
			else
			{
				surface.Button("", x, num2 + 12f, 435f, 155f, delegate
				{
					selectedMessage = message;
					Go("messageDetail");
				}, val);
				surface.Label(Short(message.Text, 150), x, num2 + 12f, 425f, 145f, 23f, color, left: true);
			}
			surface.Label(message.Utc.ToLocalTime().ToString("HH:mm") + (flag ? ("  •  " + T(message.Read ? "Lu" : "Envoyé")) : ""), x, num2 - 87f, 425f, 28f, 15f, color, left: true);
		}
		if (array2.Length == 0)
		{
			surface.Label(T("Commence la conversation"), 0f, 100f, 510f, 100f, 27f, Muted);
		}
		surface.Button(T("Écrire"), -140f, -270f, 250f, 65f, delegate
		{
			attached = (body = "");
			Go("compose");
		}, Blue);
		surface.Button(T("Vocal"), 140f, -270f, 250f, 65f, delegate
		{
			Go("voiceRecord");
		}, Navy);
		Pages(array.Length, 2);
		surface.Button(T("Messages"), 0f, -355f, 180f, 55f, delegate
		{
			Go("inbox");
		}, Navy);
	}

	private void DrawMessageDetail()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		if (selectedMessage == null)
		{
			Go("thread");
			return;
		}
		Header(T("Message"), SelectedContact?.Name ?? PhoneService.Display(number));
		surface.Panel(0f, 55f, 540f, 460f, Card);
		surface.Label(selectedMessage.Text, 0f, 55f, 500f, 425f, 23f, Ink, left: true);
		surface.Button(T("Retour"), 0f, -300f, 380f, 60f, delegate
		{
			Go("thread");
		}, Navy);
	}

	private void DrawVoiceBars(float x, float y, Color color)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		int[] array = new int[14]
		{
			8, 17, 26, 13, 31, 22, 9, 27, 18, 12,
			22, 30, 13, 8
		};
		for (int i = 0; i < array.Length; i++)
		{
			surface.Panel(x + ((float)i - (float)(array.Length - 1) * 0.5f) * 19f, y, 7f, array[i], color);
		}
	}

	private void DrawVoiceRecorder()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Message vocal"), SelectedContact?.Name ?? PhoneService.Display(number));
		surface.Panel(0f, 135f, 530f, 200f, Card);
		surface.Label(mod.Notes.Recording ? T("Enregistrement…") : (mod.Notes.Draft ? T("Vocal prêt") : T("Enregistre ta voix")), 0f, 195f, 490f, 65f, 30f, Ink);
		surface.Label(TimeSpan.FromSeconds(mod.Notes.Elapsed).ToString("mm\\:ss") + " / 00:15", 0f, 100f, 490f, 65f, 40f, mod.Notes.Recording ? ContactRed : Ink);
		if (mod.Notes.Recording)
		{
			surface.Label(T("Niveau micro") + " : " + (int)(mod.Notes.InputLevel * 100f) + " %", 0f, 25f, 490f, 32f, 20f, Muted);
			surface.Label(T(mod.Notes.MicrophoneStatus), 0f, -100f, 510f, 30f, 18f, Muted);
		}
		if (mod.Notes.Recording)
		{
			surface.Button(T("Arrêter"), 0f, -40f, 530f, 75f, delegate
			{
				NoteAction(delegate
				{
					mod.Notes.Stop();
				});
			}, ContactRed);
		}
		else
		{
			surface.Button(mod.Notes.Draft ? T("Réenregistrer") : T("Enregistrer"), 0f, -40f, 530f, 75f, delegate
			{
				NoteAction(delegate
				{
					mod.Notes.Start();
				});
			}, ContactRed);
		}
		if (mod.Notes.Draft)
		{
			surface.Button(T("Réécouter"), -140f, -155f, 250f, 70f, delegate
			{
				NoteAction(delegate
				{
					mod.Notes.Preview();
				});
			}, Navy);
			surface.Button(mod.Notes.Sending ? T("Envoi…") : T("Envoyer"), 140f, -155f, 250f, 70f, delegate
			{
				NoteAction(delegate
				{
					mod.Notes.Send(number);
				});
			}, Blue);
		}
		surface.Label(T("Micro Fusion • 15 secondes maximum"), 0f, -265f, 510f, 50f, 20f, Muted);
		surface.Button(T("Annuler"), 0f, -350f, 380f, 60f, delegate
		{
			mod.Notes.Clear();
			Go("thread");
		}, Navy);
	}

	private void NoteAction(Action action)
	{
		try
		{
			action();
			Refresh();
		}
		catch (Exception ex)
		{
			mod.LocalStatus(S(ex.Message));
		}
	}

	private void DrawSettings()
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Réglages"), "WindyPhone");
		surface.Button(T("Appels : ") + T(PhoneSounds.CallNames[mod.Sounds.Preferences.CallTone]), 0f, 235f, 530f, 65f, delegate
		{
			callTone = true;
			Go("tones");
		}, Navy);
		surface.Button(T("Messages : ") + T(PhoneSounds.MessageNames[mod.Sounds.Preferences.MessageTone]), 0f, 150f, 530f, 65f, delegate
		{
			callTone = false;
			Go("tones");
		}, Navy);
		surface.Label(T("Volume des sonneries : ") + Math.Round(mod.Sounds.Preferences.RingVolume * 100f) + " %", 0f, 75f, 530f, 45f, 24f, Ink);
		surface.Button("− 10 %", -140f, 10f, 250f, 55f, delegate
		{
			Volume(-0.1f);
		}, Blue);
		surface.Button("+ 10 %", 140f, 10f, 250f, 55f, delegate
		{
			Volume(0.1f);
		}, Blue);
		surface.Button(T("Fluidité caméra / visio"), 0f, -75f, 530f, 65f, delegate
		{
			Go("cameraSettings");
		}, Navy);
		surface.Button(T("Langue") + " : " + ((mod.Sounds.Preferences.Language == "en") ? "English" : "Français"), 0f, -165f, 530f, 65f, delegate
		{
			Go("language");
		}, Blue);
		surface.Button(T("Changer le code"), -140f, -275f, 250f, 70f, delegate
		{
			pin = (pinFirst = (pinError = ""));
			pinConfirm = false;
			Go("pinChange");
		}, Navy);
		surface.Button(T("Verrouiller"), 140f, -275f, 250f, 70f, delegate
		{
			mod.LockPhone();
		}, Navy);
		surface.Button(T("Apparence"), 0f, -355f, 530f, 55f, delegate
		{
			Go("appearance");
		}, Blue);
	}

	private void DrawLanguage()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		Header(T("Langue"), "WindyPhone   •   Français / English");
		surface.Button(((mod.Sounds.Preferences.Language == "fr") ? "✓  " : "") + "Français", 0f, 190f, 530f, 90f, delegate
		{
			Language("fr");
		}, Blue);
		surface.Button(((mod.Sounds.Preferences.Language == "en") ? "✓  " : "") + "English", 0f, 60f, 530f, 90f, delegate
		{
			Language("en");
		}, Navy);
		surface.Button(T("Retour"), 0f, -200f, 400f, 65f, delegate
		{
			Go("settings");
		}, Navy);
	}

	private void Language(string language)
	{
		try
		{
			mod.Sounds.SetLanguage(language);
			Refresh();
		}
		catch (Exception ex)
		{
			mod.LocalStatus(ex.Message);
		}
	}
}
