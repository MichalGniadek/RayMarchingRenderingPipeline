using System;
using UnityEngine;
using UnityEngine.Rendering;

public class RMRenderPipeline : RenderPipeline
{
    CommandBuffer buffer = new CommandBuffer { name = "Ray Marching" };
    RenderTexture outputTexture = null;

    ComputeShader shader;
    SDFScene scene;
    Action updateDynamicParameters = null;

    public RMRenderPipeline(SDFScene scene, Action updateDynamicParameters)
    {
        this.shader = scene.LoadShader();
        this.scene = scene;
        this.updateDynamicParameters = updateDynamicParameters;
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

        buffer.SetComputeTextureParam(shader, 0, "output", outputTexture);
        buffer.SetComputeMatrixParam(shader, "camera_to_world", camera.cameraToWorldMatrix);
        buffer.SetComputeMatrixParam(shader, "inverse_projection", camera.projectionMatrix.inverse);
    }

    void BufferCommands(Camera camera)
    {
        buffer.BeginSample("Render " + camera.name);

        scene.UpdateBuffers(buffer, shader);

        int threadGroupsX = Mathf.CeilToInt(camera.pixelWidth / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(camera.pixelHeight / 32.0f);
        buffer.DispatchCompute(shader, 0, threadGroupsX, threadGroupsY, 1);

        buffer.Blit(outputTexture, camera.targetTexture);

        buffer.EndSample("Render " + camera.name);
    }

    void Cleanup()
    {
        buffer.Clear();
        outputTexture.Release();
    }
}
