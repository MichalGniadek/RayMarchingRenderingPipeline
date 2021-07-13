# Ray Marching Rendering Pipeline
A custom scriptable rendering pipeline for Unity game engine, that instead of rendering polygons generates image from SDF scene description inside a custom compute shader.

## What are signed distance field functions
SDF function is a function that takes a point and returns a distance to the closes point in a scene. It can be used for rendering and while it's not as performant as standard rasterisation it has many advantages such as infinitely repeatable environments and smoothly merging different objects.

## How to use
You must create a Ray Marching Render Pipeline Asset. After that in the Inspector you can set the SDF Scene asset, the resolution at which it should be rendered (it will be upscaled to target resolution automatically, but lower resolution can be used for better performance) and number of ray marching steps.

### SDF Scene
SDF Scene's Inspector contains a custom shader editor (that can be seen below). You can use it to write a signed distance field function that will describe your scene. The shader must be written in HLSL. You can put a statement inside `@` to make it apply only in shading pass, saving performance.
The rendering pipeline includes some utility functions like `boxSD(float3 position, float3 size)` or `unlitMaterial(float4 material)`.
**skybox** should contain a function that describes the skybox color.
You can use buttons below to add additional parameters that can be changed in the inspector or from C# script. They will be updated automatically in the shader. Currently, the only supported types are float vectors.
Shader will recompile automatically after changes, but you can also force it with a button at the bottom.

## Showcase
![Menger](Showcase\menger.png)
![Code editor](Showcase\code_editor.png)