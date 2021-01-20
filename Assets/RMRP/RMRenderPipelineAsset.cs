using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Ray Marching Render Pipeline")]
public class RMRenderPipelineAsset : RenderPipelineAsset
{
    public ComputeShader shader = null;
    public Action updateDynamicParametersAction = null;

    protected override RenderPipeline CreatePipeline()
    {
        return new RMRenderPipeline(shader, updateDynamicParametersAction);
    }
}
