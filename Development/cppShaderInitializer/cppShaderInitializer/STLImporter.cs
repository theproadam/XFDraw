using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using xfcore.Extras;
using xfcore.Buffers;

namespace cppShaderInitializer
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
}
