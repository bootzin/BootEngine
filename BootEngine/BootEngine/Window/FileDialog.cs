using BootEngine.Utils;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace BootEngine.Window
{
	public sealed class FileDialog
	{
		private int selectedFolderIndex;
		private int selectedFileIndex;
		private string curPath = Directory.GetCurrentDirectory();
		private string curFile = "";
		private string curFolder = "";
		private bool sortAscending = true;
		private SortType sortType;

		private float initialSpacingCol0 = 230f;
		private float initialSpacingCol1 = 80f;
		private float initialSpacingCol2 = 80f;

		private string newFolderName = "";
		private string newFolderError = "";
		private string dialogError = "";
		private bool doAction;

		public bool ShowFileDialog(ref bool isOpen, out string path, DialogType type = DialogType.Open, string filter = "")
		{
			path = null;
			if (isOpen)
			{
				var style = ImGui.GetStyle();
				Vector2 minWinSize = style.WindowMinSize;
				style.WindowMinSize = new Vector2(500, 396);

				var windowSize = new Vector2(756, 420);
				ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);
				var mainViewport = ImGui.GetMainViewport();
				ImGui.SetNextWindowPos(mainViewport.Pos + mainViewport.Size / 2f - windowSize / 2f, ImGuiCond.Appearing);
				ImGui.Begin("Select a file", ref isOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking);

				var curDirInfo = new DirectoryInfo(curPath);
				var directories = curDirInfo.GetDirectories();
				List<FileInfo> files;
				if (!string.IsNullOrWhiteSpace(filter))
					files = curDirInfo.GetFiles(filter).ToList();
				else
					files = curDirInfo.GetFiles().ToList();

				ImGui.Text(curPath);

				ImGui.BeginChild("Directories##1", new Vector2(.27f * ImGui.GetWindowWidth(), .71f * ImGui.GetWindowHeight()), true, ImGuiWindowFlags.HorizontalScrollbar);

				if (ImGui.Selectable("..", false, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(ImGui.GetWindowContentRegionWidth(), 0))
					&& ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					curPath = Directory.GetParent(curPath).FullName;
				}

				for (int i = 0; i < directories.Length; i++)
				{
					if (ImGui.Selectable(directories[i].Name, i == selectedFolderIndex, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(ImGui.GetWindowContentRegionWidth(), 0)))
					{
						curFile = "";
						if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
						{
							curPath = directories[i].FullName;
							selectedFileIndex = selectedFolderIndex = 0;
							ImGui.SetScrollHereY(0f);
							curFolder = "";
						}
						else
						{
							selectedFolderIndex = i;
							curFolder = directories[i].Name;
						}
					}
				}

				ImGui.EndChild(); // Directories

				ImGui.SameLine();

				ImGui.BeginChild("Files##1", new Vector2(.68f * ImGui.GetWindowWidth(), .71f * ImGui.GetWindowHeight()), true, ImGuiWindowFlags.HorizontalScrollbar);
				ImGui.Columns(4);
				if (initialSpacingCol0 > 0)
				{
					ImGui.SetColumnWidth(0, initialSpacingCol0);
					initialSpacingCol0 = 0;
				}
				if (initialSpacingCol1 > 0)
				{
					ImGui.SetColumnWidth(1, initialSpacingCol1);
					initialSpacingCol1 = 0;
				}
				if (initialSpacingCol2 > 0)
				{
					ImGui.SetColumnWidth(2, initialSpacingCol2);
					initialSpacingCol2 = 0;
				}

				if (ImGui.Selectable("File"))
				{
					sortType = SortType.FileName;
					sortAscending = !sortAscending;
				}
				ImGui.NextColumn();

				if (ImGui.Selectable("Size"))
				{
					sortType = SortType.Size;
					sortAscending = !sortAscending;
				}
				ImGui.NextColumn();

				if (ImGui.Selectable("Type"))
				{
					sortType = SortType.Type;
					sortAscending = !sortAscending;
				}
				ImGui.NextColumn();

				if (ImGui.Selectable("Date"))
				{
					sortType = SortType.Date;
					sortAscending = !sortAscending;
				}
				ImGui.NextColumn();
				ImGui.Separator();

				if (sortAscending)
				{
					switch (sortType)
					{
						case SortType.FileName:
							files = files.OrderBy(file => file.Name).ToList();
							break;
						case SortType.Size:
							files = files.OrderBy(file => file.Length).ToList();
							break;
						case SortType.Type:
							files = files.OrderBy(file => file.Extension).ToList();
							break;
						case SortType.Date:
							files = files.OrderBy(file => file.LastWriteTime).ToList();
							break;
					}
				}
				else
				{
					switch (sortType)
					{
						case SortType.FileName:
							files = files.OrderByDescending(file => file.Name).ToList();
							break;
						case SortType.Size:
							files = files.OrderByDescending(file => file.Length).ToList();
							break;
						case SortType.Type:
							files = files.OrderByDescending(file => file.Extension).ToList();
							break;
						case SortType.Date:
							files = files.OrderByDescending(file => file.LastWriteTime).ToList();
							break;
					}
				}

				for (int i = 0; i < files.Count; i++)
				{
					if (ImGui.Selectable(files[i].Name, i == selectedFileIndex, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(ImGui.GetWindowContentRegionWidth(), 0)))
					{
						selectedFileIndex = i;
						curFile = files[i].Name;
						curFolder = "";
						if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
						{
							doAction = true;
						}
					}

					ImGui.NextColumn();
					ImGui.TextUnformatted(files[i].Length.ToString());
					ImGui.NextColumn();
					ImGui.TextUnformatted(files[i].Extension);
					ImGui.NextColumn();
					ImGui.TextUnformatted(files[i].LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
					ImGui.NextColumn();
				}
				ImGui.EndChild(); // Files

				string selectedFilePath = curPath + (curPath[^1] == '\\' ? "" : "\\") + (curFolder.Length > 0 ? curFolder : curFile);
				ImGui.PushItemWidth(.95f * ImGui.GetWindowWidth());

				if (ImGui.InputText("", ref selectedFilePath, 500, ImGuiInputTextFlags.EnterReturnsTrue))
				{
					if (type != DialogType.SelectFolder)
					{
						curFile = selectedFilePath.Substring(selectedFilePath.LastIndexOf('\\'));
					}
					else
					{
						curFolder = selectedFilePath.Substring(selectedFilePath.LastIndexOf('\\'));
					}
					doAction = true;
				}

				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);

				if (ImGui.Button("New Folder"))
				{
					ImGui.OpenPopup("NewFolderPopup");
				}
				ImGui.SameLine();

				if (curFolder?.Length == 0)
				{
					ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
					ImGui.Button("Delete Folder");
					ImGui.PopStyleVar();
				}
				else if (ImGui.Button("Delete Folder"))
				{
					ImGui.OpenPopup("DeleteFolderPopup");
				}

				Vector2 center = new Vector2(ImGui.GetWindowPos().X + ImGui.GetWindowSize().X * 0.5f, ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y * 0.5f);
				ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(.5f, .5f));
				if (ImGui.BeginPopup("NewFolderPopup", ImGuiWindowFlags.Modal))
				{
					ImGui.Text("Enter a name for the new folder");
					ImGui.InputText("##FolderName", ref newFolderName, 500u);
					if (ImGui.Button("Create##1"))
					{
						if (string.IsNullOrWhiteSpace(newFolderName))
						{
							newFolderError = "Folder name can't be empty!";
						}
						else
						{
							string newFilePath = curPath + (curPath[^1] == '\\' ? "" : "\\") + newFolderName;
							Directory.CreateDirectory(newFilePath);
							newFolderName = "";
							ImGui.CloseCurrentPopup();
						}
					}
					ImGui.SameLine();
					if (ImGui.Button("Cancel##1"))
					{
						newFolderError = "";
						ImGui.CloseCurrentPopup();
					}
					ImGui.TextColored(ColorF.Red, newFolderError);
					ImGui.EndPopup();
				}

				ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(.5f, .5f));
				if (ImGui.BeginPopup("DeleteFolderPopup", ImGuiWindowFlags.Modal))
				{
					ImGui.TextColored(ColorF.Red, "Are you sure you want to delete this folder?");
					ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);
					ImGui.TextUnformatted(curFolder);
					ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);
					if (ImGui.Button("Yes"))
					{
						Directory.Delete(curPath + (curPath[^1] == '\\' ? "" : "\\") + curFolder);
						curFolder = "";
						ImGui.CloseCurrentPopup();
					}
					ImGui.SameLine();
					if (ImGui.Button("No"))
					{
						ImGui.CloseCurrentPopup();
					}
					ImGui.EndPopup();
				}
				ImGui.SameLine();
				ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 134);

				if (ImGui.Button("Cancel"))
				{
					selectedFileIndex = selectedFolderIndex = 0;
					curFile = "";
					isOpen = false;
					return false;
				}
				ImGui.SameLine();

				if (type == DialogType.SelectFolder && (ImGui.Button("Choose") || doAction))
				{
					doAction = false;
					if (curFolder.Length == 0)
					{
						dialogError = "Error: You must select a folder!";
					}
					else
					{
						path = curPath + (curPath[^1] == '\\' ? "" : "\\") + curFolder;
						selectedFileIndex = selectedFolderIndex = 0;
						curFile = "";
						isOpen = false;
						return true;
					}
				}
				else if (type == DialogType.Open && (ImGui.Button("Open") || doAction))
				{
					doAction = false;
					if (curFile.Length == 0)
					{
						dialogError = "Please select a file to open!";
					}
					else
					{
						path = curPath + (curPath[^1] == '\\' ? "" : "\\") + curFile;
						selectedFileIndex = selectedFolderIndex = 0;
						curFile = "";
						isOpen = false;
						return true;
					}
				}
				else if (type == DialogType.Save && (ImGui.Button("Save") || doAction))
				{
					doAction = false;
					if (curFile.Length == 0)
					{
						dialogError = "Invalid file name!";
					}
					else
					{
						if (!File.Exists(curPath + (curPath[^1] == '\\' ? "" : "\\") + curFile))
						{
							path = curPath + (curPath[^1] == '\\' ? "" : "\\") + curFile;
							selectedFileIndex = selectedFolderIndex = 0;
							curFile = "";
							isOpen = false;
							return true;
						}
						else
						{
							ImGui.OpenPopup("OverwriteFilePopup");
						}
					}
				}

				ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(.5f, .5f));
				if (ImGui.BeginPopup("OverwriteFilePopup", ImGuiWindowFlags.Modal))
				{
					ImGui.TextColored(ColorF.Red, "Are you sure you want to overwrite this file?");
					ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);
					ImGui.TextUnformatted(curFile);
					ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);
					if (ImGui.Button("Yes"))
					{
						path = curPath + (curPath[^1] == '\\' ? "" : "\\") + curFile;
						selectedFileIndex = selectedFolderIndex = 0;
						curFile = "";
						isOpen = false;
						ImGui.CloseCurrentPopup();
						return true;
					}
					ImGui.SameLine();
					if (ImGui.Button("No"))
					{
						ImGui.CloseCurrentPopup();
					}
					ImGui.EndPopup();
				}

				if (dialogError.Length > 0)
					ImGui.TextColored(ColorF.Red, dialogError);

				ImGui.End();
				style.WindowMinSize = minWinSize;
			}
			return false;
		}

		public enum DialogType
		{
			Open,
			Save,
			SelectFolder
		}

		private enum SortType
		{
			FileName,
			Size,
			Type,
			Date,
		}
	}
}
