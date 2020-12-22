using NAudio.Wave;

namespace BootEngine.Audio
{
	public sealed class Sound
	{
		public float[] AudioData { get; set; }
		public WaveFormat WaveFormat { get; set; }
		public bool Loop { get; set; }
	}
}
