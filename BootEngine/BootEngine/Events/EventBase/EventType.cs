namespace BootEngine.Events
{
	public enum EventType
	{
		None = 0,
		//Window
		WindowClose, WindowResize, WindowFocus, WindowLostFocus, WindowMoved,
		//App
		AppTick, AppUpdate, AppRender,
		//Keyboard
		KeyPressed, KeyReleased,
		//Mouse
		MouseButtonPressed, MouseButtonReleased, MouseMoved, MouseScrolled
	}
}
