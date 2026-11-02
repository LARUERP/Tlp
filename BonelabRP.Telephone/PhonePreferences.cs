using System;

namespace BonelabRP.Telephone;

public sealed class PhonePreferences
{
	public string Theme { get; set; } = "light";

	public string Language { get; set; } = "fr";

	public int VideoFps { get; set; } = 15;

	public int PhotoFps { get; set; } = 30;

	public float RingVolume { get; set; } = 1f;

	public int CallTone { get; set; }

	public int MessageTone { get; set; }

	public PhonePreferences Copy()
	{
		return new PhonePreferences
		{
			Theme = Theme,
			Language = Language,
			CallTone = CallTone,
			MessageTone = MessageTone,
			RingVolume = RingVolume,
			VideoFps = VideoFps,
			PhotoFps = PhotoFps
		};
	}

	public void Validate()
	{
		if ((Theme != "light" && Theme != "dark") || !PhoneLanguage.Valid(Language) || (VideoFps != 8 && VideoFps != 15 && VideoFps != 24) || (PhotoFps != 15 && PhotoFps != 30 && PhotoFps != 45) || !float.IsFinite(RingVolume) || RingVolume < 0f || RingVolume > 1f || CallTone < 0 || CallTone > 3 || MessageTone < 0 || MessageTone > 3)
		{
			throw new InvalidOperationException("Sonnerie invalide.");
		}
	}
}
