using System;
using UnityEngine;
using UnityEngine.Rendering;

public class RMRenderPipeline : RenderPipeline
{
    CommandBuffer buffer = new CommandBuffer { name = "Ray Marching" };
    RenderTexture outputTexture = null;

    SDFScene scene;
    Action updateDynamicParameters = null;
    RayMarchingSettings settings = null;

    public RMRenderPipeline(SDFScene scene, Action updateDynamicParameters, RayMarchingSettings settings)
    {
        this.settings = settings;
        SetScene(scene);
        this.updateDynamicParameters = updateDynamicParameters;
    }

    public void SetScene(SDFScene scene)
    {
        this.scene = scene;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        updateDynamicParameters?.Invoke();

        foreach (Camera camera in cameras)
        {
            Setup(camera);

            BufferCommands(camera);
            context.ExecuteCommandBuffer(buffer);
            context.Submit();

            Cleanup();
        }
    }

    void Setup(Camera camera)
    {
        outputTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        buffer.SetComputeTextureParam(scene.SDFShader, 0, "output", outputTexture);
        // I think this only applies to xbone
        buffer.SwitchIntoFastMemory(outputTexture, FastMemoryFlags.SpillBottom, 1f, false);
        buffer.SetComputeMatrixParam(scene.SDFShader, "camera_to_world", camera.cameraToWorldMatrix);
        buffer.SetComputeMatrixParam(scene.SDFShader, "inverse_projection", camera.projectionMatrix.inverse);
    }

    void BufferCommands(Camera camera)
    {
        buffer.BeginSample("Render " + camera.name);

        scene.UpdateBuffersData(buffer, scene.SDFShader);

        int threadGroupsX = Mathf.CeilToInt(camera.pixelWidth * settings.resolution / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(camera.pixelHeight * settings.resolution / 8.0f);
        buffer.DispatchCompute(scene.SDFShader, 0, threadGroupsX, threadGroupsY, 1);

        buffer.Blit(outputTexture, camera.targetTexture,
                Vector2.one * settings.resolution, Vector2.zero);

        buffer.EndSample("Render " + camera.name);
    }

    void Cleanup()
    {
        buffer.Clear();
        outputTexture.Release();
    }
}
