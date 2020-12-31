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

		protected Entity Entity { get; set; }

		protected Script(Entity entity)
		{
			Entity = entity;
		}

		public virtual void OnUpdate() { }
		public virtual void OnEnable() { }
		public virtual void OnDisable() { }
	}
}
