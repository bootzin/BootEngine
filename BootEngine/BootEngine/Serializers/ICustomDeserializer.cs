using BootEngine.ECS;
using System.Collections;

namespace BootEngine.Serializers
{
	public interface ICustomDeserializer
	{
		void Deserialize(Scene scene, IDictionary data);
	}
}
