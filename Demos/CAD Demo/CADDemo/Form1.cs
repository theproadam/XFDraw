using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xfcore;
using xfcore.Shaders;
using xfcore.Buffers;
using xfcore.Extras;
using xfcore.Performance;
using xfcore.Debug;
using xfcore.Info;
using xfcore.Shaders.Builder;
using System.Diagnostics;

namespace CADDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        RenderThread RT = new RenderThread(60);
        
        GLTexture colorBuffer, depthBuffer;
        GLBuffer vertexBuffer, normalBuffer;

        float[] vbuf = null;
        float[] nbuf = null;

        Shader cadShader;

        Vector3 cameraPoint = new Vector3(0, 0, 0);
        float cameraZoom = 1.0f;
        Vector3 cameraRotation = new Vector3(0, 0, 0);

        CADInputManager inputManager;

        BlitData formData;
        int viewportWidth, viewportHeight;

        Stopwatch sw = new Stopwatch();

        Vector3 CalculateCenterOfModel(float[] Input, out float BiggestDelta)
        {
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            for (int i = 0; i < Input.Length / 3; i++)
            {
                if (Input[i * 3] > max.x) max.x = Input[i * 3];
                if (Input[i * 3] < min.x) min.x = Input[i * 3];

                if (Input[i * 3 + 1] > max.y) max.y = Input[i * 3 + 1];
                if (Input[i * 3 + 1] < min.y) min.y = Input[i * 3 + 1];

                if (Input[i * 3 + 2] > max.z) max.z = Input[i * 3 + 2];
                if (Input[i * 3 + 2] < min.z) min.z = Input[i * 3 + 2];
            }

            float BG = (max.x - min.x);
            if ((max.y - min.y) > BG) BG = (max.y - min.y);
            if ((max.z - min.z) > BG) BG = (max.z - min.z);

            BiggestDelta = BG;// *2;
            return new Vector3((max.x - min.x) / 2f + min.x, (max.y - min.y) / 2f + min.y, (max.z - min.z) / 2f + min.z);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;

            this.Resize += Form1_Resize;
            this.FormClosing += Form1_FormClosing;

            viewportWidth = ClientSize.Width;
            viewportHeight = ClientSize.Height;

            colorBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(Color4));
            depthBuffer = new GLTexture(viewportWidth, viewportHeight, typeof(float));

            inputManager = new CADInputManager(this);
            formData = new BlitData(this);

            cadShader = LoadShader("cadVS.glsl", "cadFS.glsl", "cad_shader");
            cadShader.AssignBuffer("FragColor", colorBuffer);
            cadShader.ConfigureFaceCulling(GLCull.GL_BACK);

            ObjectLoader.ImportSTL(@"C:\\Users\\Adam\\3D Objects\\3D\\custom piece1.stl", out vbuf, out nbuf);
            inputManager.cameraPoint = CalculateCenterOfModel(vbuf, out inputManager.CameraZoom);
            inputManager.cameraRotation = new Vector3(0, 45, -45);

            vertexBuffer = new GLBuffer(vbuf, 3);
            normalBuffer = new GLBuffer(nbuf, 3);

            RT.RenderFrame += RT_RenderFrame;
            RT.Start();
        }

        void RT_RenderFrame()
        {
            GL.Clear(colorBuffer, 240, 240, 240);
            depthBuffer.Clear();

            inputManager.CalculateMouseInput();
            inputManager.CalcualteKeyboardInput(6.994f);
            cameraRotation = inputManager.cameraRotation;
     
            Matrix3x3 cameraRotMat = Matrix3x3.RollMatrix(-cameraRotation.y) * Matrix3x3.PitchMatrix(-cameraRotation.z);
            Vector3 dir = Matrix3x3.PitchMatrix(-cameraRotation.z) * (Matrix3x3.RollMatrix(-cameraRotation.y) * new Vector3(0, 0, -1));

            dir.y = -dir.y;
            dir.x = -dir.x;

            Vector3 cameraPosition = inputManager.cameraPoint + dir * inputManager.CameraZoom;

            sw.Reset();

            if (vertexBuffer != null)
            { 
                GLMatrix projMatrix = GLMatrix.Perspective(90, viewportWidth, viewportHeight);

                cadShader.SetValue("cameraPos", cameraPosition);
                cadShader.SetValue("cameraRot", cameraRotMat);
                cadShader.SetValue("normal_buffer", normalBuffer);
                cadShader.SetValue("camera_rotation", cameraRotMat);

                sw.Start();
                GL.Draw(vertexBuffer, cadShader, depthBuffer, projMatrix, GLMode.Triangle);
                sw.Stop();
            }

            this.Invoke((Action)delegate()
            {
                this.Text = "->" + sw.Elapsed.TotalMilliseconds + "ms";
            });

            GL.Blit(colorBuffer, formData);
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
            ShaderCompile sModule = ShaderParser.Parse(vsShaderName, fsShaderName, outputName, CompileOption.TriangleSingleMode);
            //   ShaderCompile.COMMAND_LINE = "/DEBUG /ZI";
            string temp = ShaderCompile.COMMAND_LINE;
            //  ShaderCompile.COMMAND_LINE = "";

            Shader outputShader;

            Console.Write("Compiling Shader, This may take some time: " + outputName + " -> \n");
            if (!sModule.Compile(out outputShader))
            {
                Console.WriteLine("Failed to compile Shader!");
                Console.ReadLine();
                throw new Exception();
                return null;
            }
            Console.WriteLine("Success!");

            ShaderCompile.COMMAND_LINE = temp;

            return outputShader;
        }

        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            RT.Abort();
            RT.Stop();
        }

        void Form1_Resize(object sender, EventArgs e)
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

        private void menuItem7_Click(object sender, EventArgs e)
        {
            OpenFileDialog oDialog = new OpenFileDialog();
            oDialog.Filter = "Stereolithography format|*.stl|All Files|*.*";

            if (oDialog.ShowDialog() != DialogResult.OK)
                return;

            

            ObjectLoader.ImportSTL(oDialog.FileName, out vbuf, out nbuf);

            vertexBuffer = new GLBuffer(vbuf, 3);
            normalBuffer = new GLBuffer(nbuf, 3);

            inputManager.cameraPoint = CalculateCenterOfModel(vbuf, out inputManager.CameraZoom);
            
        }
    }

    public class CADInputManager
    { 
        bool rdown = false, ldown = false, udown = false, bdown = false;
        bool shiftDown = false;

        Vector2 KeyDelta = new Vector2(0, 0);
        Form sourceForm;

        bool mmbdown = false;
        bool rmbdown = false;

        int MMBDeltaX, MMBDeltaY;

        public Vector3 cameraPoint = new Vector3(0, 0, 0);
        public Vector3 cameraRotation = new Vector3(0, 0, 0);
        Form targetForm;

        public float CameraZoom = 1.0f;

        public CADInputManager(Form targetForm)
        {
            sourceForm = targetForm;

            this.targetForm = targetForm;

            targetForm.KeyDown += targetForm_KeyDown;
            targetForm.KeyUp += targetForm_KeyUp;
            targetForm.MouseClick += targetForm_MouseClick;

            targetForm.MouseDown += targetForm_MouseDown;
            targetForm.MouseUp += targetForm_MouseUp;

            targetForm.MouseWheel += targetForm_MouseWheel;
        }

        void targetForm_MouseUp(object sender, MouseEventArgs e)
        {
             if (e.Button == MouseButtons.Middle) mmbdown = false;
             if (e.Button == MouseButtons.Right) rmbdown = false;


             if (!mmbdown)
             {
                 targetForm.Cursor = Cursors.Default;
             }

        }

        void targetForm_MouseDown(object sender, MouseEventArgs e)
        {
            MMBDeltaX = Cursor.Position.X;
            MMBDeltaY = Cursor.Position.Y;
            mmbdown = e.Button == MouseButtons.Middle;
            rmbdown = e.Button == MouseButtons.Right;

            if (mmbdown && !shiftDown)
            {
                targetForm.Cursor = Cursors.NoMove2D;
            }
            else if (mmbdown && shiftDown)
            {
                targetForm.Cursor = Cursors.SizeAll;
            }
        }

        void targetForm_MouseWheel(object sender, MouseEventArgs e)
        {
           CameraZoom += -(e.Delta / 60f);
        }

        void targetForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {

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

            if ((char)e.KeyCode == 16)
                shiftDown = false;

            if (mmbdown && !shiftDown)
            {
                targetForm.Cursor = Cursors.NoMove2D;
            }
            else if (mmbdown && shiftDown)
            {
                targetForm.Cursor = Cursors.SizeAll;
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

            shiftDown = e.Modifiers == Keys.Shift;

            if (mmbdown && !shiftDown)
            {
                targetForm.Cursor = Cursors.NoMove2D;
            }
            else if (mmbdown && shiftDown)
            {
                targetForm.Cursor = Cursors.SizeAll;
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

            Vector3 cCam = cameraRotation;
            cCam.y = 0;

            cameraPoint = GLExtra.Pan3D(cameraPoint, cCam, (KeyDelta.x / 32f) * deltaTime, 0, (KeyDelta.y / 32f) * deltaTime);
          //  cameraPoint.y = 0;
           // cameraPoint += new Vector3((KeyDelta.x / 32f) * deltaTime, 0, (KeyDelta.y / 32f) * deltaTime);
        }

        public void CalculateMouseInput()
        {
            int MouseX = 0;
            int MouseY = 0;

            if (mmbdown && !shiftDown)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                MouseX = cursorX - MMBDeltaX;
                MouseY = cursorY - MMBDeltaY;
                MMBDeltaX = cursorX; MMBDeltaY = cursorY;

              //  Cursor.Position = new Point(sourceX, sourceY);
                cameraRotation += new Vector3(0, MouseY / 8f, MouseX / 8f);
            }

            if (mmbdown && shiftDown)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                MouseX = cursorX - MMBDeltaX;
                MouseY = cursorY - MMBDeltaY;
                MMBDeltaX = cursorX; MMBDeltaY = cursorY;

                Vector3 cCam = cameraRotation;
                cCam.y = 0;

                cameraPoint = GLExtra.Pan3D(cameraPoint, cCam, CameraZoom / 25f * MouseX / 32f, 0, CameraZoom / 25f * -MouseY / 32f);
            }

            if (rmbdown)// & !requestHome)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                MouseX = cursorX - MMBDeltaX;
                MouseY = cursorY - MMBDeltaY;
                MMBDeltaX = cursorX; MMBDeltaY = cursorY;

                cameraPoint = GLExtra.Pan3D(cameraPoint, cameraRotation, MouseX / 8f, MouseY / 8f);
            }

            if (cameraRotation.y > 90f) cameraRotation.y = 90f;
            if (cameraRotation.y < -90f) cameraRotation.y = -90f;
           

        }

        public Matrix3x3 CreateCameraRotationMatrix()
        {
            return Matrix3x3.RollMatrix(-cameraRotation.y) * Matrix3x3.PitchMatrix(-cameraRotation.z);
        }
    }
}
