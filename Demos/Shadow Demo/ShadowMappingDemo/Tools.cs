using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using xfcore.Extras;
using xfcore.Buffers;
using System.Drawing;
using System.Windows.Forms;
using xfcore;

namespace ShadowMappingDemo
{
    public class STLImporter
    {
        //WARNING: This STL Importer has issues importing ASCII Files on certain computers running Windows 10.
        public string STLHeader { get; private set; }
        public STLFormat STLType { get; private set; }
        public uint TriangleCount { get; private set; }
        public Triangle[] AllTriangles { get; private set; }

        public STLImporter(string TargetFile)
        {
            // Verify That The File Exists
            if (!File.Exists(TargetFile))
                throw new System.IO.FileNotFoundException("Target File Does Not Exist!", "Error!");

            // Load The File Into The Memory as ASCII
            string[] allLinesASCII = File.ReadAllLines(TargetFile);

            // Detect if STL File is ASCII or Binary
            bool ASCII = isAscii(allLinesASCII);

            // Insert Comment Here
            if (ASCII)
            {
                STLType = STLFormat.ASCII;
                AllTriangles = ASCIISTLOpen(allLinesASCII);
            }
            else
            {
                STLType = STLFormat.Binary;
                AllTriangles = BinarySTLOpen(TargetFile);
            }

        }

        Triangle[] BinarySTLOpen(string TargetFile)
        {
            List<Triangle> Triangles = new List<Triangle>();

            byte[] fileBytes = File.ReadAllBytes(TargetFile);
            byte[] header = new byte[80];

            for (int b = 0; b < 80; b++)
                header[b] = fileBytes[b];

            STLHeader = System.Text.Encoding.UTF8.GetString(header);

            uint NumberOfTriangles = System.BitConverter.ToUInt32(fileBytes, 80);
            TriangleCount = NumberOfTriangles;

            for (int i = 0; i < NumberOfTriangles; i++)
            {
                // Read The Normal Vector
                float normalI = System.BitConverter.ToSingle(fileBytes, 84 + i * 50);
                float normalJ = System.BitConverter.ToSingle(fileBytes, (1 * 4) + 84 + i * 50);
                float normalK = System.BitConverter.ToSingle(fileBytes, (2 * 4) + 84 + i * 50);

                // Read The XYZ Positions of The First Vertex
                float vertex1x = System.BitConverter.ToSingle(fileBytes, 3 * 4 + 84 + i * 50);
                float vertex1y = System.BitConverter.ToSingle(fileBytes, 4 * 4 + 84 + i * 50);
                float vertex1z = System.BitConverter.ToSingle(fileBytes, 5 * 4 + 84 + i * 50);

                // Read The XYZ Positions of The Second Vertex
                float vertex2x = System.BitConverter.ToSingle(fileBytes, 6 * 4 + 84 + i * 50);
                float vertex2y = System.BitConverter.ToSingle(fileBytes, 7 * 4 + 84 + i * 50);
                float vertex2z = System.BitConverter.ToSingle(fileBytes, 8 * 4 + 84 + i * 50);

                // Read The XYZ Positions of The Third Vertex
                float vertex3x = System.BitConverter.ToSingle(fileBytes, 9 * 4 + 84 + i * 50);
                float vertex3y = System.BitConverter.ToSingle(fileBytes, 10 * 4 + 84 + i * 50);
                float vertex3z = System.BitConverter.ToSingle(fileBytes, 11 * 4 + 84 + i * 50);

                // Read The Attribute Byte Count
                int Attribs = System.BitConverter.ToInt16(fileBytes, 12 * 4 + 84 + i * 50);

                // Create a Triangle
                Triangle T = new Triangle();

                // Save all the Data Into Said Triangle
                T.normals = new Vector3(normalI, normalK, normalJ);
                T.vertex1 = new Vector3(vertex1x, vertex1z, vertex1y);
                T.vertex2 = new Vector3(vertex2x, vertex2z, vertex2y);//Possible Error?
                T.vertex3 = new Vector3(vertex3x, vertex3z, vertex3y);

                // Add The Triangle
                Triangles.Add(T);
            }

            return Triangles.ToArray();
        }

