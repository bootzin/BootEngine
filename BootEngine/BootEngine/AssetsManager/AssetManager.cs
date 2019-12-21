using BootEngine.AssetsManager.Images;
using BootEngine.AssetsManager.Shaders;
using BootEngine.Utils.ProfilingTools;
using StbImageSharp;
using System;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace BootEngine.AssetsManager
{
	public static class AssetManager
	{
		#region Properties
		private readonly static GraphicsDevice gd = Application.App.Window.GraphicsDevice;
		#endregion

		#region Textures
		/// <summary>
		/// Loads and updates a single 2D <see cref="Texture"/>
		/// </summary>
		/// <param name="texturePath">Path to the texture.</param>
		/// <param name="usage">A collection of flags determining the <see cref="TextureUsage"/></param>
		/// <returns>The update <see cref="Texture"/></returns>
		public static Texture LoadTexture2D(string texturePath, TextureUsage usage)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(AssetManager));
#endif
			ImageResult texSrc = ImageHelper.LoadImage(texturePath);
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
		#endregion

		#region Shaders
		/// <summary>
		/// Loads both vertex and fragment shaders from a single custom file
		/// </summary>
		/// <param name="path">The path to the file in which the shaders are.</param>
		/// <returns>An array containing both the vertex and the fragment shader</returns>
		public static Shader[] LoadShaders(string path)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(AssetManager));
#endif
			(string vs, string fs) = ShaderHelper.LoadShaders(path);
			Shader[] shaders = new Shader[2];
			shaders[0] = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vs), "main"));
			shaders[0].Name = ExtractNameFromPath(path) + "-Vertex";
			shaders[1] = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fs), "main"));
			shaders[1].Name = ExtractNameFromPath(path) + "-Fragment";
			ResourceCache.AddShaders(shaders);
			return shaders;
		}

		/// <summary>
		/// Loads every shader specified in <paramref name="pathStageList"/>. 
		/// Does not generate equivalents for other backends.
		/// </summary>
		/// <param name="pathStageList">Array of path-stage tuple arguments for the shader</param>
		/// <returns>An array with the created shaders.</returns>
		public static Shader[] LoadShaders(params (string Path, ShaderStages Stage)[] pathStageList)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(AssetManager));
#endif
			Shader[] shaders = new Shader[pathStageList.Length];
			for (int i = 0; i < pathStageList.Length; i++)
			{
				shaders[i] = gd.ResourceFactory.CreateShader(new ShaderDescription(pathStageList[i].Stage, ShaderHelper.LoadShader(pathStageList[i].Path), "main"));
				shaders[i].Name = $"{ExtractNameFromPath(pathStageList[i].Path)}-{pathStageList[i].Stage}";
			}
			ResourceCache.AddShaders(shaders);
			return shaders;
		}

		/// <summary>
		/// Loads both the vertex and the fragment shader and generates necessary 
		/// files for other backends using Veldrid.SPIRV
		/// </summary>
		/// <param name="path">The path to the file in which the shaders are.</param>
		/// <returns>An array containing both the vertex and the fragment shader compiled.</returns>
		public static Shader[] GenerateShadersFromFile(string path)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(AssetManager));
#endif
			(string vs, string fs) = ShaderHelper.LoadShaders(path);
			return GenerateShaders(ExtractNameFromPath(path), vs, fs);
		}

		/// <summary>
		/// Loads both the vertex and the fragment shader and generates necessary 
		/// files for other backends using Veldrid.SPIRV
		/// </summary>
		/// <param name="setName">The name of the shader set. Should be unique.</param>
		/// <param name="vertexSrc">Vertex shader source code</param>
		/// <param name="fragmentSrc">Fragment shader source code</param>
		/// <returns>An array containing both the vertex and the fragment shader compiled.</returns>
		public static Shader[] GenerateShaders(string setName, string vertexSrc, string fragmentSrc)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(AssetManager));
#endif
			return GenerateShaders(setName, Encoding.UTF8.GetBytes(vertexSrc), Encoding.UTF8.GetBytes(fragmentSrc));
		}

		/// <summary>
		/// Loads both the vertex and the fragment shader and generates necessary 
		/// files for other backends using Veldrid.SPIRV
		/// </summary>
		/// <param name="setName">The name of the shader set. Should be unique.</param>
		/// <param name="vertexBytecode">Vertex shader bytecode</param>
		/// <param name="fragmentBytecode">Fragment shader bytecode</param>
		/// <returns>An array containing both the vertex and the fragment shader compiled.</returns>
		public static Shader[] GenerateShaders(string setName, byte[] vertexBytecode, byte[] fragmentBytecode)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(AssetManager));
#endif
			ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, vertexBytecode, "main");
			ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, fragmentBytecode, "main");
			Shader[] shaders = gd.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
			shaders[0].Name = setName + "-Vertex";
			shaders[1].Name = setName + "-Fragment";
			ResourceCache.AddShaders(shaders);
			return shaders;
		}
		#endregion

		#region Utils
		private static string ExtractNameFromPath(ReadOnlySpan<char> path)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(AssetManager));
#endif
			return path.Slice(path.LastIndexOfAny("/\\") + 1, path.LastIndexOf(".")).ToString();
		}
		#endregion
	}
}
