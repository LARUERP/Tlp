using System;

namespace BonelabRP.Telephone;

public sealed class ScreenSettings
{
	public float X { get; set; }

	public float Y { get; set; }

	public float Z { get; set; }

	public float RotationX { get; set; }

	public float RotationY { get; set; }

	public float RotationZ { get; set; }

	public float SizeMultiplier { get; set; } = 1f;

	public bool ReverseFace { get; set; }

	public void Validate()
	{
		if (!float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(Z) || !float.IsFinite(RotationX) || !float.IsFinite(RotationY) || !float.IsFinite(RotationZ) || !float.IsFinite(SizeMultiplier) || Math.Abs(X) > 5f || Math.Abs(Y) > 5f || Math.Abs(Z) > 5f || SizeMultiplier < 0.1f || SizeMultiplier > 5f)
		{
			throw new InvalidOperationException("Réglage écran invalide.");
		}
	}
}
