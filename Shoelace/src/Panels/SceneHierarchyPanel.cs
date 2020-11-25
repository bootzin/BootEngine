using BootEngine.ECS;
using BootEngine.ECS.Components;
using ImGuiNET;
using Leopotam.Ecs;
using Shoelace.Services;

namespace Shoelace.Panels
{
	internal sealed class SceneHierarchyPanel : Panel
	{
		private readonly Scene _scene = default;
		private readonly EcsFilter<TagComponent> _hierarchyFilter = default;

		private readonly GuiService _guiService = default;

		public override void OnGuiRender()
		{
			ImGui.Begin("Scene Hierarchy");

			const ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;

			foreach (var entityId in _hierarchyFilter)
			{
				DrawEntitynode(entityId, flags);
			}

			if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered())
				_guiService.SelectedEntity = default;

			if (ImGui.BeginPopupContextWindow("", 2, false))
			{
				if (ImGui.MenuItem("Create Entity"))
					_scene.CreateEntity("New Entity");

				ImGui.EndPopup();
			}

			ImGui.End();
		}

		private void DrawEntitynode(int entityId, ImGuiTreeNodeFlags flags)
		{
			ref var entt = ref _hierarchyFilter.GetEntity(entityId);
			if (_guiService.SelectedEntity == entt)
				flags |= ImGuiTreeNodeFlags.Selected;
			bool open = ImGui.TreeNodeEx(_hierarchyFilter.Get1(entityId).Tag, flags);

			if (ImGui.IsItemClicked())
			{
				_guiService.SelectedEntity = entt;
			}

			if (ImGui.BeginPopupContextItem(entt.GetInternalId().ToString(), 2))
			{
				if (ImGui.MenuItem("Delete Entity"))
				{
					if (_guiService.SelectedEntity == entt)
						_guiService.SelectedEntity = default;
					entt.Destroy();
				}
				ImGui.EndPopup();
			}

			if (open)
			{
				ImGui.TreePop();
			}
		}
	}
}
