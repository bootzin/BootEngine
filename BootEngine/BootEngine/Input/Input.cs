using Veldrid;

namespace BootEngine.Input
{
	public abstract class InputManager
	{
		public static InputSnapshot Snapshot { get; set; }

		public static InputManager Instance { get; private set; }

		public static T CreateInstance<T>() where T : InputManager, new()
		{
			if (Instance == null)
			{
				T input = new T();
				input.Initialize();
				Instance = input;
				return input;
			}
			return (T)Instance;
		}

		protected virtual void Initialize() { }

		protected virtual bool IsKeyPressed(Key key)
		{
			for (int i = 0; i  < Snapshot.KeyEvents.Count; i++)
			{
				if (Snapshot.KeyEvents[i].Down && Snapshot.KeyEvents[i].Key == key)
					return true;
			}
			return false;
		}
	}
}
