﻿using BootEngine;
using BootEngine.AssetsManager;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Events;
using BootEngine.Input;
using BootEngine.Layers.GUI;
using BootEngine.Logging;
using BootEngine.Utils;
using BootEngine.Window;
using ImGuiNET;
using Leopotam.Ecs;
using Shoelace.Styling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace Shoelace.Panels
{
	// TODO: Build and store directory tree and update it when necessary, since building it every frame is too costy
	internal sealed class AssetManagerPanel : Panel, IEcsRunSystem
	{
		private readonly EcsFilter<EcsKeyEvent> _keyPressedEvents = default;

		private bool setColumnWidth = true;
		private string searchPattern = "";

		private string activeFolder = "";
		private DirectoryInfo activeFolderInfo = new DirectoryInfo(EditorConfig.AssetDirectory);
		private bool isFolderSelected;

		private bool shouldOpen;
		private string actionPath;

		private int selectedFile = -1;
		private int maxItems = 3;
		private int itemCount = 0;

		private int deleteFile = -1;
		private bool promptDelete;
		private bool renamingFile;

		private bool isSearching;

		private bool createFolder;
		private bool createFile;
		private string createFileName = "";
		private FileType createFileType;
		private bool importingAsset;
		private readonly List<string> _supportedImageExtensions = new List<string> { ".png", ".bmp", ".jpg", ".jpeg" };
		private readonly DirectoryInfo _assetDirectoryInfo = new DirectoryInfo(EditorConfig.AssetDirectory);
		private const int assetSize = 96;

		public override void OnGuiRender()
		{
			bool open = true;
			ImGui.Begin("Asset Manager", ref open, ImGuiWindowFlags.NoNav);
			if (ImGui.Button("Create"))
			{
				ImGui.OpenPopup("Create");
			}

			#region Search bar
			var width = ImGui.CalcItemWidth();
			ImGui.SameLine();

			ImGui.SetNextItemWidth(ImGui.CalcItemWidth());
			ImGui.SetCursorPosX((width * .35f) + 8);
			string tmp = searchPattern;
			bool updateSearchPattern = ImGui.InputTextWithHint("##Search", FontAwesome5.Search + " Search", ref tmp, 512, ImGuiInputTextFlags.AutoSelectAll);

			if (ImGui.IsItemClicked(0))
			{
				isSearching = true;
			}

			ImGui.SameLine();

			ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 8);
			if (ImGui.Button(FontAwesome5.Times))
			{
				searchPattern = "";
			}

			if (updateSearchPattern)
			{
				searchPattern = tmp;
			}
			#endregion

			if (ImGui.BeginPopup("Create"))
			{
				DrawCreateMenu();
				ImGui.EndPopup();
			}

			ImGui.Separator();

			ImGui.Columns(2, "##AssetColumns", true);
			if (setColumnWidth)
			{
				ImGui.SetColumnWidth(0, width * .35f);
				setColumnWidth = false;
			}

			ImGui.BeginChild("FileBrowser##1", new Vector2(0, ImGui.GetWindowHeight() * .71f));

			if (ImGui.TreeNodeEx("Assets", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Selected | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick))
			{
				if (ImGui.IsItemClicked())
				{
					activeFolder = "";
					activeFolderInfo = _assetDirectoryInfo;
					isSearching = false;
				}
				DrawAssetsDir(_assetDirectoryInfo);
				ImGui.TreePop();
			}
			ImGui.EndChild();

			ImGui.NextColumn();

			if (ImGui.IsItemClicked(0))
			{
				selectedFile = -1;
				isSearching = false;
			}

			ImGui.BeginChild("ContentBrowser##1", new Vector2(0, ImGui.GetWindowHeight() * .71f), false, ImGuiWindowFlags.NoNav);

			#region Breadcrumb Trail
			string[] splitPath = activeFolder.Split('\\');
			if (splitPath[0].Length > 0)
			{
				if (ImGui.Selectable("Assets", false, ImGuiSelectableFlags.AllowItemOverlap, ImGui.CalcTextSize("Assets")))
				{
					activeFolder = "";
					activeFolderInfo = _assetDirectoryInfo;
				}

				DragAndDropTargetFolder(_assetDirectoryInfo);

				ImGui.SameLine();
				ImGui.Text(" > ");
				ImGui.SameLine();

				if (ImGui.Selectable(splitPath[0], false, ImGuiSelectableFlags.AllowItemOverlap, ImGui.CalcTextSize(splitPath[0])))
				{
					activeFolder = splitPath[0];
					activeFolderInfo = new DirectoryInfo(string.Join('\\', _assetDirectoryInfo.FullName, activeFolder));
				}

				DragAndDropTargetFolder(new DirectoryInfo(string.Join('\\', _assetDirectoryInfo.FullName, splitPath[0])));
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

				DragAndDropTargetFolder(new DirectoryInfo(string.Join('\\', _assetDirectoryInfo.FullName, string.Join('\\', splitPath[..(i + 2)]))));
			}
			#endregion

			ImGui.Separator();
			DrawAssets();

			if (ImGui.BeginPopupContextWindow("", ImGuiPopupFlags.NoOpenOverItems | ImGuiPopupFlags.MouseButtonRight))
			{
				if (ImGui.BeginMenu("Create"))
				{
					DrawCreateMenu();
					ImGui.EndMenu();
				}

				if (ImGui.MenuItem("Import new asset..."))
				{
					importingAsset = true;
				}

				ImGui.Separator();

				if (ImGui.MenuItem("Open in Explorer"))
				{
					new Process()
					{
						StartInfo = new ProcessStartInfo(activeFolderInfo.FullName)
						{
							UseShellExecute = true,
							CreateNoWindow = true
						}
					}.Start();
				}

				ImGui.EndPopup();
			}

			ImGui.EndChild();

			ImGui.End();

			if (FileDialog.ShowFileDialog(ref importingAsset, out string importPath))
			{
				importingAsset = false;
				if (File.Exists(importPath))
					File.Copy(importPath, activeFolderInfo.FullName + importPath[importPath.LastIndexOf('\\')..]);
			}
		}

		private void DrawCreateMenu()
		{
			if (ImGui.MenuItem("Folder"))
			{
				createFolder = true;
			}
			ImGui.Separator();
			if (ImGui.MenuItem("Script"))
			{
				createFile = true;
				createFileName = "NewScript.cs";
				createFileType = FileType.Script;
			}

			if (ImGui.MenuItem("Shader"))
			{
				createFile = true;
				createFileName = "NewShader.glsl";
				createFileType = FileType.Shader;
			}
		}

		private void DrawAssets()
		{
			maxItems = (int)(ImGui.GetColumnWidth() / assetSize) - 1;
			int i = 0;
			int currentIndex = 0;

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

			if (createFolder)
			{
				activeFolderInfo.CreateSubdirectory("New Folder");
				searchPattern = "";
			}
			if (createFile)
			{
				File.Create(Path.Combine(activeFolderInfo.FullName, createFileName)).Dispose();
				searchPattern = "";
			}

			FileSystemInfo[] files;
			if (searchPattern.Length > 0)
				files = activeFolderInfo.GetFileSystemInfos("*" + searchPattern + "*", new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true });
			else
				files = activeFolderInfo.GetFileSystemInfos();
			itemCount = files.Length - 1;

			foreach (var file in files)
			{
				if ((createFolder && file.Name == "New Folder") || (createFile && file.Name == createFileName))
				{
					createFolder = createFile = false;
					selectedFile = currentIndex;
					renamingFile = true;
				}
				if (selectedFile == currentIndex)
				{
					actionPath = file.FullName;
					isFolderSelected = (file.Attributes & FileAttributes.Directory) != 0;
				}
				if (deleteFile != -1 && selectedFile == currentIndex && deleteFile == currentIndex)
				{
					if (promptDelete)
					{
						ImGui.OpenPopup("DeleteFolderPopup");

						Vector2 center = new Vector2(ImGui.GetMainViewport().Pos.X + (ImGui.GetMainViewport().Size.X * 0.5f), ImGui.GetMainViewport().Pos.Y + (ImGui.GetMainViewport().Size.Y * 0.5f));
						ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(.5f, .5f));
						if (ImGui.BeginPopup("DeleteFolderPopup", ImGuiWindowFlags.Modal))
						{
							ImGui.TextColored(ColorF.Red, "Are you sure you want to delete this item?");
							ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);
							ImGui.TextUnformatted(file.Name);
							ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);
							if (ImGui.Button("Yes"))
							{
								promptDelete = false;
								ImGui.CloseCurrentPopup();
							}
							ImGui.SameLine();
							if (ImGui.Button("No"))
							{
								promptDelete = false;
								deleteFile = -1;
								ImGui.CloseCurrentPopup();
							}
							ImGui.EndPopup();
						}
					}
					else
					{
						if (file is DirectoryInfo di)
							di.Delete(true);
						else
							file.Delete();
						deleteFile = -1;
						continue;
					}
				}

				bool isFolder = false;
				bool frame = false;
				if (ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(assetSize + 4)) || selectedFile == currentIndex)
				{
					frame = true;
					ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
					ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(.18f, .18f, .18f, 1));
				}

				if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				{
					if (renamingFile && createFileName.Length > 0)
					{
						UpdateFileContent();
					}
					renamingFile = false;
					isSearching = false;
					if (ImGui.IsWindowFocused() && !ImGui.IsAnyItemHovered())
					{
						selectedFile = -1;
						isFolder = false;
					}
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
					if (file.Extension.Length > 0 && File.Exists(Path.Combine(fileExtIconsPath, file.Extension[1..] + ".png")))
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
					isFolderSelected = isFolder;
					if (ImGui.IsMouseDoubleClicked(0))
					{
						shouldOpen = true;
					}
				}
				if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
				{
					selectedFile = currentIndex;
					isSearching = false;
				}


				if (isFolder)
					DragAndDropTargetFolder((DirectoryInfo)file);

				ImGui.EndGroup();
				ImGui.SameLine();

				ImGui.BeginGroup();
				ImGui.Indent(-6);
				ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);

				Texture tex;
				try
				{
					tex = AssetManager.LoadTexture2D(imgPath, BootEngineTextureUsage.Sampled);
				}
				catch
				{
					tex = AssetManager.LoadTexture2D(Path.Combine(fileExtIconsPath, "unknown.png"), BootEngineTextureUsage.Sampled);
				}
				var buttonTextureBinding = ImGuiLayer.GetOrCreateImGuiBinding(Application.App.Window.GraphicsDevice.ResourceFactory, tex);

				if (ImGui.ImageButton(buttonTextureBinding, new Vector2(assetSize - 6, assetSize - (2 * ImGui.CalcTextSize(file.Name).Y)), new Vector2(0, 1), new Vector2(1, 0), 4)
					 && !ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					selectedFile = currentIndex;
					isFolderSelected = isFolder;
					isSearching = false;
				}
				if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered())
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
				if (renamingFile && selectedFile == currentIndex)
				{
					name = file.Name;
					ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + MathF.Max(((assetSize - ImGui.CalcTextSize(name).X) / 2) - 4, 0), ImGui.GetCursorPosY() - 3));
					if (name.Length > 10)
					{
						ImGui.SetNextItemWidth(ImGui.CalcTextSize(name[..10]).X + 4);
					}
					else
					{
						ImGui.SetNextItemWidth(ImGui.CalcTextSize(name).X + 4);
					}
					if (ImGui.InputText("##RenameInput", ref name, 512, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue) && file.Name != name)
					{
						try
						{
							if ((file.Attributes & FileAttributes.Directory) != 0)
							{
								Directory.Move(file.FullName, Path.Combine(Path.GetDirectoryName(file.FullName), name));
							}
							else
							{
								if (createFileName.Length > 0)
									createFileName = name;
								File.Move(file.FullName, Path.Combine(Path.GetDirectoryName(file.FullName), name));
							}
						}
						catch (Exception ex)
						{
							Logger.CoreError("Failed to rename file!", ex);
						}
					}
					ImGui.SetKeyboardFocusHere();
				}
				else
				{
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
				}

				ImGui.EndGroup();

				if (frame)
				{
					ImGui.PopStyleVar();
					ImGui.PopStyleColor();
				}

				if (ImGui.BeginPopupContextWindow(file.Name))
				{
					if (ImGui.BeginMenu("Create"))
					{
						DrawCreateMenu();
						ImGui.EndMenu();
					}

					ImGui.Separator();

					if (ImGui.MenuItem("Show in Explorer"))
					{
						new Process()
						{
							StartInfo = new ProcessStartInfo(activeFolderInfo.FullName)
							{
								UseShellExecute = true,
								CreateNoWindow = true
							}
						}.Start();
					}

					if (ImGui.MenuItem("Open"))
					{
						shouldOpen = true;
					}

					if (ImGui.MenuItem("Rename"))
					{
						renamingFile = true;
					}

					if (ImGui.MenuItem("Delete"))
					{
						deleteFile = selectedFile;
						promptDelete = true;
					}

					ImGui.EndPopup();
				}

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

				currentIndex++;
			}
		}

		private void UpdateFileContent()
		{
			Logger.Assert(File.Exists(Path.Combine(activeFolderInfo.FullName, createFileName)), "File does not exist!");
			switch (createFileType)
			{
				case FileType.Unknown:
				case FileType.Shader:
					if (!createFileName.EndsWith(".glsl"))
					{
						File.Move(Path.Combine(activeFolderInfo.FullName, createFileName), Path.Combine(activeFolderInfo.FullName, createFileName + ".glsl"));
						createFileName += ".glsl";
					}
					using (StreamWriter sw = File.CreateText(Path.Combine(activeFolderInfo.FullName, createFileName)))
					{
						sw.Write(EditorHelper.ShaderTemplate);
					}
					break;
				case FileType.Script:
					if (!createFileName.EndsWith(".cs"))
					{
						File.Move(Path.Combine(activeFolderInfo.FullName, createFileName), Path.Combine(activeFolderInfo.FullName, createFileName + ".cs"));
						createFileName += ".cs";
					}
					using (StreamWriter sw = File.CreateText(Path.Combine(activeFolderInfo.FullName, createFileName)))
					{
						sw.Write(EditorHelper.ScriptTemplate.Replace("SCRIPT_NAME", createFileName.Replace(' ', '_')[..createFileName.LastIndexOf('.')]));
					}
					break;
			}
			createFileName = "";
		}

		private void DrawAssetsDir(DirectoryInfo curDirInfo)
		{
			foreach (var child in curDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
			{
				if (child.GetDirectories("*", SearchOption.TopDirectoryOnly).Length > 0)
				{
					bool open = ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick);
					DragAndDropTargetFolder(child);

					if (ImGui.IsItemClicked())
					{
						activeFolder = Path.GetRelativePath(EditorConfig.AssetDirectory, child.FullName);
						activeFolderInfo = child;
						isSearching = false;
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

					DragAndDropTargetFolder(child);

					if (ImGui.IsItemClicked())
					{
						activeFolder = Path.GetRelativePath(EditorConfig.AssetDirectory, child.FullName);
						activeFolderInfo = child;
						isSearching = false;
					}
					if (open)
					{
						ImGui.TreePop();
					}
				}
			}
		}

		private void DragAndDropTargetFolder(DirectoryInfo targetFolder)
		{
			if (ImGui.BeginDragDropTarget())
			{
				if (!ImGui.IsMouseDown(0))
				{
					string resourcePath = Marshal.PtrToStringAuto(ImGui.GetDragDropPayload().Data);
					var info = new DirectoryInfo(resourcePath);
					try
					{
						if ((info.Attributes & FileAttributes.Directory) != 0)
						{
							if (Directory.Exists(info.FullName) && !Directory.Exists(Path.Combine(targetFolder.FullName, info.Name)))
							{
								Directory.Move(info.FullName, Path.Combine(targetFolder.FullName, info.Name));
								selectedFile = -1;
							}
						}
						else
						{
							if (File.Exists(info.FullName) && !File.Exists(Path.Combine(targetFolder.FullName, info.Name)))
							{
								File.Move(info.FullName, Path.Combine(targetFolder.FullName, info.Name));
								selectedFile = -1;
							}
						}
					}
					catch (Exception ex)
					{
						Logger.CoreError("Failed to rename file!", ex);
					}
				}

				ImGui.EndDragDropTarget();
			}
		}

		public void Run()
		{
			if (selectedFile != -1)
			{
				foreach (var kev in _keyPressedEvents)
				{
					var e = _keyPressedEvents.Get1(kev).Event;
					if (!e.Handled && e is KeyPressedEvent)
					{
						if (!renamingFile && e.KeyCode == KeyCodes.Right)
						{
							selectedFile++;
							if (selectedFile > itemCount)
								selectedFile = itemCount;
							break;
						}
						if (!renamingFile && e.KeyCode == KeyCodes.Left)
						{
							selectedFile--;
							if (selectedFile < 0)
								selectedFile = 0;
							break;
						}
						if (!renamingFile && e.KeyCode == KeyCodes.Up)
						{
							selectedFile -= maxItems + 1;
							if (selectedFile < 0)
								selectedFile = 0;
							break;
						}
						if (!renamingFile && e.KeyCode == KeyCodes.Down)
						{
							selectedFile += maxItems + 1;
							if (selectedFile > itemCount)
								selectedFile = itemCount;
							break;
						}

						if ((e.KeyCode == KeyCodes.Enter || e.KeyCode == KeyCodes.KeypadEnter))
						{
							if (renamingFile)
							{
								renamingFile = false;
								if (createFileName.Length > 0)
									UpdateFileContent();
							}
							else if (promptDelete)
							{
								promptDelete = false;
							}
							else if (isSearching)
							{
								isSearching = false;
							}
							else
							{
								shouldOpen = true;
							}
						}

						if (e.KeyCode == KeyCodes.Delete)
						{
							if (ImGui.GetIO().WantCaptureKeyboard)
								break;
							deleteFile = selectedFile;
							if (!InputManager.Instance.GetKeyDown(KeyCodes.ShiftLeft) && !InputManager.Instance.GetKeyDown(KeyCodes.ShiftRight))
								promptDelete = true;
							break;
						}

						if (e.KeyCode == KeyCodes.F2)
						{
							renamingFile = true;
						}

						if (e.KeyCode == KeyCodes.Esc)
						{
							if (!renamingFile && !promptDelete && deleteFile == -1)
								selectedFile = -1;
							if (renamingFile && createFileName.Length > 0)
								UpdateFileContent();
							renamingFile = false;
							deleteFile = -1;
							promptDelete = false;
						}
					}
				}
			}
		}

		private enum FileType
		{
			Unknown,
			Shader,
			Script
		}
	}
}
