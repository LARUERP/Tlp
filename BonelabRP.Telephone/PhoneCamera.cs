using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;

namespace BonelabRP.Telephone;

public sealed class PhoneCamera : IDisposable
{
	private GameObject go;

	private Camera camera;

	private RenderTexture preview;

	private RenderTexture live;

	private RenderTexture photo;

	private Texture2D smallPixels;

	private Texture2D largePixels;

	private bool disposed;

	private readonly Action<string> warn;

	public bool Busy { get; private set; }

	public RenderTexture Preview => preview;

	public RenderTexture Live => live;

	public PhoneCamera(Action<string> warn)
	{
		this.warn = warn;
	}

	public bool Capture(PhoneSurface screen, bool large, bool front, bool encode, Action<byte[]> completed, bool photoPreview = false, bool allowUnheld = false)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Expected O, but got Unknown
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Expected O, but got Unknown
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Expected O, but got Unknown
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Expected O, but got Unknown
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		if (disposed || Busy || screen == null || (Object)(object)screen.Body == (Object)null || (!screen.IsHeld && !allowUnheld) || !screen.PlacementReady)
		{
			return false;
		}
		if ((Object)(object)camera == (Object)null)
		{
			go = new GameObject("RP_LocalPhoneCamera");
			camera = go.AddComponent<Camera>();
			((Behaviour)camera).enabled = false;
			camera.stereoTargetEye = (StereoTargetEyeMask)0;
			camera.fieldOfView = 70f;
			camera.nearClipPlane = 0.04f;
			camera.farClipPlane = 200f;
			camera.cullingMask = -33;
			camera.clearFlags = (CameraClearFlags)1;
			camera.allowHDR = false;
			camera.allowMSAA = false;
			preview = new RenderTexture(320, 240, 24, (RenderTextureFormat)0);
			preview.Create();
			live = new RenderTexture(320, 240, 24, (RenderTextureFormat)0);
			live.Create();
			smallPixels = new Texture2D(320, 240, (TextureFormat)3, false);
		}
		if (large && (Object)(object)photo == (Object)null)
		{
			photo = new RenderTexture(640, 480, 24, (RenderTextureFormat)0);
			photo.Create();
			largePixels = new Texture2D(640, 480, (TextureFormat)3, false);
		}
		Transform cameraPose = screen.CameraPose;
		Vector3 val = (front ? (-cameraPose.forward) : cameraPose.forward);
		((Component)camera).transform.position = cameraPose.position + val * 0.05f;
		((Component)camera).transform.rotation = Quaternion.LookRotation(val, cameraPose.up);
		camera.targetTexture = (large ? photo : (photoPreview ? preview : live));
		camera.aspect = 1.3333334f;
		Busy = true;
		((Behaviour)camera).enabled = true;
		try
		{
			MelonCoroutines.Start(ReadFrame(large, encode, completed, camera.targetTexture));
		}
		catch
		{
			((Behaviour)camera).enabled = false;
			Busy = false;
			throw;
		}
		return true;
	}

	private IEnumerator ReadFrame(bool large, bool encode, Action<byte[]> completed, RenderTexture target)
	{
		yield return (object)new WaitForEndOfFrame();
		if (disposed)
		{
			Busy = false;
			yield break;
		}
		try
		{
			((Behaviour)camera).enabled = false;
			if (!encode)
			{
				completed?.Invoke(null);
				yield break;
			}
			Texture2D pixels = (large ? largePixels : smallPixels);
			RenderTexture previous = RenderTexture.active;
			try
			{
				RenderTexture.active = target;
				pixels.ReadPixels(new Rect(0f, 0f, (float)(large ? 640 : 320), (float)(large ? 480 : 240)), 0, 0, false);
				pixels.Apply(false, false);
			}
			finally
			{
				RenderTexture.active = previous;
			}
			byte[] bytes = null;
			int cap = (large ? 65536 : 16384);
			int[] array = ((!large) ? new int[3] { 30, 20, 12 } : new int[3] { 70, 45, 25 });
			foreach (int quality in array)
			{
				bytes = ((IEnumerable<byte>)ImageConversion.EncodeToJPG(pixels, quality)).ToArray();
				if (bytes.Length <= cap)
				{
					break;
				}
			}
			if (bytes.Length > cap || !PhoneMediaRules.Jpeg(bytes, large ? 640 : 320, large ? 480 : 240))
			{
				throw new InvalidOperationException("Image trop volumineuse.");
			}
			completed?.Invoke(bytes);
		}
		catch (Exception ex)
		{
			Exception e = ex;
			warn("Caméra téléphone : " + e.Message);
			PhoneMod.Instance?.LocalStatus("Capture impossible : " + e.Message);
		}
		finally
		{
			if ((Object)(object)camera != (Object)null)
			{
				((Behaviour)camera).enabled = false;
			}
			Busy = false;
		}
	}

	public void Dispose()
	{
		disposed = true;
		if ((Object)(object)camera != (Object)null)
		{
			((Behaviour)camera).enabled = false;
		}
		if ((Object)(object)go != (Object)null)
		{
			Object.Destroy((Object)(object)go);
		}
		RenderTexture[] array = (RenderTexture[])(object)new RenderTexture[3] { preview, live, photo };
		foreach (RenderTexture val in array)
		{
			if ((Object)(object)val != (Object)null)
			{
				val.Release();
				Object.Destroy((Object)(object)val);
			}
		}
		if ((Object)(object)smallPixels != (Object)null)
		{
			Object.Destroy((Object)(object)smallPixels);
		}
		if ((Object)(object)largePixels != (Object)null)
		{
			Object.Destroy((Object)(object)largePixels);
		}
		camera = null;
		go = null;
		preview = (live = (photo = null));
		smallPixels = (largePixels = null);
	}
}
