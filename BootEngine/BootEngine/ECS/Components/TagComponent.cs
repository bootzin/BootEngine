namespace BootEngine.ECS.Components
{
	public struct TagComponent
	{
		public string Tag { get; set; }
		public TagComponent(string tag)
		{
			Tag = tag;
		}
	}
}
