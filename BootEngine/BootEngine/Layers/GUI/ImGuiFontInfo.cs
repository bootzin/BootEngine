namespace BootEngine.Layers.GUI
{
	public sealed class ImGuiFontInfo
	{
		public bool IsIconFont { get; set; }
		public ushort[] Ranges { get; set; }
		public string Path { get; set; }
		public float Size { get; set; }
		public bool MergeMode { get; set; }
	}
}
