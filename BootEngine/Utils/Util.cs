using System;
using StbImageSharp;
using Veldrid;
using Utils.Exceptions;
using System.IO;

namespace Utils
{
	public static class Util
	{
		public static double Deg2Rad(double angle)
		{
			return Math.PI * angle / 180.0;
		}

		/// <summary>
		/// Loads an image from a file. Supported formats include Bmp, Gif, Jpeg, Png and Tiff. 
		/// Remeber to call <see cref="Image.Dispose()"/> after usage.
		/// </summary>
		/// <param name="texturePath"></param>
		/// <returns></returns>
		public static ImageResult LoadImage(string imgPath)
		{
			StbImage.stbi_set_flip_vertically_on_load(1);
			return ImageResult.FromMemory(File.ReadAllBytes(imgPath), ColorComponents.RedGreenBlueAlpha);
		}

		public static Texture LoadTexture2D(GraphicsDevice gd, string texturePath, TextureUsage usage)
		{
			ImageResult texSrc = LoadImage(texturePath);
			TextureDescription texDesc = TextureDescription.Texture2D(
				(uint)texSrc.Width,
				(uint)texSrc.Height,
				1, // Miplevel
				1, // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				usage);
			Texture tex = gd.ResourceFactory.CreateTexture(texDesc);
			gd.UpdateTexture(
				tex,
				texSrc.Data,
				0, // x
				0, // y
				0, // z
				(uint)texSrc.Width,
				(uint)texSrc.Height,
				1,  // Depth
				0,  // Miplevel
				0); // ArrayLayers
			return tex;
		}
	}
}
