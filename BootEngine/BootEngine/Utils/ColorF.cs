﻿using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace BootEngine.Utils
{
	public struct ColorF : IEquatable<ColorF>
	{
		private readonly Vector4 _channels;

		/// <summary>
		/// The red component.
		/// </summary>
		public float R => _channels.X;
		/// <summary>
		/// The green component.
		/// </summary>
		public float G => _channels.Y;
		/// <summary>
		/// The blue component.
		/// </summary>
		public float B => _channels.Z;
		/// <summary>
		/// The alpha component.
		/// </summary>
		public float A => _channels.W;

		/// <summary>
		/// Constructs a new ColorF from the given components.
		/// </summary>
		/// <param name="r">The red component.</param>
		/// <param name="g">The green component.</param>
		/// <param name="b">The blue component.</param>
		/// <param name="a">The alpha component.</param>
		public ColorF(float r, float g, float b, float a)
		{
			_channels = new Vector4(r, g, b, a);
		}

		public ColorF(float r, float g, float b)
		{
			_channels = new Vector4(r, g, b, 1);
		}

		public ColorF(string hex)
		{
			hex = hex.TrimStart('#');
			if (hex.Length == 6)
			{
				_channels = new Vector4(
				int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
				int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
				int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber),
				255) / 255f;
			}
			else
			{
				_channels = new Vector4(
				int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
				int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
				int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber),
				int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber)) / 255f;
			}
		}

		/// <summary>
		/// Constructs a new ColorF from the XYZW components of a vector.
		/// </summary>
		/// <param name="channels">The vector containing the color components.</param>
		public ColorF(Vector4 channels)
		{
			_channels = channels;
		}

		/// <summary>
		/// The total size, in bytes, of an ColorF value.
		/// </summary>
		public static readonly int SizeInBytes = 16;

		/// <summary>
		/// Red (1, 0, 0, 1)
		/// </summary>
		public static readonly ColorF Red = new ColorF(1, 0, 0, 1);
		/// <summary>
		/// Dark Red (0.6f, 0, 0, 1)
		/// </summary>
		public static readonly ColorF DarkRed = new ColorF(0.6f, 0, 0, 1);
		/// <summary>
		/// Active Red (0.64f, 0.11f, 0.11f, 1)
		/// </summary>
		public static readonly ColorF ActiveRed = new ColorF(0.64f, 0.11f, 0.11f, 1.00f);
		/// <summary>
		/// Hover Red (0.76f, 0.21f, 0.26f, 1)
		/// </summary>
		public static readonly ColorF HoverRed = new ColorF(0.76f, 0.21f, 0.26f, 1.00f);
		/// <summary>
		/// Green (0, 1, 0, 1)
		/// </summary>
		public static readonly ColorF Green = new ColorF(0, 1, 0, 1);
		/// <summary>
		/// Blue (0, 0, 1, 1)
		/// </summary>
		public static readonly ColorF Blue = new ColorF(0, 0, 1, 1);
		/// <summary>
		/// Yellow (1, 1, 0, 1)
		/// </summary>
		public static readonly ColorF Yellow = new ColorF(1, 1, 0, 1);
		/// <summary>
		/// Grey (0.25f, 0.25f, 0.25f, 1)
		/// </summary>
		public static readonly ColorF Grey = new ColorF(.25f, .25f, .25f, 1);
		/// <summary>
		/// Dark Grey (.13f, .13f, .13f, 1)
		/// </summary>
		public static readonly ColorF DarkGrey = new ColorF(.13f, .13f, .13f, 1);
		/// <summary>
		/// Background Grey (.18f, .18f, .18f)
		/// </summary>
		public static readonly ColorF BackgroundGrey = new ColorF(.18f, .18f, .18f, 1);
		/// <summary>
		/// Light Grey (.31f, .31f, .31f, 1)
		/// </summary>
		public static readonly ColorF LightGrey = new ColorF(.31f, .31f, .31f, 1);
		/// <summary>
		/// Lighter Grey (0.65f, 0.65f, 0.65f, 1)
		/// </summary>
		public static readonly ColorF LighterGrey = new ColorF(.65f, .65f, .65f, 1);
		/// <summary>
		/// Cyan (0, 1, 1, 1)
		/// </summary>
		public static readonly ColorF Cyan = new ColorF(0, 1, 1, 1);
		/// <summary>
		/// White (1, 1, 1, 1)
		/// </summary>
		public static readonly ColorF White = new ColorF(1, 1, 1, 1);
		/// <summary>
		/// Cornflower Blue (0.3921f, 0.5843f, 0.9294f, 1)
		/// </summary>
		public static readonly ColorF CornflowerBlue = new ColorF(0.3921f, 0.5843f, 0.9294f, 1);
		/// <summary>
		/// Clear (0, 0, 0, 0)
		/// </summary>
		public static readonly ColorF Clear = new ColorF(0, 0, 0, 0);
		/// <summary>
		/// Black (0, 0, 0, 1)
		/// </summary>
		public static readonly ColorF Black = new ColorF(0, 0, 0, 1);
		/// <summary>
		/// Pink (1, 0.45f, 0.75f, 1)
		/// </summary>
		public static readonly ColorF Pink = new ColorF(1f, 0.45f, 0.75f, 1);
		/// <summary>
		/// Orange (1, 0.36f, 0, 1)
		/// </summary>
		public static readonly ColorF Orange = new ColorF(1f, 0.36f, 0f, 1);

		/// <summary>
		/// Converts this ColorF into a Vector4.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector4 ToVector4() => _channels;

		/// <summary>
		/// Element-wise equality.
		/// </summary>
		/// <param name="other">The instance to compare to.</param>
		/// <returns>True if all elements are equal; false otherswise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ColorF other)
		{
			return _channels.Equals(other._channels);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			return obj is ColorF other && Equals(other);
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return (R, G, B, A).GetHashCode();
		}

		/// <summary>
		/// Returns a string representation of this color.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"R:{R.ToString()}, G:{G.ToString()}, B:{B.ToString()}, A:{A.ToString()}";
		}

		/// <summary>
		/// Element-wise equality.
		/// </summary>
		/// <param name="left">The first value.</param>
		/// <param name="right">The second value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ColorF left, ColorF right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Element-wise inequality.
		/// </summary>
		/// <param name="left">The first value.</param>
		/// <param name="right">The second value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ColorF left, ColorF right)
		{
			return !left.Equals(right);
		}

		public static implicit operator Vector4(ColorF c) => c.ToVector4();
		public static implicit operator ColorF(Vector4 vec) => new ColorF(vec);
		public static implicit operator ColorF(RgbaFloat rgba) => new ColorF(rgba.ToVector4());
		public static implicit operator RgbaFloat(ColorF c) => new RgbaFloat(c);
	}
}
