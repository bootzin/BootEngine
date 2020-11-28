using BootEngine.ECS.Components;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using ImGuiNET;
using Leopotam.Ecs;
using Shoelace.Services;
using System;
using System.Numerics;

namespace Shoelace.Panels
{
	internal sealed class PropertiesPanel : Panel
	{
		private readonly GuiService _guiService = default;

		public override void OnGuiRender()
		{
			ImGui.Begin("Properties");
			if (_guiService.SelectedEntity != default)
			{
				DrawComponents(_guiService.SelectedEntity);
				if (ImGui.BeginPopupContextWindow("", 2, false))
				{
					if (ImGui.BeginMenu("Add Component"))
					{
						if (!_guiService.SelectedEntity.Has<TransformComponent>() && ImGui.MenuItem("Transform"))
							_guiService.SelectedEntity.Get<TransformComponent>();
						if (!_guiService.SelectedEntity.Has<SpriteComponent>() && ImGui.MenuItem("Sprite"))
							_guiService.SelectedEntity.Get<SpriteComponent>();
						if (!_guiService.SelectedEntity.Has<VelocityComponent>() && ImGui.MenuItem("Velocity"))
							_guiService.SelectedEntity.Get<VelocityComponent>();
						if (!_guiService.SelectedEntity.Has<CameraComponent>() && ImGui.MenuItem("Camera"))
						{
							CameraComponent cam = new CameraComponent()
							{
								Camera = new OrthoCamera()
							};
							_guiService.SelectedEntity.Replace(in cam);
						}
						ImGui.EndMenu();
					}

					ImGui.EndPopup();
				}
			}
			ImGui.End();
		}

		private void DrawComponents(EcsEntity entity)
		{
			if (entity.Has<TagComponent>())
			{
				DrawTagComponent(ref entity.Get<TagComponent>());
			}

			if (entity.Has<CameraComponent>())
			{
				ImGui.Separator();
				bool open = DrawComponentBase("Camera Component", out bool removeComponent);
				if (open)
				{
					DrawCameraComponent(ref entity.Get<CameraComponent>());
					ImGui.TreePop();
				}
				if (removeComponent)
					entity.Del<CameraComponent>();
			}

			if (entity.Has<TransformComponent>())
			{
				ImGui.Separator();
				bool open = DrawComponentBase("Transform Component", out bool removeComponent);
				if (open)
				{
					DrawTransformComponent(ref entity.Get<TransformComponent>());
					ImGui.TreePop();
				}
				if (removeComponent)
					entity.Del<TransformComponent>();
			}

			if (entity.Has<SpriteComponent>())
			{
				ImGui.Separator();
				bool open = DrawComponentBase("Sprite Component", out bool removeComponent);
				if (open)
				{
					DrawSpriteComponent(ref entity.Get<SpriteComponent>());
					ImGui.TreePop();
				}
				if (removeComponent)
					entity.Del<SpriteComponent>();
			}

			if (entity.Has<VelocityComponent>())
			{
				ImGui.Separator();
				bool open = DrawComponentBase("Velocity Component", out bool removeComponent);
				if (open)
				{
					DrawVelocityComponent(ref entity.Get<VelocityComponent>());
					ImGui.TreePop();
				}
				if (removeComponent)
					entity.Del<VelocityComponent>();
			}
		}

		private void DrawSpriteComponent(ref SpriteComponent spriteComponent)
		{
			string texName = string.IsNullOrWhiteSpace(spriteComponent.Texture?.Name) ? "None" : spriteComponent.Texture?.Name;
			ImGui.Text("Texture: " + texName);

			Vector4 color = spriteComponent.Color;
			if (ImGui.ColorEdit4("Color", ref color))
				spriteComponent.Color = color;
		}

		private void DrawVelocityComponent(ref VelocityComponent velocityComponent)
		{
			velocityComponent.Velocity = DrawVec3Control("Velocity", velocityComponent.Velocity);
			velocityComponent.RotationSpeed = Util.Deg2Rad(DrawVec3Control("Rot. Speed", Util.Rad2Deg(velocityComponent.RotationSpeed), dragAmnt: 1));
		}

		private void DrawTransformComponent(ref TransformComponent transformComponent)
		{
			ImGui.Indent(-8);
			transformComponent.Position = DrawVec3Control("Translation", transformComponent.Position);
			transformComponent.Rotation = Util.Deg2Rad(DrawVec3Control("Rotation", Util.Rad2Deg(transformComponent.Rotation), dragAmnt: 1));
			transformComponent.Scale = DrawVec3Control("Scale", transformComponent.Scale, 1);
		}

		private bool DrawComponentBase(string id, out bool removeComponent)
		{
			removeComponent = false;
			const ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.AllowItemOverlap
				| ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding;
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4));
			bool open = ImGui.TreeNodeEx(id, flags);
			ImGui.PopStyleVar();

			if (ImGui.BeginPopupContextItem(id, 2))
			{
				if (ImGui.MenuItem("Remove Component"))
				{
					removeComponent = true;
				}
				ImGui.EndPopup();
			}

