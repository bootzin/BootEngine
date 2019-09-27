using BootEngine.Events;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Veldrid;

namespace BootEngine.Layers.GUI
{
    public class GuiLayer : LayerBase
    {
        private GraphicsDevice gd;
        private CommandList cl;
        private ImGuiController controller;

        public GuiLayer(GraphicsDevice gd, CommandList cl) : base("GUI Layer")
        {
            this.gd = gd;
            this.cl = cl;
        }

        public unsafe override void OnAttach()
        {
            ImGui.SetCurrentContext(ImGui.CreateContext());
            ImGui.StyleColorsDark();

            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
            io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;

            // TODO: should eventually use boot engine key system
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;

            controller = new ImGuiController(gd, gd.MainSwapchain.Framebuffer.OutputDescription, 1280, 720);
        }

        public override void OnDetach()
        {

        }

        public override void OnUpdate()
        {
            var t = true;
            ImGui.ShowDemoWindow(ref t);

            cl.Begin();
            cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
            //cl.ClearColorTarget(0, new RgbaFloat(0.45f, 0.55f, 0.6f, 1f));
            controller.Render(gd, cl);
            cl.End();
            gd.SubmitCommands(cl);
            gd.SwapBuffers(gd.MainSwapchain);
        }

        public override void OnEvent(EventBase @event)
        {

        }
    }
}
