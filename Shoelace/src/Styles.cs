using BootEngine.Utils;
using ImGuiNET;

namespace Shoelace
{
	internal static class Styles
	{
		public static void SetDarkTheme()
		{
			var colors = ImGui.GetStyle().Colors;

			// Text
			colors[(int)ImGuiCol.Text] = ColorF.White;
			colors[(int)ImGuiCol.TextDisabled] = new ColorF(0.50f, 0.50f, 0.50f, 1.00f);

			// Windows
			colors[(int)ImGuiCol.WindowBg] = new ColorF(0.11f, 0.11f, 0.11f, 1.00f);
			colors[(int)ImGuiCol.ChildBg] = new ColorF();
			colors[(int)ImGuiCol.PopupBg] = new ColorF(0.08f, 0.08f, 0.08f, 0.94f);
			colors[(int)ImGuiCol.Border] = new ColorF(0.63f, 0.63f, 0.63f, 0.50f);
			colors[(int)ImGuiCol.BorderShadow] = new ColorF();

			// Frame BG
			colors[(int)ImGuiCol.FrameBg] = ColorF.BackgroundGrey;
			colors[(int)ImGuiCol.FrameBgHovered] = ColorF.Grey;
			colors[(int)ImGuiCol.FrameBgActive] = new ColorF(0.38f, 0.38f, 0.38f, 0.75f);

			// Title BG
			colors[(int)ImGuiCol.TitleBg] = new ColorF(0.09f, 0.09f, 0.09f, 1.00f);
			colors[(int)ImGuiCol.TitleBgActive] = ColorF.LightGrey;
			colors[(int)ImGuiCol.TitleBgCollapsed] = ColorF.LightGrey;
			colors[(int)ImGuiCol.MenuBarBg] = new ColorF(0.10f, 0.10f, 0.10f, 1.00f);

			// Scroll
			colors[(int)ImGuiCol.ScrollbarBg] = new ColorF(0.07f, 0.07f, 0.07f, 0.53f);
			colors[(int)ImGuiCol.ScrollbarGrab] = new ColorF(0.59f, 0.59f, 0.59f, 0.50f);
			colors[(int)ImGuiCol.ScrollbarGrabHovered] = new ColorF(0.49f, 0.49f, 0.49f, 0.75f);
			colors[(int)ImGuiCol.ScrollbarGrabActive] = new ColorF(0.51f, 0.51f, 0.51f, 1.00f);

			// Checkmark and slider
			colors[(int)ImGuiCol.CheckMark] = ColorF.ActiveRed;
			colors[(int)ImGuiCol.SliderGrab] = new ColorF(0.53f, 0.53f, 0.53f, 1.00f);
			colors[(int)ImGuiCol.SliderGrabActive] = new ColorF(0.81f, 0.81f, 0.81f, 0.92f);

			// Buttons
			colors[(int)ImGuiCol.Button] = new ColorF(0.65f, 0.65f, 0.65f, 0.40f);
			colors[(int)ImGuiCol.ButtonHovered] = new ColorF(0.55f, 0.55f, 0.55f, 1.00f);
			colors[(int)ImGuiCol.ButtonActive] = new ColorF(0.82f, 0.82f, 0.82f, 1.00f);

			// Header
			colors[(int)ImGuiCol.Header] = ColorF.BackgroundGrey;
			colors[(int)ImGuiCol.HeaderHovered] = new ColorF(0.27f, 0.27f, 0.27f, 1.00f);
			colors[(int)ImGuiCol.HeaderActive] = new ColorF(0.32f, 0.32f, 0.32f, 1.00f);

			// Separator
			colors[(int)ImGuiCol.Separator] = new ColorF(0.39f, 0.39f, 0.39f, 0.50f);
			colors[(int)ImGuiCol.SeparatorHovered] = ColorF.HoverRed;
			colors[(int)ImGuiCol.SeparatorActive] = ColorF.ActiveRed;

			// Resize
			colors[(int)ImGuiCol.ResizeGrip] = new ColorF(1.00f, 1.00f, 1.00f, 0.25f);
			colors[(int)ImGuiCol.ResizeGripHovered] = new ColorF(1.00f, 1.00f, 1.00f, 0.53f);
			colors[(int)ImGuiCol.ResizeGripActive] = new ColorF(1.00f, 1.00f, 1.00f, 0.77f);

			// Tabs
			colors[(int)ImGuiCol.Tab] = ColorF.LightGrey;
			colors[(int)ImGuiCol.TabHovered] = ColorF.HoverRed;
			colors[(int)ImGuiCol.TabActive] = ColorF.ActiveRed;
			colors[(int)ImGuiCol.TabUnfocused] = new ColorF(0.20f, 0.20f, 0.20f, 1.00f);
			colors[(int)ImGuiCol.TabUnfocusedActive] = ColorF.LightGrey;

			// Docking
			colors[(int)ImGuiCol.DockingPreview] = ColorF.HoverRed;
			colors[(int)ImGuiCol.DockingEmptyBg] = new ColorF(0.17f, 0.17f, 0.17f, 1.00f);

			// Plotting
			colors[(int)ImGuiCol.PlotLines] = new ColorF(0.69f, 0.59f, 0.59f, 1.00f);
			colors[(int)ImGuiCol.PlotLinesHovered] = new ColorF(1.00f, 0.18f, 0.18f, 1.00f);
			colors[(int)ImGuiCol.PlotHistogram] = ColorF.HoverRed;
			colors[(int)ImGuiCol.PlotHistogramHovered] = new ColorF(0.87f, 0.61f, 0.61f, 1.00f);

			colors[(int)ImGuiCol.TextSelectedBg] = new ColorF(0.80f, 0.44f, 0.44f, 0.61f);
			colors[(int)ImGuiCol.DragDropTarget] = new ColorF(0.85f, 0.15f, 0.15f, 1.00f);
			colors[(int)ImGuiCol.NavHighlight] = new ColorF(0.85f, 0.15f, 0.15f, 1.00f);
			colors[(int)ImGuiCol.NavWindowingHighlight] = new ColorF(1.00f, 1.00f, 1.00f, 0.70f);
			colors[(int)ImGuiCol.NavWindowingDimBg] = new ColorF(0.80f, 0.80f, 0.80f, 0.20f);
			colors[(int)ImGuiCol.ModalWindowDimBg] = new ColorF(0.80f, 0.80f, 0.80f, 0.35f);
		}
	}
}