			return open;
		}

		private void DrawCameraComponent(ref CameraComponent cc)
		{
			var cam = cc.Camera;
			bool active = cam.Active;
			if (ImGui.Checkbox("Active", ref active))
				cam.Active = active;

			if (ImGui.BeginCombo("Projection", cam.ProjectionType.ToString()))
			{
				for (int i = 0; i < 2; i++)
				{
					var projType = (ProjectionType)i;
					bool isSelected = cam.ProjectionType == projType;
					if (ImGui.Selectable(projType.ToString(), isSelected))
					{
						cam.ProjectionType = projType;
					}

					if (isSelected)
						ImGui.SetItemDefaultFocus();
				}

				ImGui.EndCombo();
			}

			if (cam.ProjectionType == ProjectionType.Orthographic)
			{
				float orthoSize = cam.OrthoSize;
				if (ImGui.DragFloat("Size", ref orthoSize))
					cam.OrthoSize = orthoSize;

				float orthoNear = cam.OrthoNear;
				if (ImGui.DragFloat("Near", ref orthoNear))
					cam.OrthoNear = orthoNear;

				float orthoFar = cam.OrthoFar;
				if (ImGui.DragFloat("Far", ref orthoFar))
					cam.OrthoFar = orthoFar;
			}

			if (cam.ProjectionType == ProjectionType.Perspective)
			{
				float perspectiveFov = Util.Rad2Deg(cam.PerspectiveFov);
				if (ImGui.DragFloat("Vertical Fov", ref perspectiveFov))
					cam.PerspectiveFov = Util.Deg2Rad(Util.Clamp(perspectiveFov, 1f, 179f));

				float perspectiveNear = cam.PerspectiveNear;
				if (ImGui.DragFloat("Near", ref perspectiveNear))
					cam.PerspectiveNear = Util.Clamp(perspectiveNear, 0.01f, cam.PerspectiveFar - 0.01f);

				float perspectiveFar = cam.PerspectiveFar;
				if (ImGui.DragFloat("Far", ref perspectiveFar))
					cam.PerspectiveFar = MathF.Max(perspectiveFar, cam.PerspectiveNear + 0.01f);
			}
		}

		private void DrawTagComponent(ref TagComponent tc)
		{
			var tag = tc.Tag;
			const ImGuiInputTextFlags flags = ImGuiInputTextFlags.AutoSelectAll;
			if (ImGui.InputText("##Tag", ref tag, 256u, flags))
			{
				tc.Tag = tag;
			}
		}

		private Vector3 DrawVec3Control(string label, Vector3 values, float resetValue = 0, float dragAmnt = 0.1f, float columnWidth = 100f)
		{
			var io = ImGui.GetIO();
			var boldFont = io.Fonts.Fonts[1];
			ImGui.PushID(label);

			ImGui.Columns(2);
			ImGui.SetColumnWidth(0, columnWidth);
			ImGui.Text(label);
			ImGui.NextColumn();

			ImGui.PushItemWidth(ImGui.CalcItemWidth() / 3f);
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 2));

			float lineHeight = ImGui.GetFontSize() + (ImGui.GetStyle().FramePadding.Y * 2f);
			Vector2 buttonSize = new Vector2(lineHeight + 3f, lineHeight);

			ImGui.PushStyleColor(ImGuiCol.Button, ColorF.ActiveRed);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorF.ActiveRed);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new ColorF(0.84f, 0.29f, 0.34f));
			ImGui.PushFont(boldFont);
			float valX = values.X;
			if (ImGui.Button("X", buttonSize))
			{
				valX = resetValue;
			}
			ImGui.PopStyleColor(3);
			ImGui.PopFont();

			ImGui.SameLine();
			ImGui.DragFloat("##X", ref valX, dragAmnt, 0.0f, 0.0f, "%.2f");
			ImGui.SameLine();

			ImGui.PushStyleColor(ImGuiCol.Button, new ColorF(.188f, .247f, .624f));
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, new ColorF(.188f, .247f, .624f));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new ColorF(0.29f, 0.34f, 0.84f));
			ImGui.PushFont(boldFont);
			float valY = values.Y;
			if (ImGui.Button("Y", buttonSize))
			{
				valY = resetValue;
			}
			ImGui.PopStyleColor(3);
			ImGui.PopFont();

			ImGui.SameLine();
			ImGui.DragFloat("##Y", ref valY, dragAmnt, 0.0f, 0.0f, "%.2f");
			ImGui.SameLine();

			ImGui.PushStyleColor(ImGuiCol.Button, new ColorF(.11f, .64f, .11f));
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, new ColorF(.11f, .64f, .11f));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new ColorF(0.29f, 0.84f, 0.34f));
			ImGui.PushFont(boldFont);
			float valZ = values.Z;
			if (ImGui.Button("Z", buttonSize))
			{
				valZ = resetValue;
			}
			ImGui.PopStyleColor(3);
			ImGui.PopFont();

			ImGui.SameLine();
			ImGui.DragFloat("##Z", ref valZ, dragAmnt, 0.0f, 0.0f, "%.2f");

			ImGui.Columns(1);
			values = new Vector3(valX, valY, valZ);
			ImGui.PopID();
			ImGui.PopItemWidth();
			ImGui.PopStyleVar();
			return values;
		}
	}
}
