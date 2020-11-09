using System;

namespace Utils
{
	public static class Util
	{
		public static float Deg2Rad(float angle)
		{
			return MathF.PI * angle / 180.0f;
		}
	}
}
