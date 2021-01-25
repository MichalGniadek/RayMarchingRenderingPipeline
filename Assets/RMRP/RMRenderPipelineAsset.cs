using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Ray Marching Render Pipeline")]
public class RMRenderPipelineAsset : RenderPipelineAsset
{
    public SDFScene scene = null;
    public Action updateDynamicParametersAction = null;
    RMRenderPipeline pipeline = null;

    protected override RenderPipeline CreatePipeline()
    {
        pipeline = new RMRenderPipeline(scene, updateDynamicParametersAction);
        return pipeline;
    }

    override protected void OnValidate()
    {
        scene.Compile();
        if (pipeline != null) pipeline.SetScene(scene);
    }
}
