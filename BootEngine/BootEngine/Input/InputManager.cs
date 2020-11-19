using BootEngine.Utils.ProfilingTools;
using System.Collections.Generic;
using System.Numerics;
using BootEngine.Utils;
using Veldrid;

namespace BootEngine.Input
{
	public abstract class InputManager
	{
		private readonly static HashSet<KeyCodes> currentlyPressedKeys = new HashSet<KeyCodes>();
		private readonly static HashSet<KeyCodes> newKeysThisFrame = new HashSet<KeyCodes>();

		private readonly static HashSet<MouseButtonCodes> currentlyPressedMouseButtons = new HashSet<MouseButtonCodes>();
		private readonly static HashSet<MouseButtonCodes> newMouseButtonsThisFrame = new HashSet<MouseButtonCodes>();
		private static InputSnapshot snapshot;

		public static InputSnapshot Snapshot
		{
			get => snapshot;

			set
			{
				snapshot = value;
				UpdateInputState(value);
			}
		}

		public static InputManager Instance { get; private set; }

		public static T CreateInstance<T>() where T : InputManager, new()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(InputManager));
#endif
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

		public virtual bool GetKeyDown(KeyCodes key)
		{
			return currentlyPressedKeys.Contains(key);
		}

		public virtual bool GetMouseButtonDown(MouseButtonCodes button)
		{
			return currentlyPressedMouseButtons.Contains(button);
		}

		public virtual bool GetKeyPress(KeyCodes key)
		{
			return newKeysThisFrame.Contains(key);
		}

		public virtual bool GetMousePress(MouseButtonCodes mouseButton)
		{
			return newMouseButtonsThisFrame.Contains(mouseButton);
		}

		public virtual Vector2 GetMousePosition()
		{
			return Snapshot.MousePosition;
		}

		private static void UpdateInputState(InputSnapshot snapshot)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(InputManager));
#endif
			newKeysThisFrame.Clear();
			newMouseButtonsThisFrame.Clear();

			for (int i = 0; i < snapshot.KeyEvents.Count; i++)
			{
				KeyEvent ke = snapshot.KeyEvents[i];
				if (ke.Down)
				{
					if (currentlyPressedKeys.Add((KeyCodes)ke.Key))
					{
						newKeysThisFrame.Add((KeyCodes)ke.Key);
					}
				}
				else
				{
					currentlyPressedKeys.Remove((KeyCodes)ke.Key);
					newKeysThisFrame.Remove((KeyCodes)ke.Key);
				}
			}
			for (int i = 0; i < snapshot.MouseEvents.Count; i++)
			{
				MouseEvent me = snapshot.MouseEvents[i];
				if (me.Down)
				{
					if (currentlyPressedMouseButtons.Add((MouseButtonCodes)me.MouseButton))
					{
						newMouseButtonsThisFrame.Add((MouseButtonCodes)me.MouseButton);
					}
				}
				else
				{
					currentlyPressedMouseButtons.Remove((MouseButtonCodes)me.MouseButton);
					newMouseButtonsThisFrame.Remove((MouseButtonCodes)me.MouseButton);
				}
			}
		}
	}
}
