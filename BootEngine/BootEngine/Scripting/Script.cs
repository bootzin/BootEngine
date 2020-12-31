using BootEngine.ECS;

namespace BootEngine.Scripting
{
	public abstract class Script
	{
		private bool enabled = true;

		public bool Enabled
		{
			get { return enabled; }
			set
			{
				if (value)
					OnEnable();
				else
					OnDisable();
				enabled = value;
			}
		}

		public string FilePath { get; set; }
		public string FileName { get; set; }

		protected Entity Entity { get; set; }

		protected Script(Entity entity, [System.Runtime.CompilerServices.CallerFilePath] string path = null)
		{
			Entity = entity;
			FilePath = path;
			FileName = path[(path.LastIndexOf('\\') + 1)..];
		}

		public virtual void OnUpdate() { }
		public virtual void OnEnable() { }
		public virtual void OnDisable() { }
	}
}
