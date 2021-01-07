using BootEngine.Utils.Exceptions;
using BootEngine.Utils.ProfilingTools;
using System;
using System.IO;

namespace BootEngine.AssetsManager
{
	public static class GeneralHelper
	{
		public static byte[] ReadFile(string path)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(GeneralHelper));
#endif
			if (File.Exists(path))
				return File.ReadAllBytes(path);

			throw new BootEngineException($"Unable to read file from {path}");
		}
	}
}
