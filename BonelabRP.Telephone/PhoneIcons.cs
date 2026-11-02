using System;
using UnityEngine;

namespace BonelabRP.Telephone;

public static class PhoneIcons
{
	private static bool Line(float x, float y, float ax, float ay, float bx, float by, float w = 2.8f)
	{
		float num = bx - ax;
		float num2 = by - ay;
		float num3 = Math.Max(0f, Math.Min(1f, ((x - ax) * num + (y - ay) * num2) / (num * num + num2 * num2)));
		return (x - ax - num3 * num) * (x - ax - num3 * num) + (y - ay - num3 * num2) * (y - ay - num3 * num2) < w * w;
	}

	public static Sprite Create(string key)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_10c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_10d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_10f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1079: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(64, 64, (TextureFormat)4, false);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				float num = j;
				float num2 = i;
				float num3 = (float)Math.Sqrt((num - 32f) * (num - 32f) + (num2 - 32f) * (num2 - 32f));
				bool flag = false;
				switch (key)
				{
				case "windy":
					flag = Line(num, num2, 8f, 39f, 44f, 39f) || Line(num, num2, 44f, 39f, 51f, 45f) || Line(num, num2, 51f, 45f, 48f, 53f) || Line(num, num2, 48f, 53f, 40f, 53f) || Line(num, num2, 8f, 27f, 51f, 27f) || Line(num, num2, 51f, 27f, 56f, 34f) || Line(num, num2, 56f, 34f, 51f, 39f) || Line(num, num2, 8f, 16f, 34f, 16f) || Line(num, num2, 34f, 16f, 41f, 9f) || Line(num, num2, 41f, 9f, 34f, 5f);
					break;
				case "camera":
					flag = Line(num, num2, 10f, 16f, 54f, 16f) || Line(num, num2, 54f, 16f, 54f, 45f) || Line(num, num2, 54f, 45f, 10f, 45f) || Line(num, num2, 10f, 45f, 10f, 16f) || Line(num, num2, 23f, 45f, 27f, 52f) || Line(num, num2, 27f, 52f, 39f, 52f) || Line(num, num2, 39f, 52f, 43f, 45f) || Math.Abs((float)Math.Sqrt((num - 32f) * (num - 32f) + (num2 - 30f) * (num2 - 30f)) - 10f) < 2.5f;
					break;
				case "gallery":
					flag = Line(num, num2, 15f, 10f, 49f, 10f) || Line(num, num2, 49f, 10f, 49f, 43f) || Line(num, num2, 49f, 43f, 39f, 53f) || Line(num, num2, 39f, 53f, 15f, 53f) || Line(num, num2, 15f, 53f, 15f, 10f) || Line(num, num2, 39f, 53f, 39f, 43f) || Line(num, num2, 39f, 43f, 49f, 43f) || Line(num, num2, 23f, 21f, 41f, 21f) || Line(num, num2, 23f, 31f, 41f, 31f);
					break;
				case "send":
					flag = Line(num, num2, 9f, 12f, 55f, 32f) || Line(num, num2, 55f, 32f, 9f, 52f) || Line(num, num2, 9f, 52f, 19f, 32f) || Line(num, num2, 19f, 32f, 9f, 12f) || Line(num, num2, 19f, 32f, 48f, 32f);
					break;
				case "clip":
					flag = Line(num, num2, 22f, 18f, 43f, 39f) || Line(num, num2, 43f, 39f, 43f, 47f) || Line(num, num2, 43f, 47f, 35f, 51f) || Line(num, num2, 35f, 51f, 12f, 28f) || Line(num, num2, 12f, 28f, 12f, 18f) || Line(num, num2, 12f, 18f, 22f, 8f) || Line(num, num2, 22f, 8f, 31f, 8f) || Line(num, num2, 31f, 8f, 51f, 28f) || Line(num, num2, 22f, 18f, 26f, 14f) || Line(num, num2, 26f, 14f, 46f, 34f);
					break;
				case "contacts":
					flag = (num - 32f) * (num - 32f) + (num2 - 43f) * (num2 - 43f) < 81f || (num2 > 12f && num2 < 29f && (num - 32f) * (num - 32f) + (num2 - 12f) * (num2 - 12f) < 300f);
					break;
				case "messages":
					flag = Line(num, num2, 12f, 45f, 52f, 45f) || Line(num, num2, 52f, 45f, 52f, 23f) || Line(num, num2, 52f, 23f, 27f, 23f) || Line(num, num2, 27f, 23f, 16f, 13f) || Line(num, num2, 16f, 13f, 16f, 23f) || Line(num, num2, 16f, 23f, 12f, 23f) || Line(num, num2, 12f, 23f, 12f, 45f) || (num2 > 31f && num2 < 36f && (Math.Abs(num - 23f) < 2f || Math.Abs(num - 32f) < 2f || Math.Abs(num - 41f) < 2f));
					break;
				case "microphone":
					flag = Line(num, num2, 26f, 44f, 26f, 28f) || Line(num, num2, 26f, 44f, 38f, 44f) || Line(num, num2, 38f, 44f, 38f, 28f) || Line(num, num2, 38f, 28f, 26f, 28f) || Line(num, num2, 19f, 31f, 19f, 22f) || Line(num, num2, 19f, 22f, 32f, 17f) || Line(num, num2, 32f, 17f, 45f, 22f) || Line(num, num2, 45f, 22f, 45f, 31f) || Line(num, num2, 32f, 17f, 32f, 9f) || Line(num, num2, 23f, 9f, 41f, 9f);
					break;
				case "speaker":
					flag = Line(num, num2, 12f, 23f, 23f, 23f) || Line(num, num2, 23f, 23f, 36f, 12f) || Line(num, num2, 36f, 12f, 36f, 52f) || Line(num, num2, 36f, 52f, 23f, 41f) || Line(num, num2, 23f, 41f, 12f, 41f) || Line(num, num2, 12f, 41f, 12f, 23f) || Line(num, num2, 43f, 23f, 49f, 32f) || Line(num, num2, 49f, 32f, 43f, 41f) || Line(num, num2, 48f, 15f, 58f, 32f) || Line(num, num2, 58f, 32f, 48f, 49f);
					break;
				case "phone":
					flag = Line(num, num2, 18f, 48f, 14f, 39f, 5f) || Line(num, num2, 14f, 39f, 24f, 24f, 5f) || Line(num, num2, 24f, 24f, 39f, 14f, 5f) || Line(num, num2, 39f, 14f, 48f, 18f, 5f) || Line(num, num2, 18f, 48f, 25f, 42f, 5f) || Line(num, num2, 48f, 18f, 42f, 25f, 5f);
					break;
				case "bank":
					flag = Line(num, num2, 10f, 41f, 32f, 54f) || Line(num, num2, 32f, 54f, 54f, 41f) || Line(num, num2, 12f, 39f, 52f, 39f) || Line(num, num2, 12f, 12f, 52f, 12f) || Line(num, num2, 18f, 18f, 18f, 33f) || Line(num, num2, 32f, 18f, 32f, 33f) || Line(num, num2, 46f, 18f, 46f, 33f);
					break;
				case "nearby":
					flag = num3 < 5f || (Math.Abs(num3 - 15f) < 2.5f && num2 > 24f) || (Math.Abs(num3 - 26f) < 2.5f && num2 > 24f) || Line(num, num2, 23f, 10f, 32f, 21f) || Line(num, num2, 32f, 21f, 41f, 10f);
					break;
				case "settings":
					flag = Math.Abs(num3 - 13f) < 3f || (num3 > 22f && num3 < 28f) || (num3 > 25f && num3 < 32f && Math.Abs(Math.Sin(Math.Atan2(num2 - 32f, num - 32f) * 4.0)) > 0.7);
					break;
				}
				val.SetPixel(j, i, new Color(1f, 1f, 1f, (float)(flag ? 1 : 0)));
			}
		}
		val.Apply(false, false);
		return Sprite.Create(val, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, new Vector4(0f, 0f, 0f, 0f));
	}
}
