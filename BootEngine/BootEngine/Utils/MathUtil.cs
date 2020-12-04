using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BootEngine.Utils
{
	public static class MathUtil
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float[] ToFloatArray(this Matrix4x4 mat)
		{
			return new float[]
			{
				mat.M11, mat.M12, mat.M13, mat.M14,
				mat.M21, mat.M22, mat.M23, mat.M24,
				mat.M31, mat.M32, mat.M33, mat.M34,
				mat.M41, mat.M42, mat.M43, mat.M44
			};
		}
	}
}
