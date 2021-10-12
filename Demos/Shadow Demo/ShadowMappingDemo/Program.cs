using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using xfcore;
using xfcore.Shaders;
using xfcore.Buffers;
using xfcore.Extras;
using xfcore.Performance;
using xfcore.Debug;
using xfcore.Info;
using xfcore.Shaders.Builder;

namespace ShadowMappingDemo
{
    class Program
    {
        //Color and Depth framebuffer
        static GLTexture colorBuffer;
        static GLTexture depthBuffer;


        //G-BUFFER
        static GLTexture worldBuffer;
        static GLTexture diffuseBuffer;
        static GLTexture normalBuffer;
        static GLTexture specularBuffer;

        //Lightning Data
        static GLTexture shadowMap;
        static GLMatrix shadowProj;
        static Matrix3x3 shadowRot;
        static float shadowBias = 0.5f;
        static GLTexture noiseMap;

        //Cubemap reflections
        static Shader basicShaderCubemap;
        static Shader planeShaderCubemap;

        //saves 1-2ms by not requiring distSquared
        static GLTexture reflectionIndex;
        static GLCubemap reflect1;
        static GLCubemap reflect2;
        static GLCubemap reflect3;
        static GLCubemap reflect4;

        static GLCubemap skybox;

        //Object Buffer
        static GLBuffer planeObject;
        static GLBuffer teapotObject;
        static GLBuffer windowObject;

        //Texture Buffers
        static GLTexture cobbleDiffuse;
        static GLTexture cobbleNormal;
        static GLTexture cobbleHeight;
        static GLTexture cobbleSpecular;
        static float heightScale = -0.2f;

        //Shaders
        static Shader basicShader;
        static Shader lightShader;
        static Shader basicShaderShadows;
        static Shader planeShader;
        static Shader planeShaderShadows;

        //Post-FX shaders
        static Shader volumetricFog;
        static Shader SSAO;
        static Shader FXAA;
        static bool enableFXAA = false;

        //Pre FXAA Buffer
        static GLTexture fxaaBuffer;

        static float ssao_range = 5.5f;//1.0f;
        static float ssao_bias = 2.82f;//1.0f;
        static float ssao_power = 4.0f;

        static GLTexture ssaoBuffer;
        static GLBuffer ssaoSamples;
        static GLTexture ssaoNoise;

        //Form data
        static Form renderForm;
        static BlitData formData;
        static Bitmap infoBitmap = new Bitmap(400, 400);

        //Threads and rendering
        static RenderThread RT;
        static InputManager inputManager;

        //Used for delta time compensation
        static Stopwatch sw = new Stopwatch();
        static Stopwatch deltaTime = new Stopwatch();

        //Viewport Size
        static int viewportWidth = 1600, viewportHeight = 900;
        static int FramesRendered = 0;

        static int LastFPS = 0;
        static float lastDeltaTime = 0;
        static List<string> frametimeCount = new List<string>();
        static int screenshot = 0;

