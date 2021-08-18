# XFDraw Shadow Demo

![](https://github.com/theproadam/XFDraw/blob/main/Screenshots/deferred.png)

### Features
- Deferred rendering
- Phong Lightning
- Real-time Shadows
- Real-time Reflections
- Parallax
- Volumetric Fog
- Screen space ambient occlusion
- Two FXAA modes

### What is does not contain
- Tangent and Bitangent Mapping
- Screen space reflections

### What still needs to be done
- SSAO and Volumetric Fog needs to be rendered at 0.5x res as it is very expensive.

Without cubemap reflections the deferred pass should only take about 5ms:
![](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/screenshot1.png)
