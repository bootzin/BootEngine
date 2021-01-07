using BootEngine.AssetsManager;
using BootEngine.ECS.Components;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using BootEngine.Window;
using ImGuiNET;
using Leopotam.Ecs;
using Shoelace.Services;
using Shoelace.Styling;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Shoelace.Panels
{
	internal sealed class PropertiesPanel : Panel
	{
		private readonly GuiService _guiService = default;
		private bool loadTexture;
		private SpriteRendererComponent spriteComponentToChange;

		public override void OnGuiRender()
		{
			ImGui.Begin(FontAwesome5.Cubes + " Properties");
			if (_guiService.SelectedEntity != default)
			{
				DrawComponents(_guiService.SelectedEntity);
				if (ImGui.BeginPopupContextWindow("", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
				{
					if (ImGui.BeginMenu("Add Component"))
					{
						if (!_guiService.SelectedEntity.Has<TransformComponent>() && ImGui.MenuItem("Transform"))
						{
							_guiService.SelectedEntity.Get<TransformComponent>();
						}

						if (!_guiService.SelectedEntity.Has<SpriteRendererComponent>() && ImGui.MenuItem("Sprite Renderer"))
						{
							ref var sc = ref _guiService.SelectedEntity.Get<SpriteRendererComponent>();
							sc.Material = new Material("Standard2D");
							sc.SpriteData = RenderData2D.QuadData;
						}

						if (!_guiService.SelectedEntity.Has<VelocityComponent>() && ImGui.MenuItem("Velocity"))
						{
							_guiService.SelectedEntity.Get<VelocityComponent>();
						}

						if (!_guiService.SelectedEntity.Has<CameraComponent>() && ImGui.MenuItem("Camera"))
						{
							CameraComponent cam = new CameraComponent()
							{
								Camera = new Camera(true)
							};
							_guiService.SelectedEntity.Replace(in cam);
						}

#if false
						// Script components currently can't be added from the Editor.
						if (!_guiService.SelectedEntity.Has<ScriptingComponent>() && ImGui.MenuItem("Script"))
						{
							_guiService.SelectedEntity.Get<ScriptingComponent>();
						}
#endif
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
				{
					ref var cc = ref entity.Get<CameraComponent>();
					cc.Camera.Dispose();
					entity.Del<CameraComponent>();
				}
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
				{
					entity.Del<TransformComponent>();
				}
			}

			if (entity.Has<SpriteRendererComponent>())
			{
				ImGui.Separator();
				bool open = DrawComponentBase("Sprite Component", out bool removeComponent);
				if (open)
				{
					DrawSpriteComponent(ref entity.Get<SpriteRendererComponent>());
					ImGui.TreePop();
				}
				if (removeComponent)
				{
					ref var sc = ref entity.Get<SpriteRendererComponent>();
					sc.Material.Dispose();
					sc.SpriteData.Dispose();
					entity.Del<SpriteRendererComponent>();
				}
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
				{
					entity.Del<VelocityComponent>();
				}
			}

			if (entity.Has<ScriptingComponent>())
			{
				ImGui.Separator();
				bool open = DrawComponentBase("Script Component", out bool removeComponent);
				if (open)
				{
					DrawScriptingComponent(ref entity.Get<ScriptingComponent>());
					ImGui.TreePop();
				}
				if (removeComponent)
				{
					entity.Del<ScriptingComponent>();
				}
			}
		}

		private void DrawScriptingComponent(ref ScriptingComponent scriptingComponent)
		{
			string name = scriptingComponent.Script.FileName;
			if (ImGui.InputText("##ScriptName", ref name, 512u, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
			{
				if (!name.EndsWith(".cs"))
					name += ".cs";
				var newPath = scriptingComponent.Script.FilePath.Substring(0, scriptingComponent.Script.FilePath.IndexOf(scriptingComponent.Script.FileName)) + name;
				File.Move(scriptingComponent.Script.FilePath, newPath, true);
				scriptingComponent.Script.FileName = name;
				scriptingComponent.Script.FilePath = newPath;
			}

			ImGui.SameLine();
			if (ImGui.Button(FontAwesome5.Edit))
			{
				var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())); // TODO: Get proper path
				ProcessStartInfo psi = new ProcessStartInfo("cmd", "/C start devenv " + projectPath + " /Edit " + scriptingComponent.Script.FilePath);
				psi.UseShellExecute = true;
				psi.CreateNoWindow = true;
				Process.Start(psi);
			}
		}

		private void DrawSpriteComponent(ref SpriteRendererComponent spriteComponent)
		{
			string texName = string.IsNullOrWhiteSpace(spriteComponent.SpriteData.Texture?.Name) ? "None" : spriteComponent.SpriteData.Texture.Name[(int)(MathF.Max(spriteComponent.SpriteData.Texture.Name.LastIndexOf('\\'), spriteComponent.SpriteData.Texture.Name.LastIndexOf('/')) + 1)..];

			ImGui.Text("Texture");
			ImGui.SameLine();

			ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.08f, .08f, .08f, 1));
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(.08f, .08f, .08f, 1));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(.08f, .08f, .08f, 1));
			ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, .5f));
			ImGui.Button(texName, new Vector2(ImGui.CalcItemWidth(), 20));
			ImGui.PopStyleVar();

			if (ImGui.BeginDragDropTarget() && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				string texData = Marshal.PtrToStringAuto(ImGui.GetDragDropPayload().Data);
				spriteComponent.SpriteData.Texture = AssetManager.LoadTexture2D(texData, BootEngineTextureUsage.Sampled);

				ImGui.EndDragDropTarget();
			}

			ImGui.PopStyleColor(3);

			ImGui.SameLine();
			if (ImGui.Button(FontAwesome5.FolderOpen))
			{
				loadTexture = true;
				spriteComponentToChange = spriteComponent;
			}

			if (FileDialog.ShowFileDialog("Load a Texture", ref loadTexture, out string texPath, FileDialog.DialogType.Open, "Images", ".png", ".jpg", ".bmp"))
			{
				spriteComponentToChange.SpriteData.Texture = AssetManager.LoadTexture2D(texPath, BootEngineTextureUsage.Sampled);
				spriteComponentToChange = default;
				loadTexture = false;
			}

			Vector4 color = spriteComponent.Color;
			if (ImGui.ColorEdit4("Color", ref color))
				spriteComponent.Color = color;

			ImGui.Text("Material");
			ImGui.SameLine();

			ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.08f, .08f, .08f, 1));
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(.08f, .08f, .08f, 1));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(.08f, .08f, .08f, 1));
			ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, .5f));
			ImGui.Button(spriteComponent.Material.Name, new Vector2(ImGui.CalcItemWidth(), 20));
			ImGui.PopStyleVar();

			if (ImGui.BeginDragDropTarget())
			{
				ImGui.Text("TODO"); // Allow drag n' drop of textures
			}
			ImGui.EndDragDropTarget();

			ImGui.PopStyleColor(3);
		}

		private void DrawVelocityComponent(ref VelocityComponent velocityComponent)
		{
			velocityComponent.Velocity = DrawVec3Control("Velocity", velocityComponent.Velocity);
			velocityComponent.RotationSpeed = MathUtil.Deg2Rad(DrawVec3Control("Rot. Speed", MathUtil.Rad2Deg(velocityComponent.RotationSpeed), dragAmnt: 1));
		}

		private void DrawTransformComponent(ref TransformComponent transformComponent)
		{
			ImGui.Indent(-8);
			transformComponent.Translation = DrawVec3Control("Translation", transformComponent.Translation);
			transformComponent.Rotation = MathUtil.Deg2Rad(DrawVec3Control("Rotation", MathUtil.Rad2Deg(transformComponent.Rotation), dragAmnt: 1));
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

			if (ImGui.BeginPopupContextItem(id))
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
				float perspectiveFov = MathUtil.Rad2Deg(cam.PerspectiveFov);
				if (ImGui.DragFloat("Vertical Fov", ref perspectiveFov))
					cam.PerspectiveFov = MathUtil.Deg2Rad(MathUtil.Clamp(perspectiveFov, 1f, 179f));

				float perspectiveNear = cam.PerspectiveNear;
				if (ImGui.DragFloat("Near", ref perspectiveNear))
					cam.PerspectiveNear = MathUtil.Clamp(perspectiveNear, 0.01f, cam.PerspectiveFar - 0.01f);

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
