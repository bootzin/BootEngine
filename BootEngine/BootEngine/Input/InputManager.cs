using System.Numerics;
using Utils;
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

		public virtual bool IsKeyPressed(KeyCodes key)
		{
			for (int i = 0; i < Snapshot.KeyEvents.Count; i++)
			{
				if (Snapshot.KeyEvents[i].Down && Snapshot.KeyEvents[i].Key == (Key)key)
					return true;
			}
			return false;
		}

		public virtual bool IsMouseButtonPressed(MouseButtonCodes mouseButton)
		{
			return Snapshot.IsMouseDown((MouseButton)mouseButton);
		}

		public virtual Vector2 GetMousePosition()
		{
			return Snapshot.MousePosition;
		}
	}
}