        Triangle[] ASCIISTLOpen(string[] ASCIILines)
        {
            STLHeader = ASCIILines[0].Replace("solid ", "");

            uint tCount = 0;
            List<Triangle> Triangles = new List<Triangle>();

            foreach (string s in ASCIILines)
                if (s.Contains("facet normal"))
                    tCount++;

            TriangleCount = tCount;

            for (int i = 0; i < tCount * 7; i += 7)
            {
                string n = ASCIILines[i + 1].Trim().Replace("facet normal", "").Replace("  ", " ");

                // Read The Normal Vector
                float normalI = float.Parse(n.Split(' ')[1]);
                float normalJ = float.Parse(n.Split(' ')[2]);
                float normalK = float.Parse(n.Split(' ')[3]);

                string v1 = ASCIILines[i + 3].Split('x')[1].Replace("  ", " ");


                // Read The XYZ Positions of The First Vertex
                float vertex1x = float.Parse(v1.Split(' ')[1]);
                float vertex1y = float.Parse(v1.Split(' ')[2]);
                float vertex1z = float.Parse(v1.Split(' ')[3]);

                string v2 = ASCIILines[i + 4].Split('x')[1].Replace("  ", " ");

                // Read The XYZ Positions of The Second Vertex
                float vertex2x = float.Parse(v2.Split(' ')[1]);
                float vertex2y = float.Parse(v2.Split(' ')[2]);
                float vertex2z = float.Parse(v2.Split(' ')[3]);

                string v3 = ASCIILines[i + 5].Split('x')[1].Replace("  ", " ");

                // Read The XYZ Positions of The Third Vertex
                float vertex3x = float.Parse(v3.Split(' ')[1]);
                float vertex3y = float.Parse(v3.Split(' ')[2]);
                float vertex3z = float.Parse(v3.Split(' ')[3]);

                // Create a Triangle
                Triangle T = new Triangle();

                // Save all the Data Into Said Triangle
                T.normals = new Vector3(normalI, normalK, normalJ);
                T.vertex1 = new Vector3(vertex1x, vertex1z, vertex1y);
                T.vertex2 = new Vector3(vertex2x, vertex2z, vertex2y);
                T.vertex3 = new Vector3(vertex3x, vertex3z, vertex3y);

                // Add The Triangle
                Triangles.Add(T);
            }

            return Triangles.ToArray();
        }

