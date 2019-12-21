using BootEngine.Utils.ProfilingTools;
using StbImageSharp;
using static BootEngine.AssetsManager.GeneralHelper;

namespace BootEngine.AssetsManager.Images
{
	public static class ImageHelper
	{
		public static ImageResult LoadImage(string imgPath)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ImageHelper));
#endif
			StbImage.stbi_set_flip_vertically_on_load(1);
			return ImageResult.FromMemory(ReadFile(imgPath), ColorComponents.RedGreenBlueAlpha);
		}
	}
}
