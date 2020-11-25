using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BootEngine.Utils
{
	public static class Util
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rad2Deg(float angle) => 180f / MathF.PI * angle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 Rad2Deg(Vector3 vector3) => new Vector3(Rad2Deg(vector3.X), Rad2Deg(vector3.Y), Rad2Deg(vector3.Z));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Deg2Rad(float angle) => MathF.PI * angle / 180.0f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 Deg2Rad(Vector3 vector3) => new Vector3(Deg2Rad(vector3.X), Deg2Rad(vector3.Y), Deg2Rad(vector3.Z));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(float value, float min, float max)
		{
			return MathF.Max(MathF.Min(value, max), min);
		}
	}
}
