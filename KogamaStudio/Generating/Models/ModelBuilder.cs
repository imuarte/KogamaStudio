using Il2Cpp;
using MelonLoader;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using System.Collections;

namespace KogamaStudio.Generating.Models
{
    internal class ModelBuilder
    {
        private static readonly int BatchSize = 500;
        private static readonly int FrameDelay = 1; // DEFAULT: 500

        internal static float Progress = 0.0f;
        internal static bool CancelGeneration = false;
        internal static bool IsBuilding = false;
        internal static IEnumerator Build(List<ModelCubeData> cubes)
        {
            if (IsBuilding)
            {
                MelonLogger.Error("[ModelBuilder] Generation already in progress!");
                yield break;
            }

            IsBuilding = true;

            try
            {
                MVCubeModelBase targetModel = null;

                try
                {
                    var desktopEditModeController = MVGameControllerBase.EditModeUI.Cast<DesktopEditModeController>();
                    var editorStateMachine = desktopEditModeController.EditModeStateMachine;
                    var cubeModelingStateMachine = editorStateMachine.cubeModelingStateMachine;
                    targetModel = cubeModelingStateMachine.TargetCubeModel;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[ModelBuilder] Error: {ex.Message}");
                }

                if (targetModel == null)
                {
                    MelonLogger.Error("[ModelBuilder] Target model does not exist!");
                    yield break;
                }

                int placedCube = 0;
                foreach (ModelCubeData cubeData in cubes)
                {
                    if (CancelGeneration)
                    {
                        CancelGeneration = false;
                        MelonLogger.Msg("[ModelBuilder] Generation canceled");
                        yield break;
                    }

                    if (targetModel == null)
                    {
                        MelonLogger.Error("[ModelBuilder] Target model was deleted!");
                        CancelGeneration = true;
                        yield break;
                    }

                    if (MVGameControllerBase.IsPlaying)
                    {
                        MelonLogger.Error("[ModelBuilder] Not in Edit mode!");
                        CancelGeneration = true;
                        yield break;
                    }

                    var cube = new Cube(
                        new Il2CppStructArray<byte>(cubeData.Corners),
                        new Il2CppStructArray<byte>(cubeData.Materials)
                    );

                    var position = new Il2CppMV.WorldObject.IntVector(cubeData.X, cubeData.Y, cubeData.Z);

                    if (targetModel.ContainsCube(position))
                    {
                        targetModel.RemoveCube(position);
                    }

                    targetModel.AddCube(position, cube);
                    placedCube++;
                    Progress = (float)placedCube / cubes.Count;

                    if (placedCube % BatchSize == 0)
                    {
                        PipeClient.SendCommand($"generate_progress|{Progress}");
                        targetModel.HandleDelta();
                        yield return new WaitForSecondsRealtime(1f / 60f * FrameDelay);
                    }


                }

                targetModel.HandleDelta();
                PipeClient.SendCommand("generate_progress|1.0");
                MelonLogger.Msg($"[ModelBuilder] Placed {cubes.Count} cubes");
            }
            finally
            {
                IsBuilding = false;
                Progress = 0.0f;
            }
        }
    }
}
