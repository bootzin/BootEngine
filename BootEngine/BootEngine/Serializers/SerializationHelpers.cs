using System.Collections;
using System.Numerics;

namespace BootEngine.Serializers
{
	public static class SerializationHelpers
	{
		public static Vector2 AsVec2(IList obj)
		{
			return new Vector2(
				float.Parse(obj[0].ToString()),
				float.Parse(obj[1].ToString()));
		}

		public static Vector3 AsVec3(IList obj)
		{
			return new Vector3(
				float.Parse(obj[0].ToString()),
				float.Parse(obj[1].ToString()),
				float.Parse(obj[2].ToString()));
		}

		public static Vector4 AsVec4(IList obj)
		{
			return new Vector4(
				float.Parse(obj[0].ToString()),
				float.Parse(obj[1].ToString()),
				float.Parse(obj[2].ToString()),
				float.Parse(obj[3].ToString()));
		}
	}
}
