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

		private static ConcurrentDictionary<string, Shader> ShaderCache
		{
			get { return shaderCache.Value; }
		}

		public static Shader GetShader(string shaderName)
		{
			if (ShaderCache.TryGetValue(shaderName, out Shader shader))
				return shader;
			return null;
		}

		public static void AddShader(Shader shader)
		{
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

		public static void ClearCache()
		{
			foreach (KeyValuePair<string, Shader> keyValuePair in ShaderCache)
			{
				keyValuePair.Value.Dispose();
			}
		}
	}
}
