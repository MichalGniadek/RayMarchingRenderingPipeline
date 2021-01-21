using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Ray Marching Render Pipeline")]
public class RMRenderPipelineAsset : RenderPipelineAsset
{
    public SDFScene scene = null;
    public Action updateDynamicParametersAction = null;

    protected override RenderPipeline CreatePipeline()
    {
        return new RMRenderPipeline(scene, updateDynamicParametersAction);
    }
}
