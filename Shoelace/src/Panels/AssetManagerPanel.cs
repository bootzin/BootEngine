using ImGuiNET;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Shoelace.Panels
{
	internal sealed class AssetManagerPanel : Panel
	{
		private string activeFolder = "Assets";
		private readonly DirectoryInfo AssetDirectoryInfo = new DirectoryInfo(EditorConfig.AssetDirectory);

		public override void OnGuiRender()
		{
			ImGui.Begin("Asset Manager");
			ImGui.Button("Create");
			ImGui.Separator();

			var width = ImGui.CalcItemWidth();
			ImGui.Columns(2, "##AssetColumns", true);
			ImGui.SetColumnWidth(0, width * .35f);

			ImGui.BeginChild("FileBrowser##1", new Vector2(0, ImGui.GetWindowHeight() * .71f));

			if (ImGui.TreeNodeEx("Assets", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Selected))
			{
				if (ImGui.IsItemClicked())
				{
					activeFolder = "Assets";
				}
				DrawAssetsDir(AssetDirectoryInfo);
				ImGui.TreePop();
			}
			ImGui.EndChild();

			ImGui.NextColumn();

			ImGui.BeginChild("ContentBrowser##1", new Vector2(0, ImGui.GetWindowHeight() * .71f));
			ImGui.Text(activeFolder);
			ImGui.Separator();
			ImGui.EndChild();

			ImGui.End();
		}

		private void DrawAssetsDir(DirectoryInfo curDirInfo)
		{
			foreach (var child in curDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
			{
				if (child.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToList().Count > 0)
				{
					bool open = ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.SpanFullWidth);
					if (ImGui.IsItemClicked())
					{
						activeFolder = Path.GetRelativePath(EditorConfig.AssetDirectory, child.FullName);
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
