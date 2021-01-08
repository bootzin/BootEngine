using BootEngine;
using BootEngine.AssetsManager;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Events;
using BootEngine.Layers.GUI;
using ImGuiNET;
using Leopotam.Ecs;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Shoelace.Panels
{
	// TODO: Build and store directory tree and update it when necessary, since building it every frame is too costy
	internal sealed class AssetManagerPanel : Panel, IEcsRunSystem
	{
		private readonly EcsFilter<EcsKeyEvent> _keyPressedEvents = default;

		private string activeFolder = "";
		private DirectoryInfo activeFolderInfo = new DirectoryInfo(EditorConfig.AssetDirectory);
		private bool isFolderSelected;

		private bool shouldOpen;
		private string actionPath;

		private int selectedFile = -1;
		private int maxItems = 3;
		private int itemCount = 0;
		private readonly List<string> _supportedImageExtensions = new List<string> { ".png", ".bmp", ".jpg", ".jpeg" };
		private readonly DirectoryInfo _assetDirectoryInfo = new DirectoryInfo(EditorConfig.AssetDirectory);
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
					activeFolder = "";
					activeFolderInfo = _assetDirectoryInfo;
				}
				DrawAssetsDir(_assetDirectoryInfo);
				ImGui.TreePop();
			}
			ImGui.EndChild();

			ImGui.NextColumn();

			if (ImGui.IsItemClicked(0))
				selectedFile = -1;

			ImGui.BeginChild("ContentBrowser##1", new Vector2(0, ImGui.GetWindowHeight() * .71f));

			#region Breadcrumb Trail
			string[] splitPath = activeFolder.Split('\\');
			if (splitPath[0].Length > 0)
			{
				if (ImGui.Selectable("Assets", false, ImGuiSelectableFlags.AllowItemOverlap, ImGui.CalcTextSize("Assets")))
				{
					activeFolder = "";
					activeFolderInfo = _assetDirectoryInfo;
				}

				ImGui.SameLine();
				ImGui.Text(" > ");
				ImGui.SameLine();

				if (ImGui.Selectable(splitPath[0], false, ImGuiSelectableFlags.AllowItemOverlap, ImGui.CalcTextSize(splitPath[0])))
				{
					activeFolder = splitPath[0];
					activeFolderInfo = new DirectoryInfo(string.Join('\\', _assetDirectoryInfo.FullName, activeFolder));
				}
			}
			else
			{
				ImGui.Text("Assets");
			}

			for (int i = 0; i < splitPath[1..].Length; i++)
			{
				string path = splitPath[1..][i];
				ImGui.SameLine();
				ImGui.Text("> ");
				ImGui.SameLine();
				if (ImGui.Selectable(path, false, ImGuiSelectableFlags.AllowItemOverlap, ImGui.CalcTextSize(path)))
				{
					activeFolder = string.Join('\\', splitPath[..(i + 2)]);
					activeFolderInfo = new DirectoryInfo(string.Join('\\', _assetDirectoryInfo.FullName, activeFolder));
				}
			}
			#endregion

			ImGui.Separator();
			DrawAssets();

			ImGui.EndChild();

			ImGui.End();
		}

		private void DrawAssets()
		{
			maxItems = (int)(ImGui.GetColumnWidth() / assetSize) - 1;
			int i = 0;
			int currentIndex = 0;
			var files = activeFolderInfo.GetFileSystemInfos();
			itemCount = files.Length - 1;

			if (shouldOpen)
			{
				if (isFolderSelected)
				{
					activeFolder = Path.GetRelativePath(EditorConfig.AssetDirectory, actionPath);
					activeFolderInfo = new DirectoryInfo(actionPath);
					selectedFile = -1;
					isFolderSelected = false;
				}
				else
				{
					new Process()
					{
						StartInfo = new ProcessStartInfo(actionPath)
						{
							UseShellExecute = true,
							CreateNoWindow = true
						}
					}.Start();
				}
				shouldOpen = false;
			}

			foreach (var file in files)
			{
				bool isFolder = false;
				bool frame = false;
				if (ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(assetSize + 4)) || selectedFile == currentIndex)
				{
					frame = true;
					ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
					ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(.18f, .18f, .18f, 1));
				}

				if (ImGui.IsWindowFocused() && ImGui.IsMouseClicked(0) && !ImGui.IsAnyItemHovered())
				{
					selectedFile = -1;
					isFolder = false;
				}

				string fileExtIconsPath = Path.Combine(EditorConfig.InternalAssetDirectory, "textures", "icons", "fileExt - Designed by iconixar from Flaticon");
				string imgPath;
				if ((file.Attributes & FileAttributes.Directory) != 0)
				{
					imgPath = Path.Combine(EditorConfig.InternalAssetDirectory, "textures", "icons", "folder1 - Designed by DinosoftLabs from Flaticon.png");
					isFolder = true;
				}
				else if (_supportedImageExtensions.Contains(file.Extension))
				{
					imgPath = file.FullName;
				}
				else
				{
					if (File.Exists(Path.Combine(fileExtIconsPath, file.Extension[1..] + ".png")))
					{
						imgPath = Path.Combine(fileExtIconsPath, file.Extension[1..] + ".png");
					}
					else
					{
						imgPath = Path.Combine(fileExtIconsPath, "unknown.png");
					}
				}

				ImGui.BeginChild(file.FullName, new Vector2(assetSize + 4), frame, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoNav);
				ImGui.PushStyleColor(ImGuiCol.HeaderActive, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Vector4.Zero);
				ImGui.BeginGroup();
				if (ImGui.Selectable("##1" + file.Name, false, ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.AllowDoubleClick, new Vector2(0, assetSize)))
				{
					selectedFile = currentIndex;
					actionPath = file.FullName;
					isFolderSelected = isFolder;
					if (ImGui.IsMouseDoubleClicked(0))
					{
						shouldOpen = true;
					}
				}
				ImGui.EndGroup();
				ImGui.SameLine();

				ImGui.BeginGroup();
				ImGui.Indent(-6);
				ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
				var buttonTextureBinding = ImGuiLayer.GetOrCreateImGuiBinding(Application.App.Window.GraphicsDevice.ResourceFactory, AssetManager.LoadTexture2D(imgPath, BootEngine.Utils.BootEngineTextureUsage.Sampled));
				if (ImGui.ImageButton(buttonTextureBinding, new Vector2(assetSize - 6, assetSize - (2 * ImGui.CalcTextSize(file.Name).Y)), new Vector2(0, 1), new Vector2(1, 0), 4)
					 && !ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					selectedFile = currentIndex;
					actionPath = file.FullName;
					isFolderSelected = isFolder;
				}
				if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					// button double click
					shouldOpen = true;
				}

				if (ImGui.BeginDragDropSource())
				{
					var strPtr = Marshal.StringToHGlobalUni(file.FullName);
					ImGui.SetDragDropPayload("RESOURCE_PATH", strPtr, (uint)(sizeof(char) * file.FullName.Length) + 2);
					ImGui.Image(buttonTextureBinding, new Vector2(50, 50), new Vector2(0, 1), new Vector2(1, 0));
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
					ImGui.PopStyleColor();
				}

				currentIndex++;
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

		public void Run()
		{
			if (selectedFile != -1)
			{
				foreach (var kev in _keyPressedEvents)
				{
					var e = _keyPressedEvents.Get1(kev).Event;
					if (!e.Handled)
					{
						if (e.KeyCode == BootEngine.Utils.KeyCodes.Right && e is KeyPressedEvent)
						{
							selectedFile++;
							if (selectedFile > itemCount)
								selectedFile = itemCount;
							break;
						}
						if (e.KeyCode == BootEngine.Utils.KeyCodes.Left && e is KeyPressedEvent)
						{
							selectedFile--;
							if (selectedFile < 0)
								selectedFile = 0;
							break;
						}
						if (e.KeyCode == BootEngine.Utils.KeyCodes.Up && e is KeyPressedEvent)
						{
							selectedFile -= maxItems + 1;
							if (selectedFile < 0)
								selectedFile = 0;
							break;
						}
						if (e.KeyCode == BootEngine.Utils.KeyCodes.Down && e is KeyPressedEvent)
						{
							selectedFile += maxItems + 1;
							if (selectedFile > itemCount)
								selectedFile = itemCount;
							break;
						}

						if ((e.KeyCode == BootEngine.Utils.KeyCodes.Enter || e.KeyCode == BootEngine.Utils.KeyCodes.KeypadEnter) && e is KeyPressedEvent) 
						{
							shouldOpen = true;
						}
					}
				}
			}
		}
	}
}
