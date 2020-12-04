using ImGuiNET;
using Leopotam.Ecs;

namespace Shoelace.Services
{
	internal sealed class GuiService
	{
		internal OPERATION GizmoType { get; set; }
		internal bool ViewportHovered { get; set; }
		internal bool BlockEvents { get; set; }
		internal bool ViewportFocused { get; set; }
		internal EcsEntity SelectedEntity { get; set; }
	}
}
