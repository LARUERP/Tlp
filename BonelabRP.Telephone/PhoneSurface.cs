using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
using Il2CppTMPro;
using LabFusion.UI;
using LabFusion.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace BonelabRP.Telephone;

public sealed class PhoneSurface : IDisposable
{
	public readonly GameObject Body;

	private readonly GameObject root;

	private readonly GameObject canvasObject;

	private GameObject content;

	private LaserCursor cursor;

	private bool disposed;

	private readonly Action<string> warn;

	private Texture2D roundedTexture;

	private Sprite roundedSprite;

	private readonly float created = Time.realtimeSinceStartup;

	private bool cursorWarning;

	private readonly ScreenSettings settings;

	private bool wasHeld;

	private bool placed;

	private bool faceChosen;

	private bool firstPickup = true;

	private Vector3 fixedNormal;

	private Vector3 previousMin;

	private Vector3 previousMax;

	private Vector3 previousScale;

	private int stableSamples;

	private float nextPlacement;

	private float placementUntil;

	private Vector3 fitPosition;

	private Vector3 fitWorldScale;

	private Quaternion fitRotation;

	private readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

	public bool HoldsLeft => !disposed && (Object)(object)Body != (Object)null && Held(Player.LeftHand);

	public bool HoldsRight => !disposed && (Object)(object)Body != (Object)null && Held(Player.RightHand);

	public bool IsHeld => !disposed && (Object)(object)Body != (Object)null && (Held(Player.LeftHand) || Held(Player.RightHand));

	public bool PlacementReady => placed;

	public Transform CameraPose => root.transform;

	public string PlacementInfo { get; private set; } = "";

	public Color TextColor { get; set; } = Color.white;

