## XFDraw
![](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/xfban.png)

XFDraw is a realtime, high performance, software renderer written in C++ controlled via a wrapper written in C#. It's shaders are written in C++, however they are nearly identical to the GLSL language.

Just like renderXF, XFDraw has been also designed to be as simple and user friendly as possible. Because of this most of the code is composed of super simple commands, and buffer, shader and framebuffer initialization is also very simple. There is also a ![Wiki](https://github.com/theproadam/XFDraw/wiki) available to help with a quick start.

###### Currently XFDraw is still WIP. v0.5.0 should be the first stable release.

### Features
- Fully programmable fragment shader
- Programmable vertex shader (Projection is done internally)
- Hardcoded performance features (FXAA, Vignette, Draw Skybox)
- Dedicated programmable high performance screenspace shaders
- Tangent, Bitangent normal mapping supported (demo included)
- Parallax, shadow mapping and volumetric fog supported (demo included)
- Screenspace and cubemap reflections supported
- Advanced buffer critical and non-critical locking system
- Multithreaded rendering support (Create multiple shaders from one dll)
- MSAA Support (Triangles have gaps but its there)
- Direct blit (No bitmaps required)
- GDI+ Interoperability (blit bitmaps onto the drawbuffer)
- GLSL like shader code
- Correct perspective interpolation
- Perspective, Orthographic and mixed modes supported
- Built in safety features, *should* be impossible to segfault
- Textures support nearest and bilinear interpolation, with wrapping modes

### Screenshots
#### Phong Example. 926 Triangles ~1.2ms (>20ms if the viewport is filled due to forward rendering)
![Phong Shader Demo](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/TeapotPhong.png)

#### Skybox + Cubemap Example ~3ms (>30ms if the viewport is filled due to forward rendering)
![Phong Shader Demo](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/TeapoReflections.png)

#### XFDraw included demo. Parallax Mapping + Volumetric Fog + SSAO + FXAA + Deferred rendering
![XFDemo](https://github.com/theproadam/XFDraw/blob/main/Screenshots/Teapots.png)

#### Skybox + Cubemap + Screenspace Reflection ~35ms (50 ray count + inefficient SSR shader)
![SSR Demo](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/TeapotScreenSpace.png)

#### Normal Example. 926 Triangles ~0.4ms
![Depth Fill Demo](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/TeapotNormals.png)

#### Depth Fill. 350k Triangles ~3.3ms
![Depth Fill Demo](https://i.imgur.com/OlIJDbv.png)

#### Shadow mapping support. (Not Optimized Yet)
![Shadow Mapping](https://cdn.discordapp.com/attachments/545669301164703754/862901922033565696/unknown.png)

#### Shadow mapping example. (Not Optimized Yet)
![Shadow Mapping](https://cdn.discordapp.com/attachments/545669301164703754/863470567185055784/unknown.png)

#### Parallax mapping example. (Not Optimized Yet)
![Parallax](https://raw.githubusercontent.com/theproadam/XFDraw/main/Screenshots/ParallaxBeta.png)



#### Vignette Shader. ~0.6ms (in 1080p, not shown)
![Screenspace Shaders Example](https://i.imgur.com/gBNrAQr.png)

### Example Shader Code
Vertex shader: `teapotVS.cpp`:
```glsl
//version 330 Core
layout(location = 0) in vec3 pos;
layout(location = 1) in vec3 norm;

out vec3 norm_data;
out vec3 frag_pos;

uniform vec3 cameraPos;
uniform mat3 cameraRot;

void main()
{
	gl_Position = cameraRot * (pos - cameraPos); //projection is handled internally
	norm_data = norm;
	frag_pos = pos;
}
```
Fragment shader: `teapotFS.cpp`:
```glsl
out byte4 FragColor;

in vec3 norm_data;
in vec3 frag_pos;

uniform samplerCube skybox;
uniform vec3 camera_Pos;

void main()
{
	vec3 I = (frag_pos - camera_Pos);
	vec3 R = reflect(I, norm_data);
	
	FragColor = texture(skybox, R);
}

```

C# Side:
```c#
//Parse and compile the shader ->
ShaderCompile sModule = ShaderParser.Parse("teapotVS.cpp", "teapotFS.cpp", "teapot");
Shader teapotShader; sModule.Compile(out teapotShader);

//Prepare framebuffers, vertex buffers and projection->
GLTexture colorBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Color4));
GLTexture depthBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));
GLBuffer teapotObject = new GLBuffer(floatArrayThatHasXYZ_IJK_Normals, 6); //6=stride
GLMatrix projMatrix = GLMatrix.Perspective(90f, viewportWidth, viewportHeight);

//Assign uniforms and link buffers ->
teapotShader.AssignBuffer("FragColor", colorBuffer);
teapotShader.SetValue("skybox", skybox);
teapotShader.SetValue("cameraRot", rotationalMatrix);
teapotShader.SetValue("cameraPos", cameraPosition);

//When done, draw the object
GL.Draw(teapotObject, teapotShader, depthBuffer, projMatrix, GLMode.Triangle);
```
Compiling is done via `cl.exe`, however you can always compile your code manually. Expect a wiki section on how to do this soon.
