using BootEngine;
using BootEngine.AssetsManager;
using BootEngine.Layers.GUI;
using ImGuiNET;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Shoelace.Panels
{
	// TODO: Build and store directory tree and update it when necessary, since building it every frame is too costy
	internal sealed class AssetManagerPanel : Panel
	{
		private string activeFolder = "Assets";
		private readonly DirectoryInfo AssetDirectoryInfo = new DirectoryInfo(EditorConfig.AssetDirectory);
		private DirectoryInfo activeFolderInfo = new DirectoryInfo(EditorConfig.AssetDirectory);
		private const int assetSize = 96;

		public override void OnGuiRender()
		{
			ImGui.Begin("Asset Manager");
			ImGui.Button("Create");
			ImGui.Separator();

			var width = ImGui.CalcItemWidth();
			ImGui.Columns(2, "##AssetColumns", true);
			ImGui.SetColumnWidth(0, width * .35f);

			ImGui.BeginChild("FileBrowser##1", new Vector2(0, ImGui.GetWindowHeight() * .71f));

			if (ImGui.TreeNodeEx("Assets", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Selected | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick))
			{
				if (ImGui.IsItemClicked())
				{
					activeFolder = "Assets";
					activeFolderInfo = AssetDirectoryInfo;
				}
				DrawAssetsDir(AssetDirectoryInfo);
				ImGui.TreePop();
			}
			ImGui.EndChild();

			ImGui.NextColumn();

			ImGui.BeginChild("ContentBrowser##1", new Vector2(0, ImGui.GetWindowHeight() * .71f));

			ImGui.Text(activeFolder);
			ImGui.Separator();
			DrawAssets();

			ImGui.EndChild();

			ImGui.End();
		}

		private void DrawAssets()
		{
			int maxItems = (int)(ImGui.GetColumnWidth() / assetSize) - 1;
			int i = 0;
			foreach (var file in activeFolderInfo.GetFileSystemInfos())
			{
				bool frame = false;
				if (ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(assetSize + 4)))
				{
					frame = true;
					ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
				}
				ImGui.BeginChild(file.FullName, new Vector2(assetSize + 4), frame, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
				ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.HeaderActive, Vector4.Zero);
				if (ImGui.Selectable("##1" + file.Name, false, ImGuiSelectableFlags.AllowItemOverlap, new Vector2(0, assetSize)))
				{
					//
				}
				ImGui.SameLine();
				ImGui.BeginGroup();
				ImGui.Indent(-6);
				ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
				if (ImGui.ImageButton(ImGuiLayer.GetOrCreateImGuiBinding(Application.App.Window.GraphicsDevice.ResourceFactory, AssetManager.LoadTexture2D("assets/textures/sampleBoot.png", BootEngine.Utils.BootEngineTextureUsage.Sampled)), new Vector2(assetSize - 6, assetSize - (2 * ImGui.CalcTextSize(file.Name).Y)), new Vector2(0, 1), new Vector2(1, 0)))
				{
					//
				}

				if (ImGui.BeginDragDropSource())
				{
					var strPtr = Marshal.StringToHGlobalUni(file.FullName);
					ImGui.SetDragDropPayload("TEXTURE", strPtr, (uint)(sizeof(char) * file.FullName.Length) + 2);
					// Example of how to read drag n drop data
					//string namae = Marshal.PtrToStringAuto(ImGui.GetDragDropPayload().Data);
					ImGui.EndDragDropSource();
				}
				ImGui.PopStyleColor(5);

				string name;
				if (file.Name.Length > 20)
				{
					ImGui.Indent(8);
					ImGui.PushTextWrapPos(assetSize - 12);
					name = file.Name[..18] + "...";
					ImGui.Text(name);
					ImGui.PopTextWrapPos();
				}
				else
				{
					name = file.Name;
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((assetSize - ImGui.CalcTextSize(name).X) / 2));
					ImGui.Text(name);
				}

				ImGui.EndGroup();
				ImGui.EndChild();

				if (maxItems > 0 && i / maxItems == 0)
				{
					ImGui.SameLine();
					i++;
				}
				else
				{
					i = 0;
				}

				if (frame)
				{
					ImGui.PopStyleVar();
				}
			}
		}

		private void DrawAssetsDir(DirectoryInfo curDirInfo)
		{
			foreach (var child in curDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
			{
				if (child.GetDirectories("*", SearchOption.TopDirectoryOnly).Length > 0)
				{
					bool open = ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick);
					if (ImGui.IsItemClicked())
					{
						activeFolder = Path.GetRelativePath(EditorConfig.AssetDirectory, child.FullName);
						activeFolderInfo = child;
					}
					if (open)
					{
						DrawAssetsDir(child);
						ImGui.TreePop();
					}
				}
				else
				{
					bool open = ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Leaf);
					if (ImGui.IsItemClicked())
					{
						activeFolder = Path.GetRelativePath(EditorConfig.AssetDirectory, child.FullName);
						activeFolderInfo = child;
					}
					if (open)
					{
						ImGui.TreePop();
					}
				}
			}
		}
	}
}
