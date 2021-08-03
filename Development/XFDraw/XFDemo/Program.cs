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
using xfcore.Shaders.Builder;

namespace XFDemo
{
    class Program
    {
        #region Variables
        static GLTexture colorBuffer;
        static GLTexture depthBuffer;

        static Form renderForm;
        static BlitData formData;

        static RenderThread RT;
        static InputManager inputManager;

        static Stopwatch sw = new Stopwatch();
        static Stopwatch deltaTime = new Stopwatch();

        static int viewportWidth = 1600, viewportHeight = 900;

        static Color4 clearColor = new Color4(0, 0, 0);
        static bool colorsReversed = false;

        static int FramesRendered = 0, LastFPS = 0;

        //Vignette Effect
        static Shader vignetteShader;
        static GLTexture vignetteBuffer;

        static Shader colorShift;
        static Bitmap frameData = new Bitmap(400, 300);

        static GLBuffer cubeBuffer;
        static Matrix3x3 transformMatrix;
        static Shader basicShader;
        static GLMatrix projMatrix;

        static GLTexture cubeTexture;
        static MSAAData MSAA;

        //reflections demo
        static GLCubemap skybox;
        static Shader teapotShader;
        static GLBuffer teapotObject;

        //ssr reflections demo
        static GLBuffer ssrPlane;
        static Shader ssrShader;
        static GLTexture ssrBuffer;
        static GLTexture ssrBuffer2;
        static Shader ssrShaderReal;
        static GLTexture ignoreBuffer;

        #endregion

        static float rayLength = 10f;
        static int rayCount = 10;
        static float ray_bias = 0.05f;
        static float min_ray_length = 0.5f;

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            renderForm = new Form();
            renderForm.Text = "Game Window";
            renderForm.ClientSize = new Size(viewportWidth, viewportHeight);
            renderForm.StartPosition = FormStartPosition.CenterScreen;
            renderForm.SizeChanged += renderForm_SizeChanged;
            renderForm.KeyDown += renderForm_KeyDown;


