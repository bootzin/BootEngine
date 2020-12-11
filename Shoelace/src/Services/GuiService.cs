using ImGuiNET;
using Leopotam.Ecs;

namespace Shoelace.Services
{
	internal sealed class GuiService
	{
		internal OPERATION? GizmoType { get; set; } = OPERATION.TRANSLATE;
		internal bool ViewportHovered { get; set; }
		internal bool BlockEvents { get; set; }
		internal bool ViewportFocused { get; set; }
		internal EcsEntity SelectedEntity { get; set; }
		internal bool NewScene { get; set; }
		internal bool ShouldLoadScene;
		internal bool ShouldSaveScene;
	}
}