        bool isAscii(string[] Lines)
        {
            string[] Keywords = new string[] { "facet", "solid", "outer", "loop", "vertex", "endloop", "endfacet" };
            int Det = 0;

            foreach (string s in Lines)
            {
                foreach (string ss in Keywords)
                {
                    if (s.Contains(ss))
                    {
                        Det++;
                    }
                }
            }

            if (Det > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public enum STLFormat
        {
            ASCII,
            Binary
        }

        public static float[] AverageUpFaceNormalsAndOutputVertexBuffer(Triangle[] Input, float CutoffAngle)
        {
            Vector3[] VERTEX_DATA = new Vector3[Input.Length * 3];
            Vector3[] VERTEX_NORMALS = new Vector3[Input.Length * 3];
            int[] N_COUNT = new int[Input.Length * 3];

            for (int i = 0; i < Input.Length; i++)
            {
                VERTEX_DATA[i * 3] = Input[i].vertex1;
                VERTEX_DATA[i * 3 + 1] = Input[i].vertex2;
                VERTEX_DATA[i * 3 + 2] = Input[i].vertex3;
            }

            CutoffAngle *= (float)(Math.PI / 180f);
            CutoffAngle = (float)Math.Cos(CutoffAngle);

            for (int i = 0; i < VERTEX_DATA.Length; i++)
            {
                for (int j = 0; j < VERTEX_DATA.Length; j++)
                {
                    if (Vector3.Compare(VERTEX_DATA[j], VERTEX_DATA[i]) && Vector3.Dot(Input[i / 3].normals, Input[j / 3].normals) > CutoffAngle)
                    {
                        VERTEX_NORMALS[i] += Input[j / 3].normals;
                        N_COUNT[i]++;
                    }
                }
            }

            for (int i = 0; i < N_COUNT.Length; i++)
            {
                if (N_COUNT[i] != 0)
                    VERTEX_NORMALS[i] /= N_COUNT[i];
            }

            float[] Output = new float[VERTEX_DATA.Length * 6];

            for (int i = 0; i < VERTEX_DATA.Length; i++)
            {
                Output[i * 6 + 0] = VERTEX_DATA[i].x;
                Output[i * 6 + 1] = VERTEX_DATA[i].y;
                Output[i * 6 + 2] = VERTEX_DATA[i].z;
                Output[i * 6 + 3] = VERTEX_NORMALS[i].x;
                Output[i * 6 + 4] = VERTEX_NORMALS[i].y;
                Output[i * 6 + 5] = VERTEX_NORMALS[i].z;

            }

            return Output;
        }

        public static float[] FaceNormalsToVertexNormals(Triangle[] Input)
        {
            Vector3[] VERTEX_DATA = new Vector3[Input.Length * 3];
            Vector3[] VERTEX_NORMALS = new Vector3[Input.Length];
            int[] N_COUNT = new int[Input.Length * 3];

            for (int i = 0; i < Input.Length; i++)
            {
                VERTEX_DATA[i * 3] = Input[i].vertex1;
                VERTEX_DATA[i * 3 + 1] = Input[i].vertex2;
                VERTEX_DATA[i * 3 + 2] = Input[i].vertex3;
                VERTEX_NORMALS[i] = Input[i].normals;
            }


            float[] Output = new float[VERTEX_DATA.Length * 6];

            for (int i = 0; i < VERTEX_DATA.Length; i++)
            {
                Output[i * 6 + 0] = VERTEX_DATA[i].x;
                Output[i * 6 + 1] = VERTEX_DATA[i].y;
                Output[i * 6 + 2] = VERTEX_DATA[i].z;
                Output[i * 6 + 3] = VERTEX_NORMALS[i / 3].x;
                Output[i * 6 + 4] = VERTEX_NORMALS[i / 3].y;
                Output[i * 6 + 5] = VERTEX_NORMALS[i / 3].z;

            }

            return Output;
        }


    }

    public class Triangle
    {
        public Vector3 normals;
        public Vector3 vertex1;
        public Vector3 vertex2;
        public Vector3 vertex3;

    }

    public static class ToGLBuffer
    {
        public static GLBuffer ToBuffer(STLImporter Importer)
        {
            float[] vertexpoints = new float[Importer.AllTriangles.Length * 3 * 3];
            float[] normalBuffer = new float[Importer.AllTriangles.Length * 3];
            for (int i = 0; i < Importer.AllTriangles.Length; i++)
            {
                vertexpoints[i * 9] = Importer.AllTriangles[i].vertex1.x;
                vertexpoints[i * 9 + 1] = Importer.AllTriangles[i].vertex1.y;
                vertexpoints[i * 9 + 2] = Importer.AllTriangles[i].vertex1.z;
                vertexpoints[i * 9 + 3] = Importer.AllTriangles[i].vertex2.x;
                vertexpoints[i * 9 + 4] = Importer.AllTriangles[i].vertex2.y;
                vertexpoints[i * 9 + 5] = Importer.AllTriangles[i].vertex2.z;
                vertexpoints[i * 9 + 6] = Importer.AllTriangles[i].vertex3.x;
                vertexpoints[i * 9 + 7] = Importer.AllTriangles[i].vertex3.y;
                vertexpoints[i * 9 + 8] = Importer.AllTriangles[i].vertex3.z;
                normalBuffer[i * 3] = Importer.AllTriangles[i].normals.x;
                normalBuffer[i * 3 + 1] = Importer.AllTriangles[i].normals.y;
                normalBuffer[i * 3 + 2] = Importer.AllTriangles[i].normals.z;
            }

            return new GLBuffer(vertexpoints);
        }
    }

    public static class ObjectLoader
    {
        public static GLCubemap LoadCubemap(string folderPath, bool isJPEG = true)
        {
            Bitmap front = new Bitmap(folderPath + @"\FRONT." + (isJPEG ? "jpg" : "png"));
            Bitmap back = new Bitmap(folderPath + @"\BACK." + (isJPEG ? "jpg" : "png"));
            Bitmap top = new Bitmap(folderPath + @"\TOP." + (isJPEG ? "jpg" : "png"));
            Bitmap bottom = new Bitmap(folderPath + @"\BOTTOM." + (isJPEG ? "jpg" : "png"));
            Bitmap left = new Bitmap(folderPath + @"\LEFT." + (isJPEG ? "jpg" : "png"));
            Bitmap right = new Bitmap(folderPath + @"\RIGHT." + (isJPEG ? "jpg" : "png"));

            GLTexture gfront = new GLTexture(front.Width, front.Height, typeof(Color4));
            GLTexture gback = new GLTexture(back.Width, back.Height, typeof(Color4));
            GLTexture gtop = new GLTexture(top.Width, top.Height, typeof(Color4));
            GLTexture gbottom = new GLTexture(bottom.Width, bottom.Height, typeof(Color4));
            GLTexture gleft = new GLTexture(left.Width, left.Height, typeof(Color4));
            GLTexture gright = new GLTexture(right.Width, right.Height, typeof(Color4));

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

        public static GLTexture LoadTexture(string fileName)
        {
            Bitmap src = new Bitmap(fileName);
            GLTexture texture = new GLTexture(src.Width, src.Height, typeof(Color4));

            BitmapConvert(src, texture);
            return texture;
        }

        public static GLBuffer ImportSTL(string fileName, float interpolationAngle)
        {
            STLImporter sImport = new STLImporter(fileName);
            float[] cNorm = STLImporter.AverageUpFaceNormalsAndOutputVertexBuffer(sImport.AllTriangles, interpolationAngle);
            return new GLBuffer(cNorm, 6);
        }

        public static GLBuffer CreatePlaneUV(float Size, float Resolution)
        {
            float[] src = BuildCoordBuffer(Size, Resolution);
            return new GLBuffer(src, 5);
        }

        static float[] BuildCoordBuffer(float resolution, float trisMp)
        {
            List<float> vData = new List<float>();

            //int res = 256 / whatever;
            int res = (int)resolution / (int)trisMp;

            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    AddTo(vData, new Vector2(x, y), resolution, trisMp);
                }
            }

            return vData.ToArray();
        }

        static void AddTo(List<float> data, Vector2 offset, float size, float trisMp)
        {
            Vector2 mult = new Vector2(trisMp, trisMp);
            Vector2 m1 = new Vector2(1f / size * trisMp, 1f / size * trisMp);
            Random rnd = new Random();

            float[] coords1 = new float[] {
                0, 0, 0, 0, 0,
                0, 0, 1, 0, 1,
                1, 0, 1, 1, 1,

                0, 0, 0, 0, 0,
                1, 0, 1, 1, 1,
                1, 0, 0, 1, 0
                };

            for (int i = 0; i < 6; i++)
            {
                coords1[i * 5 + 0] *= mult.x;

                //  coords1[i * 5 + 1] *= mult.y;
                //   coords1[i * 5 + 2] = 0;
                coords1[i * 5 + 1] = 0;
                coords1[i * 5 + 2] *= mult.y;


                coords1[i * 5 + 3] *= m1.x;
                coords1[i * 5 + 4] *= m1.y;


                coords1[i * 5 + 0] += (offset.x * mult.x);
                //coords1[i * 5 + 1] += (offset.y * mult.y);
                coords1[i * 5 + 2] += (offset.y * mult.y);



                coords1[i * 5 + 3] += (offset.x * m1.x);
                coords1[i * 5 + 4] += (offset.y * m1.y);

            }

            data.AddRange(coords1);
        }
    }

