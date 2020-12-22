using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;

namespace BootEngine.Audio
{
	public sealed class SoundEngine : IDisposable
	{
		private readonly IWavePlayer outputDevice;
		private readonly MixingSampleProvider mixer;

		public SoundEngine(int sampleRate = 44100, int channelCount = 2)
		{
			outputDevice = new WaveOutEvent();
			mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
			{
				ReadFully = true
			};
			outputDevice.Init(mixer);
			outputDevice.Play();
			mixer.MixerInputEnded += OnMixerInputEnded;
		}

		private void OnMixerInputEnded(object sender, SampleProviderEventArgs e)
		{
			if (((SoundSampleProvider)e.SampleProvider).Sound.Loop)
				AddMixerInput(new SoundSampleProvider(((SoundSampleProvider)e.SampleProvider).Sound));
		}

		private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
		{
			if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
			{
				return input;
			}
			if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
			{
				return new MonoToStereoSampleProvider(input);
			}
			throw new NotImplementedException("Not yet implemented this channel count conversion");
		}

		public void PlaySound(Sound sound)
		{
			if ((sound.Loop && !mixer.MixerInputs.Any(i => ((SoundSampleProvider)i).Sound == sound)) || !sound.Loop)
				AddMixerInput(new SoundSampleProvider(sound));
		}

		private void AddMixerInput(ISampleProvider input)
		{
			mixer.AddMixerInput(ConvertToRightChannelCount(input));
		}

		public void Reset()
		{
			mixer.RemoveAllMixerInputs();
		}

		public void Dispose()
		{
			outputDevice.Dispose();
		}

		public static readonly SoundEngine Instance = new SoundEngine(44100, 2);

		private class SoundSampleProvider : ISampleProvider
		{
			public Sound Sound { get; }
			private long position;

			public SoundSampleProvider(Sound sound)
			{
				this.Sound = sound;
			}

			public int Read(float[] buffer, int offset, int count)
			{
				var availableSamples = Sound.AudioData.Length - position;
				var samplesToCopy = Math.Min(availableSamples, count);
				Array.Copy(Sound.AudioData, position, buffer, offset, samplesToCopy);
				position += samplesToCopy;
				return (int)samplesToCopy;
			}

			public WaveFormat WaveFormat { get { return Sound.WaveFormat; } }
		}
	}
}
