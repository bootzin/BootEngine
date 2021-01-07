using System.IO;

namespace Shoelace
{
	internal static class EditorConfig
	{
		public static string AssetDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "assets");
		public static string InternalAssetDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "internalAssets");
		public static string ProjectPath { get; set; } = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())); // TODO: Get proper path
	}
}
