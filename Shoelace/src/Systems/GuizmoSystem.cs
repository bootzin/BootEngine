using BootEngine.ECS.Components;
using BootEngine.Input;
using BootEngine.Utils;
using ImGuiNET;
using Leopotam.Ecs;
using Shoelace.Services;
using System.Numerics;

namespace Shoelace.Systems
{
	internal sealed class GuizmoSystem : IEcsSystem
	{
		private readonly GuiService _guiService = default;
		private readonly EcsFilter<CameraComponent, TransformComponent> _cameras = default;

		// TODO: Consider making my own gizmo library, ImGuizmo.NET is glitchy as hell
		public void ProcessGizmos()
		{
			var selected = _guiService.SelectedEntity;
			if (selected != default && selected.Has<TransformComponent>())
			{
				foreach (var camera in _cameras)
				{
					ref var cam = ref _cameras.Get1(camera);
					if (cam.Camera.Active)
					{
						ImGuizmo.SetID(camera);
						ImGuizmo.SetOrthographic(cam.Camera.ProjectionType == BootEngine.Renderer.Cameras.ProjectionType.Orthographic);
						ImGuizmo.SetDrawlist(ImGui.GetWindowDrawList());

						float windowWidth = ImGui.GetWindowWidth();
						float windowHeight = ImGui.GetWindowHeight();
						ImGuizmo.SetRect(ImGui.GetWindowPos().X, ImGui.GetWindowPos().Y, windowWidth, windowHeight);

						bool snap = InputManager.Instance.GetKeyDown(KeyCodes.LControl);
						float snapValue = .1f;

						if (_guiService.GizmoType == OPERATION.ROTATE)
							snapValue = 45f;

						float[] snapValues = new float[] { snapValue, snapValue, snapValue };

						ref var tc = ref selected.Get<TransformComponent>();

						var rotTmp = MathUtil.Rad2Deg(tc.Rotation);
						float[] pos = new float[] { tc.Position.X, tc.Position.Y, tc.Position.Z };
						float[] rot = new float[] { rotTmp.X, rotTmp.Y, rotTmp.Z };
						float[] sca = new float[] { tc.Scale.X, tc.Scale.Y, tc.Scale.Z };
						float[] transform = new float[16];

						ImGuizmo.RecomposeMatrixFromComponents(ref pos[0], ref rot[0], ref sca[0], ref transform[0]);

						// using the component's transform breaks guizmos since the decomposition 
						// expects angle in degree, and transform uses radians
						// TODO: either change decomposition (preferred) or change transform to use degrees
						//transform = tc.Transform.ToFloatArray();

						ref var cameraTc = ref _cameras.Get2(camera);
						Matrix4x4.Invert(cameraTc.Transform, out Matrix4x4 cameraViewMat);
						float[] cameraView = cameraViewMat.ToFloatArray();
						float[] cameraProj;
						if (cam.Camera.SwapYAxis)
						{
							cameraProj = (cam.Camera.ProjectionMatrix * new Matrix4x4(
													1, 0, 0, 0,
													0, -1, 0, 0,
													0, 0, 1, 0,
													0, 0, 0, 1)).ToFloatArray();
						}
						else
						{
							cameraProj = cam.Camera.ProjectionMatrix.ToFloatArray();
						}
						float[] deltaTransform = new float[16];

						if (snap)
							ImGuizmo.Manipulate(ref cameraView[0], ref cameraProj[0], _guiService.GizmoType, MODE.LOCAL, ref transform[0], ref deltaTransform[0], ref snapValues[0]);
						else
							ImGuizmo.Manipulate(ref cameraView[0], ref cameraProj[0], _guiService.GizmoType, MODE.LOCAL, ref transform[0], ref deltaTransform[0]);

						if (ImGuizmo.IsOver() && ImGuizmo.IsUsing())
						{
							float[] translation = new float[3];
							float[] rotation = new float[3];
							float[] scale = new float[3];

							ImGuizmo.DecomposeMatrixToComponents(ref deltaTransform[0], ref translation[0], ref rotation[0], ref scale[0]);

							switch (_guiService.GizmoType)
							{
								case OPERATION.TRANSLATE:
									tc.Position += new Vector3(translation[0], translation[1], translation[2]);
									break;
								case OPERATION.ROTATE:
									tc.Rotation -= MathUtil.Deg2Rad(new Vector3(rotation[0], rotation[1], rotation[2]));
									break;
								case OPERATION.SCALE:
									tc.Scale += new Vector3(scale[0], scale[1], scale[2]);
									break;
							}
						}
					}
				}
			}
		}
	}
}
