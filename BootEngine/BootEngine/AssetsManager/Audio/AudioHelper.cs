using BootEngine.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.Linq;

namespace BootEngine.AssetsManager.Audio
{
	internal static class AudioHelper
	{
		public static Sound LoadAudio(string path, bool loop)
		{
			using var audioFileReader = new AudioFileReader(path);
			var resampler = new WdlResamplingSampleProvider(audioFileReader, 44100);
			Sound snd = new Sound
			{
				WaveFormat = resampler.WaveFormat
			};
			List<float> wholeFile = new List<float>((int)(audioFileReader.Length / 4));
			float[] readBuffer = new float[resampler.WaveFormat.SampleRate * resampler.WaveFormat.Channels];
			int samplesRead;
			while ((samplesRead = resampler.Read(readBuffer, 0, readBuffer.Length)) > 0)
			{
				wholeFile.AddRange(readBuffer.Take(samplesRead));
			}
			snd.AudioData = wholeFile.ToArray();
			snd.Loop = loop;
			return snd;
		}
	}
}
