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

        static GLCubemap reflect1;
        static GLCubemap reflect2;
        static GLCubemap reflect3;
        static GLCubemap reflect4;


        //Object Buffer
        static GLBuffer planeObject;
        static GLBuffer teapotObject;
        static GLBuffer windowObject;

        //Texture Buffers
        static GLTexture cobblestone;

        //Shaders
        static Shader basicShader;
        static Shader lightShader;
        static Shader basicShaderShadows;
        static Shader planeShader;
        static Shader planeShaderShadows;

        static Shader volumetricFog;

        //Form data
        static Form renderForm;
        static BlitData formData;

        //Threads and rendering
        static RenderThread RT;
        static InputManager inputManager;

        //Used for delta time compensation
        static Stopwatch sw = new Stopwatch();
        static Stopwatch deltaTime = new Stopwatch();

        //Viewport Size
        static int viewportWidth = 1600, viewportHeight = 900;
        static int FramesRendered = 0;

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
            
            //Initialize the render thread
            RT = new RenderThread(144);
            RT.RenderFrame += RT_RenderFrame;

            //Load objects, average normals and create texture plane
            teapotObject = ObjectLoader.ImportSTL("models/teapot.stl", 57.0f);
            planeObject = ObjectLoader.CreatePlaneUV(180, 18);
            windowObject = ObjectLoader.ImportSTL("models/window.stl", 57.0f);
            cobblestone = ObjectLoader.LoadTexture("textures/Cobblestone [Albedo].png");

            //Parse and Compile Shaders
            basicShader = LoadShader("basicVS.cpp", "basicFS.cpp", "basicShader");
            basicShaderShadows = LoadShader("basicVS.cpp", "basicFS_shadow.cpp", "basicShaderShadow");
            lightShader = LoadShader("deferred_pass.cpp", "lightning");
            planeShader = LoadShader("planeVS.cpp", "planeFS.cpp", "planeShader");
            planeShaderShadows = LoadShader("planeVS.cpp", "planeFS_shadow.cpp", "planeShaderShadow");
            volumetricFog = LoadShader("volumetric_fog.cpp", "volumetricfog");
            //windowShader 

            //Link framebuffers to shaders and configure shaders
            basicShader.AssignBuffer("diffuse", diffuseBuffer);
            basicShader.AssignBuffer("normal", normalBuffer);
            basicShader.AssignBuffer("world_pos", worldBuffer);
            basicShader.ConfigureFaceCulling(GLCull.GL_BACK);

            lightShader.AssignBuffer("FragColor", colorBuffer);
            lightShader.AssignBuffer("pos", worldBuffer);
            lightShader.AssignBuffer("norm", normalBuffer);
            lightShader.AssignBuffer("objectColor", diffuseBuffer);

            planeShader.AssignBuffer("diffuse", diffuseBuffer);
            planeShader.AssignBuffer("normal", normalBuffer);
            planeShader.AssignBuffer("world_pos", worldBuffer);
            planeShader.SetValue("myTexture", cobblestone);
            planeShader.SetValue("textureSize", new Vector2(cobblestone.Width, cobblestone.Height));

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

            noiseMap = new GLTexture(1024, 1024, typeof(float));
            GenerateNoiseMap(1024, 1024);

            //visualize shadowmap->



            Console.WriteLine("\nCreating GL Window...");
            RT.Start();
            Application.Run(renderForm);
            RT.Stop();

            Console.WriteLine("Releasing GLTextures and GLBuffers...");
        }
        
        static void RT_RenderFrame()
        {
            //Compute the delta time
            deltaTime.Stop();
            float deltaT = (float)deltaTime.Elapsed.TotalMilliseconds;
            deltaTime.Restart();

            //Translate mouse and keyboard inputs into a new camera position
            inputManager.CalculateMouseInput();
            inputManager.CalcualteKeyboardInput(deltaT / 33.33f);
                    
            //Get a rotation matrix and camera position from the input manager
            Matrix3x3 cameraRotation = inputManager.CreateCameraRotationMatrix();
            Vector3 cameraPosition = inputManager.cameraPosition;

            //Clear all of the framebuffers
            GL.Clear(colorBuffer, new Color4(126, 127, 255));
            GL.Clear(depthBuffer);
            GL.Clear(diffuseBuffer);
            worldBuffer.Clear();

            //Set shader uniforms:
            basicShader.SetValue("cameraPos", cameraPosition);
            basicShader.SetValue("cameraRot", cameraRotation);

            GLMatrix projMatrix = GLMatrix.Perspective(90.0f, viewportWidth, viewportHeight);
         
            //Draw four Teapots:
            basicShader.SetValue("objectPos", new Vector3(0, 0, 0));
            basicShader.SetValue("objectColor", new Vector3(0.2f, 0.45f, 0.125f));
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            basicShader.SetValue("objectPos", new Vector3(70.0f, 0, 0));
            basicShader.SetValue("objectColor", new Vector3(0.45f, 0.2f, 0.125f));
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            basicShader.SetValue("objectPos", new Vector3(70.0f, 0, -70.0f));
            basicShader.SetValue("objectColor", new Vector3(0.45f, 0.125f, 0.2f));
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            basicShader.SetValue("objectPos", new Vector3(0, 0, -70.0f));
            basicShader.SetValue("objectColor", new Vector3(0.125f, 0.2f, 0.45f));
            GL.Draw(teapotObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);
        
            //Draw the window
            basicShader.SetValue("objectPos", new Vector3(0.0f, 0.0f, 0.0f));
            basicShader.SetValue("objectColor", new Vector3(0.25f, 0.25f, 0.25f));
            GL.Draw(windowObject, basicShader, depthBuffer, projMatrix, GLMode.Triangle);

            planeShader.SetValue("objectPos", new Vector3(-55.0f, 0.0f, -125.0f));


            planeShader.SetValue("cameraPos", cameraPosition);
            planeShader.SetValue("cameraRot", cameraRotation);

            GL.Draw(planeObject, planeShader, depthBuffer, projMatrix, GLMode.Triangle);


            //lightShader.SetValue("lightDir", Vector3.Normalize(new Vector3(0, 50.0f, 100.0f)));
            lightShader.SetValue("lightDir", Vector3.Normalize(new Vector3(0, 120.0f, 224.0f)));
            lightShader.SetValue("shadowBias", shadowBias);
            volumetricFog.SetValue("noiseX", (float)FramesRendered);
            volumetricFog.SetValue("noiseY", (float)FramesRendered);
            
            GL.Pass(lightShader);

            volumetricFog.SetValue("camera_pos", cameraPosition);   
            volumetricFog.Pass();


            GL.Blit(colorBuffer, formData);
          //  GL.Blit(diffuseBuffer, formData);

            Console.Title = "Frametime: " + deltaT;
         //   Console.Title = "camPos: " + cameraPosition.ToString() + ", camRot: " + inputManager.cameraRotation.ToString();
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
            }
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
          
            Task.Run(delegate()
            {
                Form displayBuf = new Form();
                BlitData bData = new BlitData(displayBuf);

                displayBuf.Text = "XFDraw shadowmap debug";
                displayBuf.ClientSize = new Size(512, 512);

                Bitmap displymp = new Bitmap(map.Width, map.Height);
                GLExtra.BlitIntoBitmap(colorBuf, displymp, new Point(0, 0), new Rectangle(0, 0, map.Width, map.Height));


                displayBuf.BackgroundImageLayout = ImageLayout.Zoom;
                displayBuf.BackgroundImage = displymp;

                displayBuf.ShowDialog();
            });


            volumetricFog.AssignBuffer("FragColor", colorBuffer);
            volumetricFog.AssignBuffer("world_pos", worldBuffer);

            volumetricFog.SetValue("NB_STEPS", (int)5);
            volumetricFog.SetValue("MAX_LENGTH", 100f);

            volumetricFog.SetValue("shadow_rot", shadowRot);
            volumetricFog.SetValue("shadow_pos", new Vector3(32.91f, 120.0f, 150.0f));

            volumetricFog.SetValue("shadow_dir",  Vector3.Normalize(new Vector3(0, 120.0f, 224.0f)));
            volumetricFog.SetValue("shadow_proj", shadowProj);
            volumetricFog.SetValue("shadow_map", map);
            volumetricFog.SetValue("ray_bias", 1.0f);
            

        }

        static unsafe void GenerateNoiseMap(int width, int height)
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
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                
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
                            data.SetPixel(w, h, Color.FromArgb(data1.GetPixel(w, h)).R / 255.0f);
                        }
                    }
                });
            });

           // noise.Dispose();
            //noiseMap = tempMap;

            GLTexture colorBuf = new GLTexture(noise.Width, noise.Height, typeof(Color4));
            GLDebug.DepthToColor(colorBuf, noiseMap, 255);

           // GLTexture colorBuf = noise;

            Task.Run(delegate()
            {
                Form displayBuf = new Form();
                BlitData bData = new BlitData(displayBuf);

                displayBuf.Text = "XFDraw shadowmap debug";
                displayBuf.ClientSize = new Size(512, 512);

                Bitmap displymp = new Bitmap(noise.Width, noise.Height);
                GLExtra.BlitIntoBitmap(colorBuf, displymp, new Point(0, 0), new Rectangle(0, 0, noise.Width, noise.Height));


                displayBuf.BackgroundImageLayout = ImageLayout.Zoom;
                displayBuf.BackgroundImage = displymp;

                displayBuf.ShowDialog();
            });

            volumetricFog.SetValue("noiseMap", noise);
            volumetricFog.ConfigureTexture("noiseMap", TextureFiltering.GL_NEAREST, TextureWarp.GL_REPEAT);
        }

        static Shader LoadShader(string shaderName, string outputName)
        {
            ShaderCompile sModule = ShaderParser.Parse(shaderName, outputName, CompileOption.None);

            //   ShaderCompile.COMMAND_LINE = "/DEBUG /ZI";

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
            ShaderCompile.COMMAND_LINE = "";

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
