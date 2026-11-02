using BoneLib;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace BonelabRP.Telephone;

public static class PhoneShutter
{
	private static float nextShot;

	private static ControllerRig menuRig;

	private static bool previousMenuDisabled;

	public static bool Owns(BaseController controller)
	{
		return PhoneMod.Instance?.OwnsPhotoController(controller) ?? false;
	}

	public static void Poll(PhoneSurface screen)
	{
		BaseController val = null;
		BaseController val2 = null;
		if (screen != null && screen.PlacementReady)
		{
			PhoneMod instance = PhoneMod.Instance;
			if (instance != null && instance.Media?.PhotoPage == true)
			{
				if (screen.HoldsLeft)
				{
					val = Player.LeftController;
				}
				if (screen.HoldsRight)
				{
					val2 = Player.RightController;
				}
			}
		}
		if ((Object)(object)val == (Object)null && (Object)(object)val2 == (Object)null)
		{
			RestoreMenu();
			return;
		}
		OpenControllerRig controllerRig = Player.ControllerRig;
		if ((Object)(object)menuRig != (Object)(object)controllerRig)
		{
			RestoreMenu();
			if ((Object)(object)controllerRig != (Object)null)
			{
				menuRig = (ControllerRig)(object)controllerRig;
				previousMenuDisabled = ((ControllerRig)controllerRig).IsMenuTapDisabled;
			}
		}
		if ((Object)(object)menuRig != (Object)null)
		{
			menuRig.IsMenuTapDisabled = true;
		}
		if ((Object)(object)val != (Object)null && val.GetBButtonDown())
		{
			Press(val);
		}
		if ((Object)(object)val2 != (Object)null && val2.GetBButtonDown())
		{
			Press(val2);
		}
	}

	public static void Press(BaseController controller)
	{
		if (Owns(controller) && !(Time.realtimeSinceStartup < nextShot))
		{
			nextShot = Time.realtimeSinceStartup + 0.3f;
			PhoneMod.Instance?.ShutterPhoto();
		}
	}

	public static void RestoreMenu()
	{
		ControllerRig val = menuRig;
		menuRig = null;
		if ((Object)(object)val != (Object)null)
		{
			val.IsMenuTapDisabled = previousMenuDisabled;
		}
	}
}