	public PhoneSurface(GameObject body, ScreenSettings settings, Action<string> warn)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		Body = body;
		this.warn = warn;
		this.settings = settings;
		placementUntil = created + 3f;
		root = new GameObject("RP_PrivatePhoneScreen");
		root.transform.SetParent(body.transform, false);
		try
		{
			canvasObject = new GameObject("CANVAS");
			canvasObject.layer = 5;
			RectTransform val = canvasObject.AddComponent<RectTransform>();
			((Transform)val).SetParent(root.transform, false);
			val.sizeDelta = new Vector2(600f, 1000f);
			Canvas val2 = canvasObject.AddComponent<Canvas>();
			val2.renderMode = (RenderMode)2;
			canvasObject.AddComponent<GraphicRaycaster>();
			CreateRoundedSprite();
			canvasObject.SetActive(false);
			root.SetActive(false);
			LaserCursorUtilities.CreateLaserCursor((Action<LaserCursor>)delegate(LaserCursor ready)
			{
				//IL_005a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0070: Unknown result type (might be due to invalid IL or missing references)
				if (disposed || (Object)(object)Body == (Object)null)
				{
					Object.Destroy((Object)(object)((Component)ready).gameObject);
				}
				else
				{
					cursor = ready;
					((Component)cursor).transform.SetParent(root.transform, false);
					((Component)cursor).transform.localPosition = Vector3.zero;
					((Component)cursor).transform.localRotation = Quaternion.identity;
					UpdateCursorRegion();
					((Component)cursor).gameObject.SetActive(true);
				}
			});
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	private bool Place(ScreenSettings s)
	{
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_0476: Unknown result type (might be due to invalid IL or missing references)
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0522: Unknown result type (might be due to invalid IL or missing references)
		//IL_0524: Unknown result type (might be due to invalid IL or missing references)
		//IL_0534: Unknown result type (might be due to invalid IL or missing references)
		//IL_0539: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_0555: Unknown result type (might be due to invalid IL or missing references)
		//IL_056b: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_0574: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0590: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05df: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_060c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0611: Unknown result type (might be due to invalid IL or missing references)
		//IL_0622: Unknown result type (might be due to invalid IL or missing references)
		//IL_0627: Unknown result type (might be due to invalid IL or missing references)
		//IL_065c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0673: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 val2 = default(Vector3);
		((Vector3)(ref val2))._002Ector(float.MinValue, float.MinValue, float.MinValue);
		bool flag = false;
		int num = 0;
		Vector3 val4;
		Vector3 val5 = default(Vector3);
		foreach (Renderer componentsInChild in Body.GetComponentsInChildren<Renderer>())
		{
			if ((Object)(object)componentsInChild == (Object)null || ((Component)componentsInChild).transform.IsChildOf(root.transform))
			{
				continue;
			}
			MeshFilter component = ((Component)componentsInChild).GetComponent<MeshFilter>();
			Mesh val3 = ((component != null) ? component.sharedMesh : null);
			if ((Object)(object)val3 == (Object)null)
			{
				SkinnedMeshRenderer component2 = ((Component)componentsInChild).GetComponent<SkinnedMeshRenderer>();
				val3 = ((component2 != null) ? component2.sharedMesh : null);
			}
			if ((Object)(object)val3 == (Object)null)
			{
				continue;
			}
			Bounds bounds = val3.bounds;
			val4 = ((Bounds)(ref bounds)).size;
			if (!(((Vector3)(ref val4)).sqrMagnitude < 1E-06f))
			{
				num++;
				for (int i = 0; i < 8; i++)
				{
					((Vector3)(ref val5))._002Ector(((i & 1) == 0) ? ((Bounds)(ref bounds)).min.x : ((Bounds)(ref bounds)).max.x, ((i & 2) == 0) ? ((Bounds)(ref bounds)).min.y : ((Bounds)(ref bounds)).max.y, ((i & 4) == 0) ? ((Bounds)(ref bounds)).min.z : ((Bounds)(ref bounds)).max.z);
					Vector3 val6 = Body.transform.InverseTransformPoint(((Component)componentsInChild).transform.TransformPoint(val5));
					val = Vector3.Min(val, val6);
					val2 = Vector3.Max(val2, val6);
					flag = true;
				}
			}
		}
		if (!flag)
		{
			stableSamples = 0;
			return false;
		}
		Vector3 lossyScale = Body.transform.lossyScale;
		val4 = val - previousMin;
		if (((Vector3)(ref val4)).sqrMagnitude < 1E-08f)
		{
			val4 = val2 - previousMax;
			if (((Vector3)(ref val4)).sqrMagnitude < 1E-08f)
			{
				val4 = lossyScale - previousScale;
				if (((Vector3)(ref val4)).sqrMagnitude < 1E-08f)
				{
					stableSamples++;
					goto IL_0272;
				}
			}
		}
		stableSamples = 0;
		goto IL_0272;
		IL_0272:
		previousMin = val;
		previousMax = val2;
		previousScale = lossyScale;
		if (stableSamples < 2)
		{
			return false;
		}
		Vector3 val7 = val2 - val;
		Vector3 val8 = (val + val2) * 0.5f;
		int num2 = ((!(val7.x <= val7.y) || !(val7.x <= val7.z)) ? ((val7.y <= val7.z) ? 1 : 2) : 0);
		Vector3 val9 = (Vector3)(num2 switch
		{
			1 => Vector3.up, 
			0 => Vector3.right, 
			_ => Vector3.forward, 
		});
		Vector3 val10 = ((num2 != 1) ? ((val7.y >= ((num2 == 0) ? val7.z : val7.x)) ? Vector3.up : ((num2 == 0) ? Vector3.forward : Vector3.right)) : ((val7.z >= val7.x) ? Vector3.forward : Vector3.right));
		Transform head = Player.Head;
		if (!faceChosen && (Object)(object)head != (Object)null)
		{
			Vector3 val11 = Body.transform.TransformDirection(val9);
			Vector3 val12 = head.position - Body.transform.TransformPoint(val8);
			if (Vector3.Dot(val11, val12) < 0f)
			{
				val9 = -val9;
			}
		}
		if (!faceChosen)
		{
			if (s.ReverseFace)
			{
				val9 = -val9;
			}
			fixedNormal = val9;
			faceChosen = true;
		}
		else
		{
			val9 = fixedNormal;
		}
		float num3 = num2 switch
		{
			1 => val7.y, 
			0 => val7.x, 
			_ => val7.z, 
		};
		float num4 = Mathf.Max(1E-06f, Mathf.Min(num2 switch
		{
			1 => Mathf.Min(val7.x, val7.z), 
			0 => Mathf.Min(val7.y, val7.z), 
			_ => Mathf.Min(val7.x, val7.y), 
		} * 0.78f, num2 switch
		{
			1 => Mathf.Max(val7.x, val7.z), 
			0 => Mathf.Max(val7.y, val7.z), 
			_ => Mathf.Max(val7.x, val7.y), 
		} * 0.78f * 0.6f)) * s.SizeMultiplier;
		root.transform.localScale = Vector3.one;
		root.transform.localPosition = val8 + val9 * (num3 * 0.5f + 0.002f) + new Vector3(s.X, s.Y, s.Z);
		root.transform.localRotation = Quaternion.LookRotation(-val9, val10) * Quaternion.Euler(s.RotationX, s.RotationY, s.RotationZ);
		canvasObject.transform.localPosition = Vector3.zero;
		canvasObject.transform.localRotation = Quaternion.identity;
		canvasObject.transform.localScale = Vector3.one * (num4 / 600f);
		fitPosition = root.transform.localPosition;
		fitRotation = root.transform.localRotation;
		fitWorldScale = canvasObject.transform.lossyScale;
		PlacementInfo = $"maillages={num}, centre local={val8}, dimensions={val7}, largeur UI={num4:F4} m, face={val9}";
		UpdateCursorRegion();
		return true;
	}

	private void UpdateCursorRegion()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		if (!((Object)(object)cursor == (Object)null))
		{
			cursor.regions = Il2CppReferenceArray<CursorRegion>.op_Implicit((CursorRegion[])(object)new CursorRegion[1]
			{
				new CursorRegion
				{
					bounds = new Bounds(Vector3.zero, Vector3.Scale(new Vector3(600f, 1000f, 80f), canvasObject.transform.lossyScale)),
					center = canvasObject.transform
				}
			});
		}
	}