    public static class Screenshot
    {
        public static void Take(GLTexture source, string fileOut)
        {
            if (File.Exists(fileOut))
                File.Delete(fileOut);

            if (source.Stride != 4)
                throw new Exception("Source must be 32bpp!");

            Bitmap output = null;

            source.LockPixels(delegate(GLBytes4 data) {
                output = new Bitmap(source.Width, source.Height);
                GLExtra.BlitIntoBitmap(source, output, new Point(0, 0), new Rectangle(0, 0, source.Width, source.Height));
            });

            output.Save(fileOut);
            output.Dispose();
  
        }
    }

    public class DebugWindow// : Form
    {
        public Bitmap displayBitmap;

        public DebugWindow(GLTexture texture)
        {
            Task.Run(delegate()
            {
                Form displayBuf = new Form();
                BlitData bData = new BlitData(displayBuf);

                displayBuf.Text = "XFDraw shadowmap debug";
                displayBuf.ClientSize = new Size(512, 512);

                displayBitmap = new Bitmap(texture.Width, texture.Height);
                GLExtra.BlitIntoBitmap(texture, displayBitmap, new Point(0, 0), new Rectangle(0, 0, texture.Width, texture.Height));

                displayBuf.BackgroundImageLayout = ImageLayout.Zoom;
                displayBuf.BackgroundImage = displayBitmap;

                displayBuf.ShowDialog();
            });
        }
    }
}
