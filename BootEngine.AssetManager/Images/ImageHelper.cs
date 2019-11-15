using StbImageSharp;
using static BootEngine.AssetManager.GeneralHelper;

namespace BootEngine.AssetManager.Images
{
	public static class ImageHelper
	{
		public static ImageResult LoadImage(string imgPath)
		{
			StbImage.stbi_set_flip_vertically_on_load(1);
			return ImageResult.FromMemory(ReadFile(imgPath), ColorComponents.RedGreenBlueAlpha);
		}
	}
}
