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
        static Matrix4x4 transformMatrix;
        static Shader basicShader;
        static GLMatrix projectionMatrix;

        #endregion



        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            renderForm = new Form();
            renderForm.Text = "Game Window";
            renderForm.ClientSize = new Size(viewportWidth, viewportHeight);
            renderForm.StartPosition = FormStartPosition.CenterScreen;

            formData = new BlitData(renderForm);
            colorBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Color4));
            depthBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));

            cubeBuffer = GLPrimitives.Cube;

            RT = new RenderThread(144);
            RT.RenderFrame += RT_RenderFrame;

            inputManager = new InputManager(renderForm);

            ReadyShaders();

            Console.Write("Initializing XFCore -> "); GL.Initialize(); Console.WriteLine("Success");

            //Prep the vignette buffer->
            vignetteBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));
            vignetteBuffer.Clear();

            vignetteShader.SetValue("viewportMod", new Vector2(2f / viewportWidth, 2f / viewportHeight));
            vignetteShader.AssignBuffer("outMultiplier", vignetteBuffer);
            vignetteShader.Pass();


            colorShift.SetValue("viewportMod", new Vector2(2f / viewportWidth, 2f / viewportHeight));
            colorShift.AssignBuffer("color", colorBuffer);
            colorShift.Pass();



            RT.Start();
            Application.Run(renderForm);
            RT.Stop();
        }

        static void RT_RenderFrame()
        {
            float deltaT = 0f;

            deltaTime.Stop();
            inputManager.CalculateMouseInput();
            inputManager.CalcualteKeyboardInput((float)deltaTime.Elapsed.TotalMilliseconds * 0.144f * 0.2f);
            deltaT = (float)deltaTime.Elapsed.TotalMilliseconds;
            deltaTime.Restart();

            transformMatrix = inputManager.CreateCameraMatrix();

            ComputeColor();

       //     GL.Clear(colorBuffer, clearColor);
       //     GL.Clear(depthBuffer);


            sw.Start();
         //   vignetteShader.Pass();
          //  GLFast.VignetteMultiply(colorBuffer, vignetteBuffer);

         //   GLDebug.DrawWireframe(cubeBuffer, colorBuffer, inputManager.cameraPosition, inputManager.cameraRotation);
//
            //GL.Draw()

            GL.Draw(cubeBuffer, basicShader, projectionMatrix, GLMode.Triangle);

            sw.Stop();

            Console.Title = "DeltaTime: " + sw.Elapsed.TotalMilliseconds.ToString(".0##") + "ms, FPS: " + LastFPS;
            sw.Reset();

            DrawText();

            GL.Blit(colorBuffer, formData);

            FramesRendered++;            
        }

        static void ReadyShaders()
        {
          //  vignetteShader = CompileShader("vignetteShader.cpp");
         //   colorShift = CompileShader("simpleShader.cpp");
            basicShader = CompileShader("basicShaderVS.cpp", "basicShaderFS.cpp");
            

        }

        static Shader CompileShader(string shaderName)
        {
            ShaderCompile sModule;
            Console.Write("Parsing Shader: " + shaderName + " -> ");
            if (!ShaderParser.Parse(shaderName, out sModule))
            {
                Console.WriteLine("Failed to parse Shader!");
                Console.ReadLine();
                return null;
            }
            Console.WriteLine("Success!");

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

        static Shader CompileShader(string vsShaderName, string fsShaderName)
        {
            ShaderCompile sModule;
            Console.Write("Parsing Shader: " + vsShaderName + ", " + fsShaderName + " -> ");
            if (!ShaderParser.Parse(vsShaderName, fsShaderName, out sModule, CompileOption.ForceRecompile))
            {
                Console.WriteLine("Failed to parse Shader!");
                Console.ReadLine();
                return null;
            }
            Console.WriteLine("Success!");

            throw new Exception("check parse level!");

            Shader outputShader;
            
            Console.Write("Compiling Shader: " + vsShaderName + ", " + fsShaderName + " -> ");
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
            GLExtra.BlitIntoBitmap(colorBuffer, frameData, new Point(0, 0), new Rectangle(0, colorBuffer.Height - 100, 400, 100));

            using (Graphics g = Graphics.FromImage(frameData))
            {
                g.DrawString("XFDraw v0.4.3", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 0, 200, 200));
                g.DrawString("XF2  : " + LastFPS + " FPS", new Font("Consolas", 12), Brushes.White, new Rectangle(0, 20, 200, 200));
            }

            GLExtra.BlitFromBitmap(frameData, colorBuffer, new Point(0, colorBuffer.Height - 100), new Rectangle(0, 0, 400, 100));  
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

        public Matrix4x4 CreateCameraMatrix()
        {
            return Matrix4x4.TranslationMatrix(cameraPosition) * Matrix4x4.RotationMatrix(cameraRotation);
        }
    }
}
