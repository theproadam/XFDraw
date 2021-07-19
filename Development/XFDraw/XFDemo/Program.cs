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
using xfcore.Shaders.Parser;

namespace XFDemo
{
    class Program
    {
        static GLTexture colorBuffer;
        static GLTexture depthBuffer;

        static Form renderForm;
        static BlitData formData;

        static RenderThread RT;
        static InputManager IM;

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

            RT = new RenderThread(144);
            RT.RenderFrame += RT_RenderFrame;

            IM = new InputManager(renderForm);

            ReadyShaders();

            Console.Write("Initializing XFCore -> "); GL.Initialize(); Console.WriteLine("Success");

            //Prep the vignette buffer->
            vignetteBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));
            GL.Clear(vignetteBuffer);

            vignetteShader.SetValue("viewportMod", new Vector2(2f / viewportWidth, 2f / viewportHeight));
            vignetteShader.AssignBuffer("outMultiplier", vignetteBuffer);
            vignetteShader.Pass();

            int col = new Color4(255, 255, 255);

            colorShift.SetValue("viewportMod", new Vector2(2f / viewportWidth, 2f / viewportHeight));
            colorShift.AssignBuffer("color", colorBuffer);
            colorShift.Pass();

            RT.Start();
            Application.Run(renderForm);
            RT.Stop();
           // Console.ReadLine();
        }

        static void RT_RenderFrame()
        {
            float deltaT = 0f;

            deltaTime.Stop();
            IM.CalculateMouseInput();
            IM.CalcualteKeyboardInput((float)deltaTime.Elapsed.TotalMilliseconds * 0.144f * 0.2f);
            deltaT = (float)deltaTime.Elapsed.TotalMilliseconds;
            deltaTime.Restart();

            ComputeColor();

           GL.Clear(colorBuffer, clearColor);
            GL.Clear(depthBuffer);


            sw.Start();
         //   colorShift.Pass();
            GLFast.VignetteMultiply(colorBuffer, vignetteBuffer);

            sw.Stop();

            Console.Title = "DeltaTime: " + sw.Elapsed.TotalMilliseconds.ToString(".0##") + "ms";
            sw.Reset();

            GL.Blit(colorBuffer, formData);

            FramesRendered++;            
        }

        static void ReadyShaders()
        {
            vignetteShader = CompileShader("vignetteShader.cpp");
            colorShift = CompileShader("simpleShader.cpp");

            

        }

        static Shader CompileShader(string shaderName)
        {
            ShaderCompile sModule;
            Console.Write("Parsing Shader -> ");
            if (!ShaderParser.Parse(shaderName, out sModule, CompileOption.ForceRecompile))
            {
                Console.WriteLine("Failed to parse Shader!");
                return null;
            }
            Console.WriteLine("Success!");

            Shader outputShader;

            Console.Write("Compiling Shader -> ");
            if (!sModule.Compile(out outputShader))
            {
                Console.WriteLine("Failed to compile Shader!");
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

        public InputManager(Form targetForm)
        {
            sourceForm = targetForm;

            targetForm.KeyDown += targetForm_KeyDown;
            targetForm.KeyUp += targetForm_KeyUp;

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
    }
}
