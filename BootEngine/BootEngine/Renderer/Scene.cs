using BootEngine.Renderer.Cameras;

namespace BootEngine.Renderer
{
	public abstract class Scene
	{
		public Renderable[] RenderableList { get; set; }
		public OrthoCameraController CameraController { get; }
	}
}
