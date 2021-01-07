﻿using System.IO;

namespace Shoelace
{
	internal static class EditorConfig
	{
		public static string AssetDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "assets");
		internal static string InternalAssetDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "internalAssets");
	}
}