	private void CreateRoundedSprite()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		roundedTexture = new Texture2D(128, 128, (TextureFormat)4, false);
		((Object)roundedTexture).name = "RP_PhoneRoundedCorners";
		((Texture)roundedTexture).filterMode = (FilterMode)1;
		((Texture)roundedTexture).wrapMode = (TextureWrapMode)1;
		Color[] array = (Color[])(object)new Color[16384];
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float num = MathF.Max(MathF.Abs((float)j + 0.5f - 64f) - 36f, 0f);
				float num2 = MathF.Max(MathF.Abs((float)i + 0.5f - 64f) - 36f, 0f);
				float num3 = Math.Clamp(0.5f - (MathF.Sqrt(num * num + num2 * num2) - 28f), 0f, 1f);
				array[i * 128 + j] = new Color(1f, 1f, 1f, num3);
			}
		}
		roundedTexture.SetPixels(Il2CppStructArray<Color>.op_Implicit(array));
		roundedTexture.Apply(false, false);
		roundedSprite = Sprite.Create(roundedTexture, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, new Vector4(28f, 28f, 28f, 28f));
	}

	public void Clear(Color? background = null)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)content != (Object)null)
		{
			content.SetActive(false);
			Object.Destroy((Object)(object)content);
		}
		content = new GameObject("PAGE");
		content.layer = 5;
		((Transform)content.AddComponent<RectTransform>()).SetParent(canvasObject.transform, false);
		Image val = Element("Background", 0f, 0f, 600f, 1000f).AddComponent<Image>();
		val.sprite = roundedSprite;
		val.type = (Type)1;
		((Graphic)val).color = (Color)(((_003F?)background) ?? new Color(0.025f, 0.04f, 0.065f, 1f));
		((Graphic)val).raycastTarget = false;
	}

	private GameObject Element(string name, float x, float y, float width, float height)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.layer = 5;
		RectTransform val2 = val.AddComponent<RectTransform>();
		((Transform)val2).SetParent(content.transform, false);
		Vector2 val3 = default(Vector2);
		((Vector2)(ref val3))._002Ector(0.5f, 0.5f);
		val2.pivot = val3;
		Vector2 anchorMin = (val2.anchorMax = val3);
		val2.anchorMin = anchorMin;
		val2.anchoredPosition = new Vector2(x, y);
		val2.sizeDelta = new Vector2(width, height);
		return val;
	}

	public void Text(string text, float y, float height = 60f, float size = 30f)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		TextMeshProUGUI val = Element("Text", 0f, y, 550f, height).AddComponent<TextMeshProUGUI>();
		((TMP_Text)val).font = TMP_Settings.defaultFontAsset;
		((TMP_Text)val).text = text;
		((TMP_Text)val).fontSize = size;
		((Graphic)val).color = TextColor;
		((TMP_Text)val).richText = false;
		((TMP_Text)val).enableWordWrapping = true;
		((TMP_Text)val).alignment = (TextAlignmentOptions)514;
		((Graphic)val).raycastTarget = false;
	}

	public void Panel(float x, float y, float width, float height, Color color)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Image val = Element("Card", x, y, width, height).AddComponent<Image>();
		val.sprite = roundedSprite;
		val.type = (Type)1;
		((Graphic)val).color = color;
		((Graphic)val).raycastTarget = false;
	}

	public void Label(string text, float x, float y, float width, float height, float size, Color color, bool left = false)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		TextMeshProUGUI val = Element("Label", x, y, width, height).AddComponent<TextMeshProUGUI>();
		((TMP_Text)val).font = TMP_Settings.defaultFontAsset;
		((TMP_Text)val).text = text;
		((TMP_Text)val).fontSize = size;
		((Graphic)val).color = color;
		((TMP_Text)val).richText = false;
		((TMP_Text)val).enableWordWrapping = true;
		((TMP_Text)val).alignment = (TextAlignmentOptions)(left ? 513 : 514);
		((Graphic)val).raycastTarget = false;
	}

	public void PhotoTile(Texture texture, float x, float y, float width, float height, Action click)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		Button("", x, y, width, height, click, (Color?)new Color(0.16f, 0.19f, 0.24f, 1f));
		if ((Object)(object)texture != (Object)null)
		{
			Picture(texture, x, y, width - 4f, height - 4f);
		}
		else
		{
			Label("…", x, y, width, height, 28f, Color.white);
		}
	}

	public void Button(string text, float x, float y, float width, float height, Action click, Color? tint = null)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Expected O, but got Unknown
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Element("Button", x, y, width, height);
		Image val2 = val.AddComponent<Image>();
		val2.sprite = roundedSprite;
		val2.type = (Type)1;
		((Graphic)val2).color = (Color)(((_003F?)tint) ?? new Color(0.12f, 0.17f, 0.28f, 1f));
		((Graphic)val2).raycastTarget = false;
		Button val3 = val.AddComponent<Button>();
		((Selectable)val3).targetGraphic = (Graphic)(object)val2;
		UIMachineUtilities.AddClickEvent(val3, click);
		GameObject val4 = new GameObject("Label");
		val4.layer = 5;
		RectTransform val5 = val4.AddComponent<RectTransform>();
		((Transform)val5).SetParent(val.transform, false);
		val5.sizeDelta = new Vector2(width - 8f, height - 4f);
		TextMeshProUGUI val6 = val4.AddComponent<TextMeshProUGUI>();
		((TMP_Text)val6).font = TMP_Settings.defaultFontAsset;
		((TMP_Text)val6).text = text;
		((TMP_Text)val6).fontSize = ((height < 65f) ? 24 : 28);
		((Graphic)val6).color = Color.white;
		((TMP_Text)val6).richText = false;
		((TMP_Text)val6).alignment = (TextAlignmentOptions)514;
		((Graphic)val6).raycastTarget = false;
		GameObject val7 = new GameObject("HIT");
		val7.layer = 5;
		val7.transform.SetParent(val.transform, false);
		BoxCollider val8 = val7.AddComponent<BoxCollider>();
		val8.size = new Vector3(width, height, 12f);
		val8.center = new Vector3(0f, 0f, -6f);
		((Collider)val8).isTrigger = true;
		val7.AddComponent<FusionUITrigger>().button = val3;
	}

	public void Picture(Texture texture, float x, float y, float width, float height, bool crop = false)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)texture == (Object)null))
		{
			GameObject val = Element("PhotoFrame", x, y, width, height);
			Image val2 = val.AddComponent<Image>();
			val2.sprite = roundedSprite;
			val2.type = (Type)1;
			((Graphic)val2).raycastTarget = false;
			val.AddComponent<Mask>().showMaskGraphic = false;
			GameObject val3 = new GameObject("Pixels");
			val3.layer = 5;
			RectTransform val4 = val3.AddComponent<RectTransform>();
			((Transform)val4).SetParent(val.transform, false);
			val4.sizeDelta = new Vector2(width, height);
			RawImage val5 = val3.AddComponent<RawImage>();
			val5.texture = texture;
			((Graphic)val5).raycastTarget = false;
			if (crop && texture.width > 0 && texture.height > 0)
			{
				float num = (float)texture.width / (float)texture.height;
				float num2 = width / height;
				float num3 = ((num > num2) ? (num2 / num) : 1f);
				float num4 = ((num > num2) ? 1f : (num / num2));
				val5.uvRect = new Rect((1f - num3) * 0.5f, (1f - num4) * 0.5f, num3, num4);
			}
		}
	}

	public void MarkIcon(string key, float x, float y, float size)
	{
		if (!icons.TryGetValue(key, out var value))
		{
			value = PhoneIcons.Create(key);
			icons[key] = value;
		}
		Image val = Element("ButtonIcon", x, y, size, size).AddComponent<Image>();
		val.sprite = value;
		((Graphic)val).raycastTarget = false;
	}

	public void IconButton(string key, string caption, float y, Action click)
	{
		Button("     " + caption, 0f, y, 540f, 75f, click);
		if (!icons.TryGetValue(key, out var value))
		{
			value = PhoneIcons.Create(key);
			icons[key] = value;
		}
		Image val = Element("ButtonIcon", -205f, y, 40f, 40f).AddComponent<Image>();
		val.sprite = value;
		((Graphic)val).raycastTarget = false;
	}

	public void Tile(string key, string caption, float x, float y, Color color, Action click, Color? captionColor = null)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		Button("", x, y, 125f, 125f, click, color);
		if (!icons.TryGetValue(key, out var value))
		{
			value = PhoneIcons.Create(key);
			icons[key] = value;
		}
		Image val = Element("Icon", x, y, 72f, 72f).AddComponent<Image>();
		val.sprite = value;
		((Graphic)val).raycastTarget = false;
		TextMeshProUGUI val2 = Element("Caption", x, y - 90f, 175f, 48f).AddComponent<TextMeshProUGUI>();
		((TMP_Text)val2).font = TMP_Settings.defaultFontAsset;
		((TMP_Text)val2).text = caption;
		((TMP_Text)val2).fontSize = 23f;
		((Graphic)val2).color = (Color)(((_003F?)captionColor) ?? TextColor);
		((TMP_Text)val2).richText = false;
		((TMP_Text)val2).alignment = (TextAlignmentOptions)514;
		((Graphic)val2).raycastTarget = false;
	}

	public void Finish()
	{
		UIMachineUtilities.OverrideFonts(content.transform);
	}

	public void Tick()
	{
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		if (disposed || (Object)(object)Body == (Object)null)
		{
			return;
		}
		if ((Object)(object)cursor == (Object)null && !cursorWarning && Time.realtimeSinceStartup - created > 30f)
		{
			cursorWarning = true;
			warn("Laser téléphone absent : vérifier le pallet Fusion Content.");
		}
		bool flag = Held(Player.LeftHand) || Held(Player.RightHand);
		if (flag && !wasHeld && firstPickup)
		{
			placed = false;
			faceChosen = false;
			canvasObject.SetActive(false);
			firstPickup = false;
			stableSamples = 0;
			nextPlacement = 0f;
		}
		wasHeld = flag;
		bool flag2 = flag || (PhoneMod.Instance?.IsCasting ?? false);
		if (root.activeSelf != flag2)
		{
			root.SetActive(flag2);
		}
		if (Time.realtimeSinceStartup >= nextPlacement && !placed)
		{
			nextPlacement = Time.realtimeSinceStartup + 0.15f;
			if (Place(settings))
			{
				if (!canvasObject.activeSelf)
				{
					canvasObject.SetActive(true);
				}
				placed = true;
			}
			else if (!placed)
			{
				canvasObject.SetActive(false);
			}
		}
		if (placed)
		{
			root.transform.localPosition = fitPosition;
			root.transform.localRotation = fitRotation;
			root.transform.localScale = Vector3.one;
			canvasObject.transform.localPosition = Vector3.zero;
			canvasObject.transform.localRotation = Quaternion.identity;
			Vector3 lossyScale = root.transform.lossyScale;
			if (Mathf.Abs(lossyScale.x) > 1E-06f && Mathf.Abs(lossyScale.y) > 1E-06f && Mathf.Abs(lossyScale.z) > 1E-06f)
			{
				canvasObject.transform.localScale = new Vector3(fitWorldScale.x / Mathf.Abs(lossyScale.x), fitWorldScale.y / Mathf.Abs(lossyScale.y), fitWorldScale.z / Mathf.Abs(lossyScale.z));
				UpdateCursorRegion();
			}
		}
	}

	private bool Held(Hand hand)
	{
		GameObject val = ((hand != null) ? hand.m_CurrentAttachedGO : null);
		return (Object)(object)val != (Object)null && ((Object)(object)val == (Object)(object)Body || val.transform.IsChildOf(Body.transform) || Body.transform.IsChildOf(val.transform));
	}

	public void Dispose()
	{
		disposed = true;
		foreach (Sprite value in icons.Values)
		{
			Object.Destroy((Object)(object)value.texture);
			Object.Destroy((Object)(object)value);
		}
		icons.Clear();
		if ((Object)(object)cursor != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)cursor).gameObject);
		}
		if ((Object)(object)root != (Object)null)
		{
			Object.Destroy((Object)(object)root);
		}
		if ((Object)(object)roundedSprite != (Object)null)
		{
			Object.Destroy((Object)(object)roundedSprite);
		}
		if ((Object)(object)roundedTexture != (Object)null)
		{
			Object.Destroy((Object)(object)roundedTexture);
		}
	}
}
