using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using xfcore;
using xfcore.Shaders;
using xfcore.Buffers;
using xfcore.Extras;
using xfcore.Performance;
using xfcore.Debug;
using xfcore.Info;

namespace cppShaderInitializer
{
    class Program
    {
        static GLTexture colorBuffer;
        static GLTexture depthBuffer;

        static Shader colorShift;
        static BlitData formData;
        static int ViewportWidth = 1600, ViewportHeight = 900;
        static Stopwatch sw = new Stopwatch();
        static Stopwatch deltaTime = new Stopwatch();
        static Form renderForm;
        static RenderThread RT;

        static Shader myShader;
        static Shader buildVignette;
        static Shader runVignette;
        static GLTexture vignetteBuffer;
        static Shader colorShifter;

        static Bitmap frameData = new Bitmap(400, 300);

        static GLTexture bricks;
        static Shader brickShader;

        static byte cR = 0, cG = 0, cB = 0;
        static bool invt = false;
        static int FramesRendered = 0, LastFPS = 0;

        static GLBuffer drawObject;
        static GLBuffer planeObject;

        static GLBuffer cubeObject;
        static GLTexture shadowMap;

        static bool CursorHook = false, mmbdown = false;
        static int MMBDeltaX, MMBDeltaY;

        static Vector3 cameraPosition = new Vector3(0, 0, 0);
        static Vector3 cameraRotation;

        static bool rdown = false, ldown = false, udown = false, bdown = false;
        static Vector2 KeyDelta = new Vector2(0, 0);

        static bool renderWireframe = false;
        static double tTime = 0;
        static int itrCountt = 0;

        static float nearBias = 0.5f, normalBias = 0.55f;
        static Vector3 lightPos = new Vector3(-30, 30, -30);

        [STAThread]
        static void Main(string[] args)
        {
            ShaderModule[] shaderModules;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            renderForm = new Form();
            FormSetup(renderForm);

            formData = new BlitData(renderForm);
            colorBuffer = new GLTexture(ViewportWidth, ViewportHeight);
            depthBuffer = new GLTexture(ViewportWidth, ViewportHeight);
            
            RT = new RenderThread(144);
            RT.RenderFrame += RT_RenderFrame;
            
            Console.Write("Initializing Shaders -> ");

            if (!Shader.LoadModules("FinalShaderTests.dll", out shaderModules))
                throw new Exception("Failed To Load Shader Modules!");

            Console.WriteLine("Success");
            Console.Write("Initializing XFCore -> "); GL.Initialize(); Console.WriteLine("Success");

            cubeObject = GLPrimitives.Cube;
            drawObject = GLPrimitives.Cube;

         //   runVignette.SetValue

               // Vector3

            myShader = Shader.Load(shaderModules[0], typeof(VS3D), typeof(FS3D));
            myShader.SetValue("rotCos", GLExtra.GetCos(cameraRotation));
            myShader.SetValue("rotSin", GLExtra.GetSin(cameraRotation));
            myShader.SetValue("camPos", cameraPosition);
            myShader.AssignBuffer("FragColor", colorBuffer);
            myShader.AssignBuffer("coords_in", drawObject);
            myShader.AssignBuffer("uv_in", drawObject, 3);

            InitializeShaders(shaderModules);

            STLImporter iImport = new STLImporter("Shapes2.stl");
            Console.WriteLine("Imported Triangles: " + iImport.AllTriangles.Length);
            drawObject = ToGLBuffer.ToBuffer(iImport);
            
            float[] cNorm = STLImporter.AverageUpFaceNormalsAndOutputVertexBuffer(iImport.AllTriangles, 89);

            STLImporter iImport2 = new STLImporter("plane.stl");

          //  float[] cNorm = STLImporter.FaceNormalsToVertexNormals(iImport.AllTriangles);
            planeObject = new GLBuffer(STLImporter.FaceNormalsToVertexNormals(iImport2.AllTriangles), 6);

            drawObject = new GLBuffer(cNorm, 6);
        //    cubeObject = GLPrimitives.Cube;

            CreateShadowMap();

            InitializeFPSCounter();

            GL.Clear(depthBuffer);
            GL.Pass(buildVignette);
            

            RT.Start();
            Application.Run(renderForm);
            RT.Stop();
        }



