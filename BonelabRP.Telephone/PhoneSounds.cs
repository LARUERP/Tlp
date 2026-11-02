using System;
using System.IO;
using System.Text.Json;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneSounds : IDisposable
{
	public static readonly string[] CallNames = new string[4] { "Classique", "Cristal", "Orbital", "Silencieux" };

	public static readonly string[] MessageNames = new string[4] { "Note", "Duo", "Carillon", "Silencieux" };

	private readonly string file;

	private readonly AudioClip[] clips = (AudioClip[])(object)new AudioClip[8];

	private GameObject go;

	private AudioSource source;

	public PhonePreferences Preferences { get; private set; } = new PhonePreferences();

	public PhoneSounds(string file)
	{
		this.file = file;
		if (File.Exists(file))
		{
			try
			{
				Preferences = JsonSerializer.Deserialize<PhonePreferences>(File.ReadAllText(file)) ?? new PhonePreferences();
				Preferences.Validate();
			}
			catch (Exception)
			{
				Preferences = new PhonePreferences();
			}
		}
	}

	public void Select(bool call, int choice)
	{
		PhonePreferences phonePreferences = Preferences.Copy();
		if (call)
		{
			phonePreferences.CallTone = choice;
		}
		else
		{
			phonePreferences.MessageTone = choice;
		}
		phonePreferences.Validate();
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(phonePreferences));
		File.Move(file + ".tmp", file, overwrite: true);
		Preferences = phonePreferences;
		Play(call, choice);
	}

	public void SetVolume(float value)
	{
		PhonePreferences phonePreferences = Preferences.Copy();
		phonePreferences.RingVolume = Math.Max(0f, Math.Min(1f, value));
		phonePreferences.Validate();
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(phonePreferences));
		File.Move(file + ".tmp", file, overwrite: true);
		Preferences = phonePreferences;
		Message();
	}

	public void SetFps(bool video, int fps)
	{
		PhonePreferences phonePreferences = Preferences.Copy();
		if (video)
		{
			phonePreferences.VideoFps = fps;
		}
		else
		{
			phonePreferences.PhotoFps = fps;
		}
		phonePreferences.Validate();
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(phonePreferences));
		File.Move(file + ".tmp", file, overwrite: true);
		Preferences = phonePreferences;
	}

	public void SetTheme(string theme)
	{
		PhonePreferences phonePreferences = Preferences.Copy();
		phonePreferences.Theme = theme;
		phonePreferences.Validate();
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(phonePreferences));
		File.Move(file + ".tmp", file, overwrite: true);
		Preferences = phonePreferences;
	}

	public void SetLanguage(string language)
	{
		PhonePreferences phonePreferences = Preferences.Copy();
		phonePreferences.Language = language;
		phonePreferences.Validate();
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(phonePreferences));
		File.Move(file + ".tmp", file, overwrite: true);
		Preferences = phonePreferences;
	}

	public AudioClip Clip(bool call, int choice)
	{
		if (choice == 3)
		{
			return null;
		}
		int num = ((!call) ? 4 : 0) + choice;
		if ((Object)(object)clips[num] != (Object)null)
		{
			return clips[num];
		}
		float num2 = (call ? 2f : 0.65f);
		float[] array = new float[(int)(24000f * num2)];
		for (int i = 0; i < array.Length; i++)
		{
			float num3 = (float)i / 24000f;
			float num4 = ((!call) ? (choice switch
			{
				1 => 740, 
				0 => 880, 
				_ => 1040, 
			}) : (choice switch
			{
				1 => 660, 
				0 => 440, 
				_ => 520, 
			}));
			bool flag = ((!call) ? (num3 < 0.45f) : (num3 < 0.3f || (num3 > 0.45f && num3 < 0.75f)));
			if (choice == 1)
			{
				num4 += (float)((int)(num3 * 8f) % 2 * 220);
			}
			if (choice == 2)
			{
				num4 += (float)((int)(num3 * 10f) % 4 * 110);
			}
			float num5 = Math.Min(1f, num3 * 80f) * Math.Min(1f, Math.Max(0f, (num2 - num3) * 80f));
			array[i] = (flag ? (0.65f * num5 * (float)Math.Sin(Math.PI * 2.0 * (double)num4 * (double)num3)) : 0f);
		}
		clips[num] = AudioClip.Create("RP_Tone_" + num, array.Length, 1, 24000, false);
		clips[num].SetData(Il2CppStructArray<float>.op_Implicit(array), 0);
		return clips[num];
	}

	public void Message()
	{
		Play(call: false, Preferences.MessageTone);
	}

	public void Play(bool call, int choice)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		AudioClip val = Clip(call, choice);
		if (!((Object)(object)val == (Object)null))
		{
			if ((Object)(object)source == (Object)null)
			{
				go = new GameObject("RP_LocalPhoneSounds");
				source = go.AddComponent<AudioSource>();
				source.spatialBlend = 0f;
				source.playOnAwake = false;
			}
			source.PlayOneShot(val, Preferences.RingVolume);
		}
	}

	public void Dispose()
	{
		if ((Object)(object)go != (Object)null)
		{
			Object.Destroy((Object)(object)go);
		}
		source = null;
		go = null;
		AudioClip[] array = clips;
		foreach (AudioClip val in array)
		{
			if ((Object)(object)val != (Object)null)
			{
				Object.Destroy((Object)(object)val);
			}
		}
		Array.Clear(clips, 0, clips.Length);
	}
}
