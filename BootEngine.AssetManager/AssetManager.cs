using BootEngine.AssetManager.Images;
using BootEngine.AssetManager.Shaders;
using StbImageSharp;
using System.Text;
using Veldrid.SPIRV;
using Veldrid;

namespace BootEngine.AssetManager
{
	public sealed class AssetManager
	{
		#region Properties
		private readonly GraphicsDevice gd;
		#endregion

		#region Constructor
		public AssetManager(GraphicsDevice graphicsDevice)
		{
			gd = graphicsDevice;
		}
		#endregion

		#region Textures
		/// <summary>
		/// Loads and updates a single 2D <see cref="Texture"/>
		/// </summary>
		/// <param name="texturePath">Path to the texture.</param>
		/// <param name="usage">A collection of flags determining the <see cref="TextureUsage"/></param>
		/// <returns>The update <see cref="Texture"/></returns>
		public Texture LoadTexture2D(string texturePath, TextureUsage usage)
		{
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
		public Shader[] LoadShaders(string path)
		{
			(string vs, string fs) = ShaderHelper.LoadShaders(path);
			var vertexShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vs), "main"));
			var fragShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fs), "main"));
			Shader[] shaders = new Shader[2] { vertexShader, fragShader };
			vertexShader.Dispose();
			fragShader.Dispose();
			return shaders;
		}

		/// <summary>
		/// Loads every shader specified in <paramref name="pathStageList"/>. 
		/// Does not generate equivalents for other backends.
		/// </summary>
		/// <param name="pathStageList">Array of path-stage arguments for the shader</param>
		/// <returns>An array with the created shaders.</returns>
		public Shader[] LoadShaders(params (string path, ShaderStages stage)[] pathStageList)
		{
			Shader[] shaders = new Shader[pathStageList.Length];
			for (int i = 0; i < pathStageList.Length; i++)
			{
				shaders[i] = gd.ResourceFactory.CreateShader(new ShaderDescription(pathStageList[i].stage, ShaderHelper.LoadShader(pathStageList[i].path), "main"));
			}
			return shaders;
		}

		/// <summary>
		/// Loads both the vertex and the fragment shader and generates necessary 
		/// files for other backends using Veldrid.SPIRV
		/// </summary>
		/// <param name="path">The path to the file in which the shaders are.</param>
		/// <returns>An array containing both the vertex and the fragment shader compiled.</returns>
		public Shader[] GenerateShaders(string path)
		{
			(string vs, string fs) = ShaderHelper.LoadShaders(path);
			ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vs), "main");
			ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fs), "main");
			Shader[] shaders = gd.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
			shaders[0].Name = path + "-Vertex";
			shaders[1].Name = path + "-Fragment";
			return shaders;
		}

		/// <summary>
		/// Loads both the vertex and the fragment shader and generates necessary 
		/// files for other backends using Veldrid.SPIRV
		/// </summary>
		/// <param name="vertexSrc">Vertex shader source code</param>
		/// <param name="fragmentSrc">Fragment shader source code</param>
		/// <returns>An array containing both the vertex and the fragment shader compiled.</returns>
		public Shader[] GenerateShaders(string vertexSrc, string fragmentSrc)
		{
			ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexSrc), "main");
			ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentSrc), "main");
			return gd.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
		}

		/// <summary>
		/// Loads both the vertex and the fragment shader and generates necessary 
		/// files for other backends using Veldrid.SPIRV
		/// </summary>
		/// <param name="vertexBytecode">Vertex shader bytecode</param>
		/// <param name="fragmentBytecode">Fragment shader bytecode</param>
		/// <returns>An array containing both the vertex and the fragment shader compiled.</returns>
		public Shader[] GenerateShaders(byte[] vertexBytecode, byte[] fragmentBytecode)
		{
			ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, vertexBytecode, "main");
			ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, fragmentBytecode, "main");
			return gd.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
		}
		#endregion
	}
}