        static void RT_RenderFrame()
        {
            deltaTime.Stop();
            CalculateMouseInput();
            CalcualteKeyboardInput((float)deltaTime.Elapsed.TotalMilliseconds * 0.144f * 0.2f);
           // Console.Title = "DeltaTime: " + deltaTime.Elapsed.TotalMilliseconds.ToString(".0##") + "ms";
            deltaTime.Restart();

            ComputeColor();
           
        //    GL.Clear(frameBuffer);
            GL.Clear(colorBuffer, cR, cG, cB);
            GL.Clear(depthBuffer);

            

            sw.Start();
         //   GLDebug.DrawFlatFill(cubeObject, frameBuffer, depthBuffer, cameraPosition, cameraRotation, true);
         //   GLDebug.DrawWireframe(cubeObject, frameBuffer, cameraPosition, cameraRotation);
        //    GLDebug.DrawFlatFill(cubeObject, frameBuffer, depthBuffer, cameraPosition, cameraRotation, renderWireframe);
           
            
            PhongConfig pc = new PhongConfig();
            pc.lightColor = new Vector3(1f, 1f, 1f);
            pc.lightPosition = lightPos;
            pc.objectColor = new Vector3(0.5f, 0.5f, 0f);
            pc.specularStrength = 0f;
            pc.ambientStrength = 0.1f;
            pc.specularPower = 16;
            pc.lightPosReal = lightPos;

            pc.SetShadowMap(shadowMap);
            pc.SetLightRotation(new Vector3(0, 25, 45));
            pc.shadowMapPresent = 1;
            pc.ShadowBias = nearBias;
            pc.ShadowNormalBias = normalBias;

            pc.LightPosCameraSpace(cameraPosition, cameraRotation);
            
         //   GLDebug.DrawFlatFill(cubeObject, frameBuffer, depthBuffer, cameraPosition, cameraRotation, false);

          //  if (renderWireframe) 
            GLDebug.DrawPhong(drawObject, colorBuffer, depthBuffer, cameraPosition, cameraRotation, pc);
          //  GLDebug.DrawPhong(planeObject, colorBuffer, depthBuffer, cameraPosition, cameraRotation, pc);


          //  else
          //  GLDebug.DrawWireframe(drawObject, frameBuffer, cameraPosition, cameraRotation);

            //GLDebug.FillDepth(drawObject, depthBuffer, cameraPosition, cameraRotation);


         //   GL.Draw(myShader, depthBuffer);//, 0, 12, GLMode.Triangle);
           
            GLFast.VignetteMultiply(colorBuffer, vignetteBuffer); 

           // GL.Pass(runVignette);
            
            sw.Stop();


            Console.Title = "DeltaTime: " + sw.Elapsed.TotalMilliseconds.ToString(".0##") + "ms";

            //GLDebug.DepthToColor(frameBuffer, depthBuffer);

            sw.Reset();

            DrawText();

            GL.Blit(colorBuffer, formData);

            FramesRendered++;            
        }

