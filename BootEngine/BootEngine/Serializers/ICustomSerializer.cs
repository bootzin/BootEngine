using YamlDotNet.Core;

namespace BootEngine.Serializers
{
	public interface ICustomSerializer
	{
		void Serialize(Emitter e, YamlSerializer caller);
	}
}
