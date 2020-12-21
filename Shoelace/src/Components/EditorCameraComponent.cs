using System;
using System.Numerics;

namespace Shoelace.Components
{
	internal struct EditorCameraComponent
	{
		public float Distance;
		public Vector3 FocalPoint;
		public Vector2 LastMousePos { get; set; }
		public readonly float RotationSpeed => .8f;
		public readonly float ZoomSpeed
		{
			get
			{
				float distance = MathF.Max(Distance * 0.2f, 0.0f);
				return MathF.Min(distance * distance, 100.0f); // max speed = 100
			}
		}

	}
}