        static void CalculateMouseInput()
        {
            int MouseX = 0;
            int MouseY = 0;

            if (CursorHook)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                int sourceX = 0;
                int sourceY = 0;

                renderForm.Invoke((Action)delegate()
                {
                    sourceX = renderForm.PointToScreen(Point.Empty).X + renderForm.ClientSize.Width / 2;
                    sourceY = renderForm.PointToScreen(Point.Empty).Y + renderForm.ClientSize.Height / 2;
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

        static void CalcualteKeyboardInput(float deltaTime)
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

        static void renderForm_SizeChanged(object sender, EventArgs e)
        {
            lock (RT.RenderLock)
            {
                ViewportWidth = ((Form)sender).ClientSize.Width;
                ViewportHeight = ((Form)sender).ClientSize.Height;
                colorShift.SetValue("viewportMod", new Vector2(255f / ViewportWidth, 255f / ViewportHeight));
                buildVignette.SetValue("viewportMod", new Vector2(2f / ViewportWidth, 2f / ViewportHeight));

                colorBuffer.Resize(ViewportWidth, ViewportHeight);
                depthBuffer.Resize(ViewportWidth, ViewportHeight);


                vignetteBuffer.Resize(ViewportWidth, ViewportHeight);

                GL.Pass(buildVignette);
            }
        }

        static void DrawText()
        {
            GLExtra.BlitIntoBitmap(colorBuffer, frameData, new Point(0, 0), new Rectangle(0, colorBuffer.Height - 200, 400, 200));
            
            using (Graphics g = Graphics.FromImage(frameData))
            {
                g.DrawString("XFDraw v0.4.3", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 0, 200, 200));
                g.DrawString("XF2  : " + LastFPS + " FPS", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 20, 200, 200));
                g.DrawString("VRAM : " + GLInfo.RAMUsageMB.ToString("0.#") + "MB", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 40, 200, 200));
             //   g.DrawString("Pixl : " + (GLInfo.PixelCount / 1024f).ToString("0.#") + "K", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 60, 200, 200));

                //g.DrawString("Pixl : " + px.ToString("0.#") + "G/s", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 60, 200, 200));


              //  g.DrawString("tris : " + GLInfo.TriangleCount.ToString() + "", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 80, 200, 200));
                g.DrawString("Bias: " + nearBias, new Font("Consolas", 12), Brushes.White, new Rectangle(0, 60, 200, 200));
                g.DrawString("Normal: " + normalBias, new Font("Consolas", 12), Brushes.White, new Rectangle(0, 80, 200, 200));


              //  g.DrawString("mode : " + renderWireframe.ToString(), new Font("Consolas", 12), Brushes.White, new Rectangle(0, 80, 200, 200));
              //  g.Clear(Color.Black);
            }

            GLInfo.ResetCount();
            GLExtra.BlitFromBitmap(frameData, colorBuffer, new Point(0, colorBuffer.Height - 100), new Rectangle(0, 0, 400, 100));  
        }

        static void ComputeColor()
        {
            if (!invt)
            {
                if (cR == 255)
                {
                    if (cG == 255)
                    {
                        if (cB == 255)
                        {
                            invt = true;
                        }
                        else cB++;
                    }
                    else cG++;
                }
                else cR++;
            }
            else
            {
                if (cR ==  0)
                {
                    if (cG == 0)
                    {
                        if (cB == 0)
                        {
                            invt = false;
                        }
                        else cB--;
                    }
                    else cG--;
                }
                else cR--;
            }
        }

        static void CreateShadowMap(int res = 2048)
        {
            shadowMap = new GLTexture(res, res);
            GL.Clear(shadowMap);


            GLDebug.FillDepth(drawObject, shadowMap, lightPos, new Vector3(0, 25, 45));

            GLTexture colorBuf = new GLTexture(res, res);
            GLDebug.DepthToColor(colorBuf, shadowMap, 1f);

            float[] values = new float[2];

            Task.Run(delegate() {
               // ShadowConfigurator displayBuf = new ShadowConfigurator();
                Form displayBuf = new Form();
                BlitData bData = new BlitData(displayBuf);
              //  BlitData bData = new BlitData(displayBuf.GetPanel1());
                
                displayBuf.Text = "XFDraw shadowmap debug";
                displayBuf.ClientSize = new Size(512, 512);

                Bitmap displymp = new Bitmap(res, res);
                GLExtra.BlitIntoBitmap(colorBuf, displymp, new Point(0, 0), new Rectangle(0, 0, res, res));

                displayBuf.BackgroundImageLayout = ImageLayout.Zoom;
                displayBuf.BackgroundImage = displymp;

                displayBuf.ShowDialog();
            });

         

        }

