namespace BonelabRP.Telephone;

public static class PhoneProximity
{
	public static bool Within(float ax, float ay, float az, float bx, float by, float bz, float radius)
	{
		if (!float.IsFinite(ax) || !float.IsFinite(ay) || !float.IsFinite(az) || !float.IsFinite(bx) || !float.IsFinite(by) || !float.IsFinite(bz) || !float.IsFinite(radius) || radius <= 0f)
		{
			return false;
		}
		double num = (double)ax - (double)bx;
		double num2 = (double)ay - (double)by;
		double num3 = (double)az - (double)bz;
		return num * num + num2 * num2 + num3 * num3 <= (double)radius * (double)radius;
	}
}