            formData = new BlitData(renderForm);
            colorBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Color4));
            depthBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));
            vignetteBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));
            ssrBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Vector3));
            ssrBuffer2 = new GLTexture(viewportWidth, viewportHeight, typeof(Vector3));
            ignoreBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));

          //  cubeBuffer = GLPrimitives.Cube;         
            STLImporter sImport = new STLImporter("Teapot Fixed.stl");
            float[] cNorm = STLImporter.AverageUpFaceNormalsAndOutputVertexBuffer(sImport.AllTriangles, 89);
            teapotObject = new GLBuffer(cNorm, 6);

            STLImporter planeImport = new STLImporter("plane.stl");
            cNorm = STLImporter.AverageUpFaceNormalsAndOutputVertexBuffer(planeImport.AllTriangles, 89);
            ssrPlane = new GLBuffer(cNorm, 6);


            RT = new RenderThread(144);
            RT.RenderFrame += RT_RenderFrame;

            inputManager = new InputManager(renderForm);

            ReadyShaders();

            Console.Write("Initializing XFCore -> "); GL.Initialize(); Console.WriteLine("Success");

            vignetteShader.SetValue("viewportMod", new Vector2(2f / viewportWidth, 2f / viewportHeight));
            vignetteShader.AssignBuffer("outMultiplier", vignetteBuffer);
            vignetteShader.Pass();

            colorShift.AssignBuffer("color", colorBuffer);
            colorShift.AssignBuffer("opacity", vignetteBuffer);

         //   colorShift.Pass();

            basicShader.AssignBuffer("FragColor", colorBuffer);

            cubeTexture = new GLTexture(512, 512, typeof(Color4));
            GL.Clear(cubeTexture, 255, 127, 0);

            basicShader.SetValue("myTexture", cubeTexture);
            basicShader.SetValue("textureSize", new Vector2(512, 512));
            basicShader.ConfigureTexture("myTexture", TextureFiltering.GL_NEAREST, TextureWarp.GL_CLAMP_TO_EDGE);

            skybox = CubemapLoader.Load(@"skybox_data\");

            teapotShader.AssignBuffer("FragColor", colorBuffer);
            teapotShader.SetValue("skybox", skybox);
            teapotShader.ConfigureTexture("skybox", TextureFiltering.GL_NEAREST, TextureWarp.GL_CLAMP_TO_EDGE);
         //   skybox.Clear(255, 0, 0);

            projMatrix = GLMatrix.Perspective(90f, viewportWidth, viewportHeight);


            ssrShader.AssignBuffer("FragColor", colorBuffer);
            ssrShader.AssignBuffer("nor_data", ssrBuffer);
            ssrShader.AssignBuffer("pos_data", ssrBuffer2);


            ssrShaderReal.SetValue("skybox", skybox);

            Console.WriteLine("\nCreating GL Window...");
            RT.Start();
            Application.Run(renderForm);
            RT.Stop();

            Console.WriteLine("Releasing GLTextures and GLBuffers...");
        }

        static void renderForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                ray_bias += 0.05f;
            }
            else if (e.KeyCode == Keys.Down)
            {
                ray_bias -= 0.05f;
            }


            if (e.KeyCode == Keys.Left)
            {
                rayCount += 1;
            }
            else if (e.KeyCode == Keys.Right)
            {
                rayCount -= 1;
            }

            if (e.KeyCode == Keys.M)
            {
                rayLength += 0.1f;
            }
            else if (e.KeyCode == Keys.N)
            {
                rayLength -= 0.1f;
            }


            if (e.KeyCode == Keys.B)
            {
                min_ray_length += 0.1f;
            }
            else if (e.KeyCode == Keys.V)
            {
                min_ray_length -= 0.1f;
            }

        }

        static void RT_RenderFrame()
        {
            float deltaT = 0f;

            deltaTime.Stop();
            inputManager.CalculateMouseInput();
            inputManager.CalcualteKeyboardInput((float)deltaTime.Elapsed.TotalMilliseconds * 0.144f * 0.2f);
            deltaT = (float)deltaTime.Elapsed.TotalMilliseconds;
            deltaTime.Restart();

            transformMatrix = inputManager.CreateCameraRotationMatrix();

          //  basicShader.SetValue("cameraRot", transformMatrix);
          //  basicShader.SetValue("cameraPos", inputManager.cameraPosition);

            ssrBuffer.Clear();
            ssrBuffer2.Clear();

            teapotShader.SetValue("cameraRot", transformMatrix);
            teapotShader.SetValue("cameraPos", inputManager.cameraPosition);
            teapotShader.SetValue("camera_Pos", inputManager.cameraPosition);
           

            
            ssrShaderReal.SetValue("ray_max_length", (float)rayLength);
            ssrShaderReal.SetValue("ray_min_distance", min_ray_length);


            ssrShaderReal.SetValue("ray_count", rayCount);
            ssrShaderReal.SetValue("ray_count_inverse", (float)(rayLength / (float)rayCount));
            ssrShaderReal.SetValue("depthBuffer", depthBuffer);
            ssrShaderReal.SetValue("colorBuffer", colorBuffer);

            ssrShaderReal.SetValue("projection", projMatrix);
            ssrShaderReal.SetValue("bias", ray_bias);

            ssrShaderReal.AssignBuffer("FragColor", colorBuffer);
            ssrShaderReal.AssignBuffer("norm_data", ssrBuffer);
            ssrShaderReal.AssignBuffer("frag_pos", ssrBuffer2);



            ssrShader.SetValue("cameraRot", transformMatrix);
            ssrShader.SetValue("cameraPos", inputManager.cameraPosition);


            ignoreBuffer.Clear();

            ComputeColor();

            GL.Clear(colorBuffer, clearColor);
            GL.Clear(depthBuffer);

            sw.Start();

            GLFast.DrawSkybox(colorBuffer, skybox, transformMatrix);
          //  GL.Draw(cubeBuffer, basicShader, depthBuffer, projMatrix, GLMode.Triangle);
     
            GL.Draw(teapotObject, teapotShader, depthBuffer, projMatrix, GLMode.Triangle);
            GL.Draw(ssrPlane, ssrShader, depthBuffer, projMatrix, GLMode.Triangle);

            ssrShaderReal.Pass();

       //     GLDebug.DrawDepth(ssrPlane, depthBuffer, inputManager.cameraPosition, inputManager.cameraRotation);

         //   GLDebug.DepthToColor(colorBuffer, depthBuffer, 1f);

            GLFast.VignetteMultiply(colorBuffer, vignetteBuffer);

            sw.Stop();

            Console.Title = "DeltaTime: " + sw.Elapsed.TotalMilliseconds.ToString(".0##") + "ms, FPS: " + LastFPS;
            sw.Reset();

            DrawText();

            GL.Blit(colorBuffer, formData);
            FramesRendered++;            
        }

        static void ReadyShaders()
        {
            vignetteShader = CompileShader("vignetteShader.cpp", "vignettePass");
            colorShift = CompileShader("simpleShader.cpp", "colorShifter");
            basicShader = CompileShader("basicShaderVS.cpp", "basicShaderFS.cpp", "basicShader");
            teapotShader = CompileShader("teapotVS.cpp", "teapotFS.cpp", "teapotShader");
            ssrShader = CompileShader("ssr_shaderVS.cpp", "ssr_shader.cpp", "ssrshader");
            ssrShaderReal = CompileShader("ssr_pass.cpp", "srrpass");


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
                vignetteBuffer.Resize(viewportWidth, viewportHeight);

                vignetteShader.SetValue("viewportMod", new Vector2(2f / viewportWidth, 2f / viewportHeight));
                vignetteShader.Pass();
            }
        }

        static Shader CompileShader(string shaderName, string outputName)
        {   
            Console.Write("Parsing Shader: " + shaderName + " -> ");

            ShaderCompile sModule = ShaderParser.Parse(shaderName, outputName, CompileOption.None);
            Console.WriteLine("Success!");

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

        static Shader CompileShader(string vsShaderName, string fsShaderName, string outputName)
        {
            Console.Write("\nParsing Shader: " + vsShaderName + ", " + fsShaderName + " -> ");

            ShaderCompile sModule = ShaderParser.Parse(vsShaderName, fsShaderName, outputName, CompileOption.None);
            Console.WriteLine("Success!");

         //   ShaderCompile.COMMAND_LINE = "/DEBUG /ZI";
        //    ShaderCompile.COMMAND_LINE = "";


            Shader outputShader;

            Console.Write("Compiling Shader, This may take some time: " + outputName + " -> \n");
            if (!sModule.Compile(out outputShader))
            {
                Console.WriteLine("Failed to compile Shader!");
                Console.ReadLine();
                return null;
            }
            Console.WriteLine("Success!");

            return outputShader;
        }

        static void ComputeColor()
        {
            byte cR = clearColor.R;
            byte cG = clearColor.G;
            byte cB = clearColor.B;

            if (!colorsReversed)
            {
                if (cR == 255)
                {
                    if (cG == 255)
                    {
                        if (cB == 255)
                        {
                            colorsReversed = true;
                        }
                        else cB++;
                    }
                    else cG++;
                }
                else cR++;
            }
            else
            {
                if (cR == 0)
                {
                    if (cG == 0)
                    {
                        if (cB == 0)
                        {
                            colorsReversed = false;
                        }
                        else cB--;
                    }
                    else cG--;
                }
                else cR--;
            }

            clearColor = new Color4(cR, cG, cB);
        }

        static void DrawText()
        {
            GLExtra.BlitIntoBitmap(colorBuffer, frameData, new Point(0, 0), new Rectangle(0, colorBuffer.Height - 200, 400, 200));

            using (Graphics g = Graphics.FromImage(frameData))
            {
                g.DrawString("XFDraw v0.4.3", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 0, 200, 200));
                g.DrawString("XF2  : " + LastFPS + " FPS", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 20, 200, 200));
                g.DrawString("RayCount: " + rayCount, new Font("Consolas", 12), Brushes.White, new Rectangle(0, 40, 200, 200));
                g.DrawString("Ray Bias: " + ray_bias, new Font("Consolas", 12), Brushes.White, new Rectangle(0, 60, 200, 200));
                g.DrawString("Ray Length: " + rayLength, new Font("Consolas", 12), Brushes.White, new Rectangle(0, 80, 200, 200));
                g.DrawString("RayMin Dist: " + min_ray_length, new Font("Consolas", 12), Brushes.White, new Rectangle(0, 100, 200, 200));

            }

            GLExtra.BlitFromBitmap(frameData, colorBuffer, new Point(0, colorBuffer.Height - 200), new Rectangle(0, 0, 400, 200));  
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Material
    {
        public Vector3 ambient;
        public Vector3 diffuse;
        public Vector3 specular;
        public Color4 resultColor;

        
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

    public static class CubemapLoader
    {
        public static GLCubemap Load(string folderPath)
        {
            Bitmap front = new Bitmap(folderPath + @"\FRONT.jpg");
            Bitmap back = new Bitmap(folderPath + @"\BACK.jpg");
            Bitmap top = new Bitmap(folderPath + @"\TOP.jpg");
            Bitmap bottom = new Bitmap(folderPath + @"\BOTTOM.jpg");           
            Bitmap left = new Bitmap(folderPath + @"\LEFT.jpg");
            Bitmap right = new Bitmap(folderPath + @"\RIGHT.jpg");

            GLTexture gfront = new GLTexture(front.Width, front.Height, typeof(Color4));
            GLTexture gback = new GLTexture(back.Width, back.Height, typeof(Color4));
            GLTexture gtop = new GLTexture(top.Width, top.Height, typeof(Color4));
            GLTexture gbottom = new GLTexture(bottom.Width, bottom.Height, typeof(Color4));
            GLTexture gleft = new GLTexture(left.Width, left.Height, typeof(Color4));
            GLTexture gright = new GLTexture(right.Width, right.Height, typeof(Color4));

            //GLExtra.BlitFromBitmap(front, gfront, new Point(0, 0), new Rectangle(0, 0, gfront.Width, gfront.Height));
            //GLExtra.BlitFromBitmap(back, gback, new Point(0, 0), new Rectangle(0, 0, gback.Width, gback.Height));
            //GLExtra.BlitFromBitmap(top, gtop, new Point(0, 0), new Rectangle(0, 0, gtop.Width, gtop.Height));
            //GLExtra.BlitFromBitmap(bottom, gbottom, new Point(0, 0), new Rectangle(0, 0, gbottom.Width, gbottom.Height));
            //GLExtra.BlitFromBitmap(left, gleft, new Point(0, 0), new Rectangle(0, 0, gleft.Width, gleft.Height));
            //GLExtra.BlitFromBitmap(right, gright, new Point(0, 0), new Rectangle(0, 0, gright.Width, gright.Height));

            BitmapConvert(front, gfront);
            BitmapConvert(back, gback);
            BitmapConvert(top, gtop);
            BitmapConvert(bottom, gbottom);
            BitmapConvert(left, gleft);
            BitmapConvert(right, gright);



           

            return new GLCubemap(gfront, gback, gleft, gright, gtop, gbottom);
        }

        static void BitmapConvert(Bitmap src, GLTexture dest)
        { 
            Bitmap bmp = new Bitmap(src);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            GLExtra.BlitFromBitmap(bmp, dest, new Point(0, 0), new Rectangle(0, 0, dest.Width, dest.Height));
        }

    }
}