        static void FormSetup(Form renderForm)
        {
            renderForm.StartPosition = FormStartPosition.CenterScreen;
            renderForm.Size = new Size(ViewportWidth, ViewportHeight);
            int WindowWidth = ViewportWidth - renderForm.ClientSize.Width;
            int WindowHeight = ViewportHeight - renderForm.ClientSize.Height;
            renderForm.Size = new Size(ViewportWidth + WindowWidth, ViewportHeight + WindowHeight);
            renderForm.Text = "Game Window";
            renderForm.SizeChanged += renderForm_SizeChanged;
            renderForm.MouseClick += renderForm_MouseClick;
            renderForm.FormClosing += renderForm_FormClosing;
            renderForm.KeyDown += renderForm_KeyDown;
            renderForm.KeyUp += renderForm_KeyUp;
            renderForm.KeyPress += renderForm_KeyPress;
        }

        static void renderForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
                renderWireframe = !renderWireframe;


            GLDebug.SetParallelizationTechnique(renderWireframe);
        }

        static void renderForm_KeyUp(object sender, KeyEventArgs e)
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

        static void renderForm_KeyDown(object sender, KeyEventArgs e)
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

            if (e.KeyCode == Keys.Up)
            {
                nearBias += 0.05f;
            }
            else if (e.KeyCode == Keys.Down)
            {
                nearBias -= 0.05f;
            }


            if (e.KeyCode == Keys.Left)
            {
                normalBias += 0.05f;
            }
            else if (e.KeyCode == Keys.Right)
            {
                normalBias -= 0.05f;
            }

        }

        static void renderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            RT.Abort();
            RT.Stop();
        }

        static void renderForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Cursor.Position = new Point(renderForm.PointToScreen(Point.Empty).X + renderForm.ClientSize.Width / 2, renderForm.PointToScreen(Point.Empty).Y + renderForm.ClientSize.Height / 2);
                Cursor.Hide();
                CursorHook = true;
                renderForm.Text = "CursorHook: " + CursorHook;
            }
        }

        static void InitializeFPSCounter()
        {
            Timer t = new Timer();
            t.Tick += delegate(object sender, EventArgs e) { 
                Console.Title = "FPS: " + FramesRendered; 
                LastFPS = FramesRendered;
                FramesRendered = 0;
            };
            t.Interval = 1000;
            t.Start();
        }

        static void InitializeShaders(ShaderModule[] shaderModules)
        {
            colorShift = Shader.Load(shaderModules[1], typeof(CShift));
            colorShift.SetValue("viewportMod", new Vector2(255f / ViewportWidth, 255f / ViewportHeight));
            colorShift.AssignBuffer("outColor", colorBuffer);
            colorShift.AssignVariable("XY_Coords", VariableType.XYScreenCoordinates);

            vignetteBuffer = new GLTexture(ViewportWidth, ViewportHeight, typeof(float));

            buildVignette = Shader.Load(shaderModules[2], typeof(CreateVingetteBuffer));
            buildVignette.AssignVariable("XY_Coords", VariableType.XYScreenCoordinates);
            buildVignette.AssignBuffer("outMultiplier", vignetteBuffer);
            buildVignette.SetValue("viewportMod", new Vector2(2f / ViewportWidth, 2f / ViewportHeight));

            runVignette = Shader.Load(shaderModules[3], typeof(MultiplyBy));
            runVignette.AssignBuffer("outColor", colorBuffer);
            runVignette.AssignBuffer("inMultiplier", vignetteBuffer);

            colorShifter = Shader.Load(shaderModules[4], typeof(ColorShift));
            colorShifter.AssignBuffer("outColor", colorBuffer);
            colorShifter.SetValue("tcolor", new Color4(255, 255, 0, 255));

            //   bricks = GLExtra.FromBitmap(new Bitmap("128p.png"), true);

            brickShader = Shader.Load(shaderModules[5], typeof(DisplayTexture));
            brickShader.AssignBuffer("outColor", colorBuffer);
            //  brickShader.SetValue("sourceTexture", bricks);
            brickShader.AssignVariable("XY_Coords", VariableType.XYScreenCoordinates);
            brickShader.SetValue("viewportMod", new Vector2(1f / ViewportWidth, 1f / ViewportHeight));

        }
    }
}