        static void Main(string[] args)
        {
            //Create the form and ready some of its properties
            renderForm = new Form();
            renderForm.Text = "Game Window";
            renderForm.ClientSize = new Size(viewportWidth, viewportHeight);
            renderForm.StartPosition = FormStartPosition.CenterScreen;
            renderForm.SizeChanged += renderForm_SizeChanged;
            renderForm.KeyDown += renderForm_KeyDown;

            //Create the blit information to allow image drawing on the form and ready the input manager
            formData = new BlitData(renderForm);
            inputManager = new InputManager(renderForm);

            //Initializing each framebuffer
            colorBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Color4));
            depthBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));
            diffuseBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Color4));
            worldBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Vector3));
            normalBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Vector3));
            specularBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));

            //Reflections
            reflectionIndex = new GLTexture(viewportWidth, viewportHeight, typeof(int));

            //FXAA
            fxaaBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Color4));
          

            //Initialize the render thread
            RT = new RenderThread(144);
            RT.RenderFrame += RT_RenderFrame;

            //Load objects, average normals and create texture plane
            teapotObject = ObjectLoader.ImportSTL("models/teapot.stl", 57.0f);
            planeObject = ObjectLoader.CreatePlaneUV(180, 18);
            windowObject = ObjectLoader.ImportSTL("models/window.stl", 57.0f);
            cobbleDiffuse = ObjectLoader.LoadTexture("textures/Cobblestone [Albedo].png");
            cobbleNormal = ObjectLoader.LoadTexture("textures/Cobblestone [Normal].png");
            cobbleHeight = ObjectLoader.LoadTexture("textures/Cobblestone [Occlusion].png");
            cobbleSpecular = ObjectLoader.LoadTexture("textures/Cobblestone [Specular].png");
            skybox = ObjectLoader.LoadCubemap("textures/skybox", false);


            //Parse and Compile Shaders
            basicShader = LoadShader("basicVS.cpp", "basicFS.cpp", "basicShader");
            basicShaderShadows = LoadShader("basicVS.cpp", "basicFS_shadow.cpp", "basicShaderShadow");
            lightShader = LoadShader("deferred_pass.cpp", "lightning");
            planeShader = LoadShader("planeVS.cpp", "planeFS.cpp", "planeShader");
            planeShaderShadows = LoadShader("planeVS.cpp", "planeFS_shadow.cpp", "planeShaderShadow");      
            volumetricFog = LoadShader("volumetric_fog.cpp", "volumetricfog");
            SSAO = LoadShader("ssao.cpp", "ssao_pass");
            FXAA = LoadShader("fxaa.cpp", "fxaa_pass");
           // blurSSAO = LoadShader("ssao_blur.cpp", "ssao_blurpass");

            //Reflections
            basicShaderCubemap = LoadShader("basicVS.cpp", "basicFS_reflection.cpp", "basicShaderReflect");
            planeShaderCubemap = LoadShader("planeVS.cpp", "planeFS_reflection.cpp", "planeShaderReflect");
            
            //Link framebuffers to shaders and configure shaders
            basicShader.AssignBuffer("diffuse", diffuseBuffer);
            basicShader.AssignBuffer("normal", normalBuffer);
            basicShader.AssignBuffer("world_pos", worldBuffer);
            basicShader.AssignBuffer("specular", specularBuffer);
            basicShader.AssignBuffer("reflection_index", reflectionIndex);

            basicShader.ConfigureFaceCulling(GLCull.GL_BACK);

            lightShader.AssignBuffer("FragColor", fxaaBuffer);
            lightShader.AssignBuffer("pos", worldBuffer);
            lightShader.AssignBuffer("norm", normalBuffer);
            lightShader.AssignBuffer("objectColor", diffuseBuffer);
            lightShader.AssignBuffer("spec_power", specularBuffer);
            lightShader.AssignBuffer("reflection_index", reflectionIndex);

            planeShader.AssignBuffer("diffuse", diffuseBuffer);
            planeShader.AssignBuffer("normal", normalBuffer);
            planeShader.AssignBuffer("world_pos", worldBuffer);
            planeShader.AssignBuffer("specular", specularBuffer);

            planeShader.SetValue("albedoTexture", cobbleDiffuse);
            planeShader.SetValue("normalTexture", cobbleNormal);
            planeShader.SetValue("heightTexture", cobbleHeight);
            planeShader.SetValue("speculTexture", cobbleSpecular);


            planeShader.SetValue("textureSize", new Vector2(cobbleDiffuse.Width, cobbleDiffuse.Height));

            //Create Shadow Map
            shadowMap = new GLTexture(2048, 2048, typeof(float));
            shadowRot = Matrix3x3.RollMatrix(-15.875f) * Matrix3x3.PitchMatrix(180.0f);
            shadowProj = GLMatrix.Orthographic(100f, shadowMap.Width, shadowMap.Height);
           // shadowProj = GLMatrix.Perspective(90f, shadowMap.Width, shadowMap.Height);
            shadowProj.SetViewportSize(shadowMap.Width, shadowMap.Height);
            shadowProj.ZFar = 10000;

            basicShaderShadows.AssignBuffer("depth", shadowMap); //not required
            planeShaderShadows.AssignBuffer("depth", shadowMap); //not required
            shadowMap.Clear();
            GenerateShadowMap(shadowMap);
            lightShader.SetValue("shadowMap", shadowMap);

            GenerateNoiseMap(1024, 1024);

            //Prepare SSAO shader->
            ssaoSamples = GenerateSSAONoise(16);
            ssaoBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));

            SSAO.AssignBuffer("ssao_buffer", ssaoBuffer);
            SSAO.AssignBuffer("frag_pos", worldBuffer);
            SSAO.AssignBuffer("normal", normalBuffer);

            SSAO.SetValue("kernel_radius", 1.0f);
            SSAO.SetValue("kernel_size", (int)4);
            SSAO.SetValue("kernel", ssaoSamples);
            SSAO.SetValue("depth", depthBuffer);

            SSAO.SetValue("ssao_noise", ssaoNoise);
            SSAO.ConfigureTexture("ssao_noise", TextureFiltering.GL_NEAREST, TextureWarp.GL_REPEAT);
            lightShader.AssignBuffer("ssao", ssaoBuffer);


            //Prepare FXAA
            
            FXAA.AssignBuffer("FragColor", colorBuffer);
            FXAA.SetValue("src", fxaaBuffer);
            FXAA.AssignBuffer("currentPixel", fxaaBuffer);

            //Create reflection cubemaps
            reflect1 = CreateCubemap(new Vector3(0, 20, 0), 256);
            reflect2 = CreateCubemap(new Vector3(70.0f, 20, 0), 256);
            reflect3 = CreateCubemap(new Vector3(70.0f, 20, -70.0f), 256);
            reflect4 = CreateCubemap(new Vector3(0, 20, -70.0f), 256);

            lightShader.SetValue("reflect1", reflect1);
            lightShader.SetValue("reflect2", reflect2);
            lightShader.SetValue("reflect3", reflect3);
            lightShader.SetValue("reflect4", reflect4);


            

            //Preset camera position
            inputManager.cameraPosition = new Vector3(-52.6f, 52.1984f, -122.77f);
            inputManager.cameraRotation = new Vector3(0, 13.8f, 45.5f);

            //Create FPS Counter->
            Timer fpsTimer = new Timer();
            fpsTimer.Tick += fpsTimer_Tick;
            fpsTimer.Interval = 1000;
            fpsTimer.Start();

            Console.WriteLine("\nCreating GL Window...");
            RT.Start();
            Application.Run(renderForm);
            RT.Stop();

            Console.WriteLine("Releasing GLTextures and GLBuffers...");
        }

        static void fpsTimer_Tick(object sender, EventArgs e)
        {
            LastFPS = FramesRendered;
            FramesRendered = 0;
        }
        
        static void RT_RenderFrame()
        {
            //Compute the delta time
            deltaTime.Stop();
            float deltaT = (float)deltaTime.Elapsed.TotalMilliseconds;
            lastDeltaTime = deltaT;
            deltaTime.Restart();

            //Translate mouse and keyboard inputs into a new camera position
            inputManager.CalculateMouseInput();
            inputManager.CalcualteKeyboardInput(deltaT / 33.33f);
                    
            //Get a rotation matrix and camera position from the input manager
            Matrix3x3 cameraRotation = inputManager.CreateCameraRotationMatrix();
            Vector3 cameraPosition = inputManager.cameraPosition;

            sw.Restart();

            //Clear all of the framebuffers
            GL.Clear(colorBuffer, new Color4(126, 127, 255));
            GL.Clear(fxaaBuffer, new Color4(126, 127, 255));
            GL.Clear(depthBuffer);
            GL.Clear(diffuseBuffer);
            worldBuffer.Clear();
            reflectionIndex.Clear();

            sw.Stop();
            frametimeCount.Add("[Pass] Clear: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();

            //Set shader uniforms:
            basicShader.SetValue("cameraPos", cameraPosition);
            basicShader.SetValue("cameraRot", cameraRotation);

            SSAO.SetValue("cameraPos", cameraPosition);
            SSAO.SetValue("cameraRot", cameraRotation);
            SSAO.SetValue("bias", ssao_bias);
            SSAO.SetValue("kernel_radius", ssao_range);
            SSAO.SetValue("ssao_power", ssao_power);

            GLMatrix projMatrix = GLMatrix.Perspective(90.0f, viewportWidth, viewportHeight);
            
            //only required for SSAO.SetValue
            projMatrix.SetViewportSize(viewportWidth, viewportHeight);
            SSAO.SetValue("cameraProj", projMatrix);
            SSAO.SetValue("FrameCount", (int)(FramesRendered % 2));


            GLFast.DrawSkybox(colorBuffer, skybox, cameraRotation);
            sw.Stop();
            frametimeCount.Add("[Fast] Skybox: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();


            sw.Restart();
            //Draw four Teapots:
            basicShader.SetValue("objectPos", new Vector3(0, 0, 0));
            basicShader.SetValue("objectColor", new Vector3(0.2f, 0.45f, 0.125f));
            basicShader.SetValue("specular_value", 0.1f);
            basicShader.SetValue("reflectionData", (int)1);
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            basicShader.SetValue("objectPos", new Vector3(70.0f, 0, 0));
            basicShader.SetValue("objectColor", new Vector3(0.45f, 0.2f, 0.125f));
            basicShader.SetValue("specular_value", 0.5f);
            basicShader.SetValue("reflectionData", (int)2);
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            basicShader.SetValue("objectPos", new Vector3(70.0f, 0, -70.0f));
            basicShader.SetValue("objectColor", new Vector3(0.45f, 0.125f, 0.2f));
            basicShader.SetValue("specular_value", 0.8f);
            basicShader.SetValue("reflectionData", (int)3);
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            basicShader.SetValue("objectPos", new Vector3(0, 0, -70.0f));
            basicShader.SetValue("objectColor", new Vector3(0.125f, 0.2f, 0.45f));
            basicShader.SetValue("specular_value", 0.3f);
            basicShader.SetValue("reflectionData", (int)4);
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

           

            sw.Stop();
            frametimeCount.Add("[Draw] Teapots: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();

            basicShader.SetValue("reflectionData", 0);

            //Draw the window
            basicShader.SetValue("objectPos", new Vector3(0.0f, 0.0f, 0.0f));
            basicShader.SetValue("objectColor", new Vector3(0.25f, 0.25f, 0.25f));
            GL.Draw(windowObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            sw.Stop();
            frametimeCount.Add("[Draw] Window: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();

            planeShader.SetValue("objectPos", new Vector3(-55.0f, 0.0f, -125.0f));
            planeShader.SetValue("cameraPos", cameraPosition);
            planeShader.SetValue("cameraRot", cameraRotation);
            planeShader.SetValue("camera_Pos", cameraPosition);
            planeShader.SetValue("heightScale", heightScale);

            GL.Draw(planeObject, planeShader, depthBuffer, projMatrix, GLMode.Triangle);

            sw.Stop();
            frametimeCount.Add("[Draw] Plane: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();

            //lightShader.SetValue("lightDir", Vector3.Normalize(new Vector3(0, 50.0f, 100.0f)));
            lightShader.SetValue("viewPos", cameraPosition);
            lightShader.SetValue("lightDir", Vector3.Normalize(new Vector3(0, 120.0f, 224.0f)));
            lightShader.SetValue("shadowBias", shadowBias);
            volumetricFog.SetValue("noiseX", 4.0f * (float)FramesRendered);
            volumetricFog.SetValue("noiseY", 4.0f * (float)FramesRendered);


            SSAO.Pass();

            sw.Stop();
            frametimeCount.Add("[Pass] SSAO: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();

            GLFast.BoxBlur5x5Float(ssaoBuffer, ssaoBuffer, fxaaBuffer);
         //   GLFast.BoxBlur5x5Float(ssaoBuffer, ssaoBuffer, fxaaBuffer);
            sw.Stop();
            frametimeCount.Add("[Pass] SSAO Blur: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();


            lightShader.AssignBuffer("FragColor", enableFXAA ? fxaaBuffer : colorBuffer);

         

            GL.Pass(lightShader);

            sw.Stop();
            frametimeCount.Add("[Pass] Lightning: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();


            FXAA.AssignBuffer("FragColor", colorBuffer);
           // if (enableFXAA) FXAA.Pass();

            if (enableFXAA)
            {
                GLFast.FastFXAA(colorBuffer, fxaaBuffer);
            }

            //colorBuffer.LockPixels()
           // planeObject.LockBuffer()

            //GLTexture myBuffer = new GLTexture()



            sw.Stop();
            frametimeCount.Add("[Pass] FXAA: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();


         //   volumetricFog.SetValue("FrameCount", FramesRendered);
            volumetricFog.AssignBuffer("FragColor", colorBuffer);
            volumetricFog.SetValue("camera_pos", cameraPosition);
          //  volumetricFog.Pass();

            sw.Stop();
            frametimeCount.Add("[Pass] Fog: " + sw.Elapsed.TotalMilliseconds.ToString("0.#") + "ms");
            sw.Restart();

            DrawText();

            GL.Blit(colorBuffer, formData);

         //   Console.Title = "Frametime: " + deltaT;
            Console.Title = "camPos: " + cameraPosition.ToString() + ", camRot: " + inputManager.cameraRotation.ToString();
            FramesRendered++;
        }

        static void renderForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                shadowBias += 0.05f;
            }
            else if (e.KeyCode == Keys.Down)
            {
                shadowBias -= 0.05f;
            }

            if (e.KeyCode == Keys.Right)
            {
                heightScale += 0.1f;
            }
            else if (e.KeyCode == Keys.Left)
            {
                heightScale -= 0.1f;
            }


            if (e.KeyCode == Keys.M)
            {
                ssao_bias += 0.1f;
            }
            else if (e.KeyCode == Keys.N)
            {
                ssao_bias -= 0.1f;
            }


            if (e.KeyCode == Keys.B)
            {
                ssao_range += 1f;
            }
            else if (e.KeyCode == Keys.V)
            {
                ssao_range -= 1f;
            }

            if (e.KeyCode == Keys.L)
            {
                ssao_power += 0.1f;
            }
            else if (e.KeyCode == Keys.K)
            {
                ssao_power -= 0.1f;
            }

            if (e.KeyCode == Keys.Space)
            {
                enableFXAA = !enableFXAA;
            }

            if (e.KeyCode == Keys.F12)
            {
                lock (RT.RenderLock)
                    Screenshot.Take(colorBuffer, "screenshot" + screenshot++ + ".png");
            }


            Console.WriteLine("ssao_range: " + ssao_range);
            Console.WriteLine("ssao_bias: " + ssao_bias);
        }

        static void renderForm_SizeChanged(object sender, EventArgs e)
        {
            lock (RT.RenderLock) //Lock statement prevents rendering while updates are occuring!
            {
                viewportWidth = ((Form)sender).ClientSize.Width;
                viewportHeight = ((Form)sender).ClientSize.Height;

                //Resize all of the buffers:
                colorBuffer.Resize(viewportWidth, viewportHeight);
                depthBuffer.Resize(viewportWidth, viewportHeight);


                diffuseBuffer.Resize(viewportWidth, viewportHeight);
                worldBuffer.Resize(viewportWidth, viewportHeight);
                normalBuffer.Resize(viewportWidth, viewportHeight);
                specularBuffer.Resize(viewportWidth, viewportHeight);

                ssaoBuffer.Resize(viewportWidth, viewportHeight);
                fxaaBuffer.Resize(viewportWidth, viewportHeight);

                reflectionIndex.Resize(viewportWidth, viewportHeight);
            }
        }

        static void DrawText()
        {
            GLExtra.BlitIntoBitmap(colorBuffer, infoBitmap, new Point(0, 0), new Rectangle(0, colorBuffer.Height - 400, 400, 400));

            using (Graphics g = Graphics.FromImage(infoBitmap))
            {
                g.DrawString("XFDraw v0.4.3", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 0, 400, 200));
                g.DrawString("XF2  : " + LastFPS + " FPS", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 20, 400, 200));
                g.DrawString("DT   : " + lastDeltaTime.ToString("#.0") + " ms", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 40, 400, 200));
                g.DrawString("VRAM: " + GLInfo.RAMUsageMB.ToString("#.0") + "MB", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 60, 400, 200));

                g.DrawString("----------------", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 80, 200, 400));

                for (int i = 0; i < frametimeCount.Count; i++)
                {
                    g.DrawString(frametimeCount[i], new Font("Consolas", 12), Brushes.White, new Rectangle(0, 100 + i * 20, 400, 200));
                }

               
            }

            GLExtra.BlitFromBitmap(infoBitmap, colorBuffer, new Point(0, colorBuffer.Height - 400), new Rectangle(0, 0, 400, 400));

            frametimeCount.Clear();
        }

        static GLCubemap CreateCubemap(Vector3 location, int res)
        {
            GLTexture front = new GLTexture(res, res, typeof(Color4));
            GLTexture back = new GLTexture(res, res, typeof(Color4));
            GLTexture left = new GLTexture(res, res, typeof(Color4));
            GLTexture right = new GLTexture(res, res, typeof(Color4));
            GLTexture top = new GLTexture(res, res, typeof(Color4));
            GLTexture bottom = new GLTexture(res, res, typeof(Color4));

            GL.Clear(front, new Color4(126, 127, 255));
            GL.Clear(back, new Color4(126, 127, 255));
            GL.Clear(left, new Color4(126, 127, 255));
            GL.Clear(right, new Color4(126, 127, 255));
            GL.Clear(top, new Color4(126, 127, 255));
            GL.Clear(bottom, new Color4(126, 127, 255));


            GLCubemap result = new GLCubemap(front, back, left, right, top, bottom);          
            GLMatrix projMat = GLMatrix.Perspective(90f, 90f); //90 hfov 90 vfov

            //will be reused 6 times!
            GLTexture depth = new GLTexture(res, res, typeof(float));

            //setup basicShaderCubemap
            basicShaderCubemap.SetValue("lightDir", Vector3.Normalize(new Vector3(0, 120.0f, 224.0f)));
            basicShaderCubemap.ConfigureFaceCulling(GLCull.GL_BACK);
            planeShaderCubemap.SetValue("lightDir", Vector3.Normalize(new Vector3(0, 120.0f, 224.0f)));

            planeShaderCubemap.SetValue("albedoTexture", cobbleDiffuse);
            planeShaderCubemap.SetValue("normalTexture", cobbleNormal);
            planeShaderCubemap.SetValue("textureSize", new Vector2(cobbleDiffuse.Width, cobbleDiffuse.Height));

            //Create basic render delegate
            Action<GLBuffer,Matrix3x3, Vector3, GLTexture, Vector3> drawBasic = delegate(GLBuffer buff, Matrix3x3 rot, Vector3 pos, GLTexture dest, Vector3 col) {
                if (location.x == pos.x && location.z == pos.z && buff != windowObject)
                    return;

                basicShaderCubemap.AssignBuffer("FragColor", dest);
                basicShaderCubemap.SetValue("objectColor", col);
                basicShaderCubemap.SetValue("cameraPos", location);
                basicShaderCubemap.SetValue("cameraRot", rot);
                basicShaderCubemap.SetValue("objectPos", pos);

                GL.Draw(buff, basicShaderCubemap, depth, projMat, GLMode.Triangle);
            };
            
            //create plane render delegate
            Action<Matrix3x3, GLTexture> drawPlane = delegate(Matrix3x3 rot, GLTexture dest)
            {

                planeShaderCubemap.SetValue("objectPos", new Vector3(-55.0f, 0.0f, -125.0f));
                planeShaderCubemap.SetValue("cameraPos", location);
                planeShaderCubemap.SetValue("cameraRot", rot);
                planeShaderCubemap.AssignBuffer("FragColor", dest);

                GL.Draw(planeObject, planeShaderCubemap, depth, projMat, GLMode.Triangle);
                depth.Clear();
            };

            //create face render delegate
            Action<Matrix3x3, GLTexture> drawFace = delegate(Matrix3x3 rotMat, GLTexture dest)
            {
                drawBasic(teapotObject, rotMat, new Vector3(0, 0, 0), dest, new Vector3(0.2f, 0.45f, 0.125f));
                drawBasic(teapotObject, rotMat, new Vector3(70.0f, 0, 0), dest, new Vector3(0.45f, 0.2f, 0.125f));
                drawBasic(teapotObject, rotMat, new Vector3(70.0f, 0, -70.0f), dest, new Vector3(0.45f, 0.125f, 0.2f));
                drawBasic(teapotObject, rotMat, new Vector3(0, 0, -70.0f), dest, new Vector3(0.125f, 0.2f, 0.45f));
                drawBasic(windowObject, rotMat, new Vector3(0.0f, 0.0f, 0.0f), dest, new Vector3(0.25f, 0.25f, 0.25f));
                
            };

            Matrix3x3 mat3 = Matrix3x3.YawMatrix(180) * Matrix3x3.RollMatrix(0) * Matrix3x3.PitchMatrix(0);
            GLFast.DrawSkybox(front, skybox, mat3);
            drawFace(mat3, front);
            drawPlane(mat3, front);

            mat3 = Matrix3x3.YawMatrix(180) * Matrix3x3.RollMatrix(0) * Matrix3x3.PitchMatrix(180f);
            GLFast.DrawSkybox(back, skybox, mat3);
            drawFace(mat3, back);
            drawPlane(mat3, back);

            mat3 = Matrix3x3.YawMatrix(180) * Matrix3x3.RollMatrix(0) * Matrix3x3.PitchMatrix(-90f);
            GLFast.DrawSkybox(left, skybox, mat3);
            drawFace(mat3, left);
            drawPlane(mat3, left);

            mat3 = Matrix3x3.YawMatrix(180) * Matrix3x3.RollMatrix(0) * Matrix3x3.PitchMatrix(90f);
            GLFast.DrawSkybox(right, skybox, mat3);
            drawFace(mat3, right);
            drawPlane(mat3, right);

            mat3 = Matrix3x3.YawMatrix(180) * Matrix3x3.RollMatrix(-90) * Matrix3x3.PitchMatrix(0);
            GLFast.DrawSkybox(bottom, skybox, mat3);
            drawFace(mat3, bottom);
            drawPlane(mat3, bottom);
            

            mat3 = Matrix3x3.YawMatrix(180) * Matrix3x3.RollMatrix(90) * Matrix3x3.PitchMatrix(0);
            GLFast.DrawSkybox(top, skybox, mat3);
            drawFace(mat3, top);
            drawPlane(mat3, top);
           


            return result;
        }

        static void GenerateShadowMap(GLTexture map)
        {
            lightShader.SetValue("shadowProj", shadowProj);
            lightShader.SetValue("shadowRot", shadowRot);
            lightShader.SetValue("shadowPos", new Vector3(32.91f, 120.0f, 150.0f));

            basicShaderShadows.ConfigureFaceCulling(GLCull.GL_FRONT);
            planeShaderShadows.ConfigureFaceCulling(GLCull.GL_FRONT);


            basicShaderShadows.SetValue("cameraRot", shadowRot);
            basicShaderShadows.SetValue("cameraPos", new Vector3(32.91f, 120.0f, 150.0f));


            planeShaderShadows.SetValue("cameraPos", new Vector3(32.91f, 120.0f, 150.0f));
            planeShaderShadows.SetValue("cameraRot", shadowRot);
            planeShaderShadows.SetValue("objectPos", new Vector3(-55.0f, 0.0f, -125.0f));

            //Draw four Teapots:
            basicShaderShadows.SetValue("objectPos", new Vector3(0, 0, 0));
            GL.Draw(teapotObject, basicShaderShadows, map, shadowProj, GLMode.Triangle);

            basicShaderShadows.SetValue("objectPos", new Vector3(70.0f, 0, 0));
            GL.Draw(teapotObject, basicShaderShadows, map, shadowProj, GLMode.Triangle);

            basicShaderShadows.SetValue("objectPos", new Vector3(70.0f, 0, -70.0f));
            GL.Draw(teapotObject, basicShaderShadows, map, shadowProj, GLMode.Triangle);

            basicShaderShadows.SetValue("objectPos", new Vector3(0, 0, -70.0f));
            GL.Draw(teapotObject, basicShaderShadows, map, shadowProj, GLMode.Triangle);

            //Draw the window
            basicShaderShadows.SetValue("objectPos", new Vector3(0.0f, 0.0f, 0.0f));
            GL.Draw(windowObject, basicShaderShadows, map, shadowProj, GLMode.Triangle);

            GL.Draw(planeObject, planeShaderShadows, map, shadowProj, GLMode.Triangle);

            GLTexture colorBuf = new GLTexture(map.Width, map.Height, typeof(Color4));

            GLDebug.DepthToColor(colorBuf, map, 0.5f);

            DebugWindow showDepth = new DebugWindow(colorBuf);

            volumetricFog.AssignBuffer("FragColor", fxaaBuffer);
            volumetricFog.AssignBuffer("world_pos", worldBuffer);

            volumetricFog.SetValue("NB_STEPS", (int)5);
            volumetricFog.SetValue("NB_STEPS_INV", (float)(1.0f / 5));

            volumetricFog.SetValue("MAX_LENGTH", 100f);

            volumetricFog.SetValue("shadow_rot", shadowRot);
            volumetricFog.SetValue("shadow_pos", new Vector3(32.91f, 120.0f, 150.0f));

            volumetricFog.SetValue("shadow_dir",  Vector3.Normalize(new Vector3(0, 120.0f, 224.0f)));
            volumetricFog.SetValue("shadow_proj", shadowProj);
            volumetricFog.SetValue("shadow_map", map);
            volumetricFog.SetValue("ray_bias", 1.0f);
            

        }

        static void GenerateNoiseMap(int width, int height)
        {
            GLTexture noise = new GLTexture(width / 8, height / 8, typeof(Color4));

            Random rnd = new Random();

            noise.LockPixels(delegate(GLBytes4 data) {
                for (int h = 0; h < noise.Height; h++)
                {
                    for (int w = 0; w < noise.Width; w++)
                    {
                       // data.SetPixel(w, h, (float)rnd.NextDouble());
                        byte value = (byte)rnd.Next(0, 255);
                        data.SetPixel(w, h, (int)new Color4(value, value, value));
                    }
                }
            });

            Bitmap src = new Bitmap(width / 8, height / 8);

            GLExtra.BlitIntoBitmap(noise, src, new Point(0,0), new Rectangle(0,0, src.Width / 8, src.Height / 8));

            Bitmap newBmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(newBmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                
                g.DrawImage(src, new Rectangle(0, 0, width * 8, height * 8));
            }

            noise.Dispose();
            noise = new GLTexture(width, height, typeof(Color4));
            GLExtra.BlitFromBitmap(newBmp, noise, new Point(0, 0), new Rectangle(0, 0, width, height));

            GLTexture tempMap = new GLTexture(width, height, typeof(float));
            noiseMap = tempMap;

            tempMap.LockPixels(delegate(GLFloatBytes data) {
                noise.LockPixels(delegate(GLBytes4 data1)
                {
                    for (int h = 0; h < tempMap.Height; h++)
                    {
                        for (int w = 0; w < tempMap.Width; w++)
                        {
                            data.SetPixel(w, h, Math.Min(1.0f, Math.Max(0.0f, Color.FromArgb(data1.GetPixel(w, h)).R / 255.0f)));
                          //  data.SetPixel(w, h, 1.0f);
                        }
                    }
                });
            });

           // noise.Dispose();
            //noiseMap = tempMap;

            GLTexture colorBuf = new GLTexture(noise.Width, noise.Height, typeof(Color4));
            GLDebug.DepthToColor(colorBuf, noiseMap, 255);

           // GLTexture colorBuf = noise;

            DebugWindow dWindow = new DebugWindow(colorBuf);

            volumetricFog.SetValue("noiseMap", noiseMap);
            volumetricFog.ConfigureTexture("noiseMap", TextureFiltering.GL_NEAREST, TextureWarp.GL_REPEAT);
        }

        static GLBuffer GenerateSSAONoise(int bufferSize)
        {
            Func<float,float,float,float> Lerp = delegate(float a, float b, float f){
                return a + f * (b - a);
            };

            Random rnd = new Random();
            Vector3[] buffer = new Vector3[bufferSize];
 
            for (int i = 0; i < bufferSize; i++)
            {
                Vector3 sample =  new Vector3((float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble());
                sample = Vector3.Normalize(sample);
                float scale = (float)i / (float)bufferSize;
                scale = Lerp(0.1f, 1.0f, scale * scale);
                sample *= scale;
                buffer[i] = sample;
            }

            GLTexture noise = new GLTexture(4, 4, typeof(Vector2));

            noise.LockPixels(delegate(GLBytes8 data) {
                for (int h = 0; h < 4; h++)
                {
                    for (int w = 0; w < 4; w++)
                    {
                        double angle = rnd.NextDouble() * 2d * Math.PI;
                        data.SetPixel(w,h, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
                    }
                } 
            });

            ssaoNoise = noise;

            return new GLBuffer(buffer);
        }

        static Shader LoadShader(string shaderName, string outputName)
        {
            ShaderCompile sModule = ShaderParser.Parse(shaderName, outputName, CompileOption.None, CompileOption.IncludeCstdio, CompileOption.UseFor);

          //  ShaderCompile.COMMAND_LINE = "/DEBUG /ZI";

            Shader outputShader;

            Console.Write("Compiling Shader: " + shaderName + " -> ");
            if (!sModule.Compile(out outputShader))
            {
                Console.WriteLine("Failed to compile Shader!");
                Console.ReadLine();
                return null;
            }
            Console.WriteLine("Success!");

            return outputShader;
        }

        static Shader LoadShader(string vsShaderName, string fsShaderName, string outputName)
        {
            ShaderCompile sModule = ShaderParser.Parse(vsShaderName, fsShaderName, outputName, CompileOption.None);
            //   ShaderCompile.COMMAND_LINE = "/DEBUG /ZI";
            string temp = ShaderCompile.COMMAND_LINE;
          //  ShaderCompile.COMMAND_LINE = "";

            Shader outputShader;

            Console.Write("Compiling Shader, This may take some time: " + outputName + " -> \n");
            if (!sModule.Compile(out outputShader))
            {
                Console.WriteLine("Failed to compile Shader!");
                Console.ReadLine();
                return null;
            }
            Console.WriteLine("Success!");

            ShaderCompile.COMMAND_LINE = temp;

            return outputShader;
        }
    }

    public class InputManager
    {
        bool rdown = false, ldown = false, udown = false, bdown = false;
        Vector2 KeyDelta = new Vector2(0, 0);
        Form sourceForm;

        bool CursorHook = false, mmbdown = false;
        int MMBDeltaX, MMBDeltaY;

        public Vector3 cameraPosition = new Vector3(0, 0, 0);
        public Vector3 cameraRotation = new Vector3(0, 0, 0);
        Form targetForm;

        public InputManager(Form targetForm)
        {
            sourceForm = targetForm;

            this.targetForm = targetForm;

            targetForm.KeyDown += targetForm_KeyDown;
            targetForm.KeyUp += targetForm_KeyUp;
            targetForm.MouseClick += targetForm_MouseClick;

        }

        void targetForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Cursor.Position = new Point(targetForm.PointToScreen(Point.Empty).X + targetForm.ClientSize.Width / 2, targetForm.PointToScreen(Point.Empty).Y + targetForm.ClientSize.Height / 2);
                Cursor.Hide();
                CursorHook = true;
            }
        }

        void targetForm_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        void targetForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D)
            {
                rdown = false;
            }

            if (e.KeyCode == Keys.A)
            {
                ldown = false;
            }

            if (e.KeyCode == Keys.W)
            {
                udown = false;
            }

            if (e.KeyCode == Keys.S)
            {
                bdown = false;
            }
        }

        void targetForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D)
            {
                rdown = true;
            }

            if (e.KeyCode == Keys.A)
            {
                ldown = true;
            }

            if (e.KeyCode == Keys.W)
            {
                udown = true;
            }

            if (e.KeyCode == Keys.S)
            {
                bdown = true;
            }

            if (e.KeyCode == Keys.Escape)
            {
                Cursor.Show();
                CursorHook = false;
            }
        }

        public void CalcualteKeyboardInput(float deltaTime)
        {
            if (rdown | ldown)
            {
                if (rdown)
                {
                    if (KeyDelta.x > 0)
                    {
                        KeyDelta.x = 0;
                    }
                    KeyDelta.x--;
                }
                else if (ldown)
                {
                    if (KeyDelta.x < 0)
                    {
                        KeyDelta.x = 0;
                    }
                    KeyDelta.x++;
                }
            }
            else
            {
                KeyDelta.x = 0;
            }

            if (udown | bdown)
            {
                if (udown)
                {
                    if (KeyDelta.y > 0)
                    {
                        KeyDelta.y = 0;
                    }
                    KeyDelta.y--;
                }
                else if (bdown)
                {
                    if (KeyDelta.y < 0)
                    {
                        KeyDelta.y = 0;
                    }
                    KeyDelta.y++;
                }
            }
            else
            {
                KeyDelta.y = 0;
            }

            cameraPosition = GLExtra.Pan3D(cameraPosition, cameraRotation, (KeyDelta.x / 32f) * deltaTime, 0, (KeyDelta.y / 32f) * deltaTime);
        }

        public void CalculateMouseInput()
        {
            int MouseX = 0;
            int MouseY = 0;

            if (CursorHook)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                int sourceX = 0;
                int sourceY = 0;

                sourceForm.Invoke((Action)delegate()
                {
                    sourceX = sourceForm.PointToScreen(Point.Empty).X + sourceForm.ClientSize.Width / 2;
                    sourceY = sourceForm.PointToScreen(Point.Empty).Y + sourceForm.ClientSize.Height / 2;
                });

                MouseX = cursorX - sourceX;
                MouseY = cursorY - sourceY;

                Cursor.Position = new Point(sourceX, sourceY);
                cameraRotation += new Vector3(0, MouseY / 8f, MouseX / 8f);
            }
            else if (mmbdown)// & !requestHome)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                MouseX = cursorX - MMBDeltaX;
                MouseY = cursorY - MMBDeltaY;
                MMBDeltaX = cursorX; MMBDeltaY = cursorY;

                cameraPosition = GLExtra.Pan3D(cameraPosition, cameraRotation, MouseX / 8f, MouseY / 8f);
            }
        }

        public Matrix3x3 CreateCameraRotationMatrix()
        {
            return Matrix3x3.RollMatrix(-cameraRotation.y) * Matrix3x3.PitchMatrix(-cameraRotation.z);
        }
    }
}
