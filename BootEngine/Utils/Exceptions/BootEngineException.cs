using System;
using System.Runtime.Serialization;

namespace Utils.Exceptions
{
	[Serializable]
	public class BootEngineException : Exception
	{
		public BootEngineException()
		{
		}

		public BootEngineException(string message) : base(message)
		{
		}

		public BootEngineException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected BootEngineException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
