using BootEngine.Log;
using System;
using System.IO;
using System.Text;
using static BootEngine.AssetManager.GeneralHelper;

namespace BootEngine.AssetManager.Shaders
{
	public static class ShaderHelper
	{
		private const string TYPE_TOKEN = "#type";
		public static byte[] LoadShader(string path)
		{
			return ReadFile(Path.Combine("shaders", path));
		}

		public static (string vertexSource, string fragmentSource) LoadShaders(string path)
		{
			string[] shaders = new string[2];
			ReadOnlySpan<char> file = Encoding.UTF8.GetString(LoadShader(path));
			int tokenPosition = file.IndexOf(TYPE_TOKEN);
			while (tokenPosition != -1)
			{
				ReadOnlySpan<char> remaining = file.Slice(tokenPosition + TYPE_TOKEN.Length + 1).TrimStart();
				ReadOnlySpan<char> token = remaining.Slice(0, remaining.IndexOfAny("\r\n"));
				Logger.CoreAssert(token.SequenceEqual("vertex") || token.SequenceEqual("fragment") || token.SequenceEqual("pixel"), $"Unsupported Shader type: {token.ToArray()}");

				ReadOnlySpan<char> shaderBegin = remaining.Slice(token.Length).TrimStart();
				tokenPosition = shaderBegin.IndexOf(TYPE_TOKEN);

				if (tokenPosition != -1)
				{
					shaders.SetValue(shaderBegin.Slice(0, tokenPosition).ToString(), token.SequenceEqual("vertex") ? 0 : 1);
					tokenPosition += (file.Length - shaderBegin.Length);
				}
				else
				{
					shaders.SetValue(shaderBegin.ToString(), token.SequenceEqual("vertex") ? 0 : 1);
				}
			}

			return (shaders[0], shaders[1]);
		}
	}
}
