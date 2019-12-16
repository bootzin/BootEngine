using System;
using System.IO;
using Utils.Exceptions;

namespace BootEngine.AssetsManager
{
	public static class GeneralHelper
	{
		private static readonly string ASSET_PATH = Path.Combine(AppContext.BaseDirectory, "assets");

		public static byte[] ReadFile(string path)
		{
			if (File.Exists(path))
			{
				return File.ReadAllBytes(path);
			}
			else
			{
				string fullPath = Path.Combine(ASSET_PATH, path);
				if (File.Exists(fullPath))
					return File.ReadAllBytes(fullPath);
			}

			throw new BootEngineException($"Unable to read file from {path}");
		}
	}
}
