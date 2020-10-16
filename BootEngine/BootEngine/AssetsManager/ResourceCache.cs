﻿using BootEngine.Utils.ProfilingTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Utils.Exceptions;
using Veldrid;

namespace BootEngine.AssetsManager
{
	public static class ResourceCache
	{
		private static readonly Lazy<ConcurrentDictionary<string, Shader>> shaderCache = new Lazy<ConcurrentDictionary<string, Shader>>();
		private static readonly Lazy<ConcurrentDictionary<string, Texture>> textureCache = new Lazy<ConcurrentDictionary<string, Texture>>();

		private static ConcurrentDictionary<string, Shader> ShaderCache
		{
			get { return shaderCache.Value; }
		}

		private static ConcurrentDictionary<string, Texture> TextureCache
		{
			get { return textureCache.Value; }
		}

		#region Shaders
		public static Shader GetShader(string shaderName)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ResourceCache));
#endif
			if (ShaderCache.TryGetValue(shaderName, out Shader shader))
				return shader;
			return null;
		}

		public static void AddShader(Shader shader)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ResourceCache));
#endif
			if (!ShaderCache.TryAdd(shader.Name, shader))
				throw new BootEngineException($"A Shader name {shader.Name} has already been loaded!");
		}

		public static void AddShaders(Shader[] shaders)
		{
			for (int i = 0; i < shaders.Length; i++)
			{
				AddShader(shaders[i]);
			}
		}
		#endregion

		#region Textures
		public static Texture GetTexture(string texturePath)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ResourceCache));
#endif
			if (TextureCache.TryGetValue(texturePath, out Texture tex))
				return tex;
			return null;
		}

		public static void AddTexture(Texture tex, string texturePath)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ResourceCache));
#endif
			if (!TextureCache.TryAdd(texturePath, tex))
				throw new BootEngineException($"A Texture with path {texturePath} has already been loaded!");
		}
		#endregion

		public static void ClearCache()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ResourceCache));
#endif
			foreach (KeyValuePair<string, Shader> keyValuePair in ShaderCache)
			{
				keyValuePair.Value.Dispose();
			}
		}
	}
}