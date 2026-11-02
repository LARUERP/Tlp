using System;

namespace BonelabRP.Telephone;

public sealed class PhoneConfiguration
{
	public int Version { get; set; } = 1;

	public string Barcode { get; set; } = "CharaWhy.Phone.Spawnable.Phone";

	public bool AutoSpawn { get; set; } = true;

	public ScreenSettings Screen { get; set; } = new ScreenSettings();

	public void Validate()
	{
		if (Version != 1 || string.IsNullOrWhiteSpace(Barcode) || Barcode.Length > 256 || Screen == null)
		{
			throw new InvalidOperationException("Configuration téléphone invalide.");
		}
		Screen.Validate();
	}
}
