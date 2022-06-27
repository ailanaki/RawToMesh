using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoGlobetrotterProtoRocktree;
using Google.Protobuf.Collections;

namespace s1
{
    public class Data
    {
        public List<CurrentMesh> Meshes = new List<CurrentMesh>();
        private DecoderRockTree decoderRockTree = new DecoderRockTree();
        private NodeData _nodeData;
        private RepeatedField<double> _ma; // nodeData.MatrixGlobeFromMesh
        private int SCALE = 10;

        // public Data(NodeData nodeData)
        // {
        //     _nodeData = nodeData;
        //     _ma = nodeData.MatrixGlobeFromMesh;
        //     foreach (var preMesh in nodeData.Meshes)
        //     {
        //        // Texture tex = preMesh.Texture[0];
        //         var cur = new CurrentMesh(ToVertice(preMesh), ToIndices(preMesh), ToNormals(preMesh));
        //         Meshes.Add(cur);
        //     }
        // }

        public Data(string nameFile)
        {
            var input = File.ReadAllBytes(nameFile);
            string name1 = "";
            int i = 0;
            while (!(nameFile[i] == '1' && nameFile[i + 1] == 's'))
            {
                i++;
            }

            i += 2;
            while (nameFile[i] != '!')
            {
                name1 += nameFile[i];
                i++;
            }

            var nodeData = NodeData.Parser.ParseFrom(input);
            _nodeData = nodeData;
            _ma = nodeData.MatrixGlobeFromMesh;
            foreach (var preMesh in nodeData.Meshes)
            {
                Texture tex = preMesh.Texture[0];
                var texH = tex.Height;
                var texW = tex.Width;
                
               // var reaTex = DecompressDXT1(tex.Data[0].ToByteArray(), (int) texW, (int) texH);
                var cur = new CurrentMesh(ToVertice(preMesh), ToIndices(preMesh), ToNormals(preMesh), 
                    name1 + "_"+preMesh.MeshId.ToString(), texW, texH);
                // var cur = new CurrentMesh(ToVertice(preMesh), ToIndices(preMesh), ToNormals(preMesh),
                //     reaTex, texH, texW);
                Meshes.Add(cur);
            }
        }

        public class Vertice
        {
            public double X, Y, Z; // position
            public byte W; // octant mask
            public double U, V; // texture coordinates

            public Vertice(double x, double y, double z, double u, double v)
            {
                X = x;
                Y = y;
                Z = z;
                U = u;
                V = v;
            }
        }

        public class Normal
        {
            public double X, Y, Z; // position

            public Normal(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }


        public class CurrentMesh
        {
            public List<Vertice> Vertices;
            public List<int> Indices;
            public List<Normal> Normals;
            public string Name;
            public uint TexW, TexH;

            public CurrentMesh(List<Vertice> vertices, List<int> indices, List<Normal> normals, string name,
                uint texW, uint texH)
            {
                Vertices = vertices;
                Indices = indices;
                Normals = normals;
                Name = name;
                TexW = texW;
                TexH = texH;
            }
            public List<GPSCoords> toGPS(List<Vertice> Vectices)
            {
                List<GPSCoords> result = new List<GPSCoords>();
                foreach (var vertex in Vectices)
                {
                    var x = vertex.X;
                    var y = vertex.Y;
                    var z = vertex.Z;
                    var R = Math.Sqrt(x * x + y * y + z * z);
                    var Latitude = Math.Asin(z / R) * 180 / Math.PI;
                    var Longitude = Math.Atan2(y,  x) * 180 / Math.PI;
                    var Height = R - 6371010;
                    result.Add(new GPSCoords(R, Latitude, Longitude, Height));
                }

                return result;
            }
        }

        private List<Vertice> ToVertice(Mesh mesh)
        {
            var answer = new List<Vertice>();
            var uvOffset = new List<float>(2);
            var uvScale = new List<float>(2);
            for (var i = 0; i < 2; i++)
            {
                uvOffset.Add(0);
                uvScale.Add(0);
            }

            //  var result = decoderRockTree.UnpackTexCoords(mesh.TextureCoordinates, vert, vert.Count, uvOffset, uvScale);
            var preVer = decoderRockTree.UnpackVertices(mesh.Vertices);
            preVer = decoderRockTree.UnpackTexCoords(mesh.TextureCoordinates, preVer,
                ref uvOffset, ref uvScale);
            var tex = mesh.Texture[0];
            for (var i = 0; i < preVer.Count; i += 1)
            {
                var x = preVer[i].X;
                var y = preVer[i].Y;
                var z = preVer[i].Z;
                var w = 1;
                var _x = x * _ma[0] + y * _ma[4] + z * _ma[8] + w * _ma[12];
                var _y = x * _ma[1] + y * _ma[5] + z * _ma[9] + w * _ma[13];
                var _z = x * _ma[2] + y * _ma[6] + z * _ma[10] + w * _ma[14];
                var _w = x * _ma[3] + y * _ma[7] + z * _ma[11] + w * _ma[15];

                var ut = 0.0;
                var vt = 0.0;

                if (mesh.UvOffsetAndScale != null)
                {
                    var u = Convert.ToInt32(preVer[i].U2 * 256 + preVer[i].U1);
                    var v = Convert.ToInt32(preVer[i].V2 * 256 + preVer[i].V1);

                    ut = (u + uvOffset[0]) * uvScale[0];
                    vt = (v + uvOffset[1]) * uvScale[1];

                    if (tex.Format == Texture.Types.Format.CrnDxt1)
                    {
                        vt = 1 - vt;
                    }
                }

                answer.Add(new Vertice(_x, _y, _z, ut, vt));
            }

            return answer;
        }

        private List<int> ToIndices(Mesh mesh)
        {
            List<int> ind = new List<int>();

            var preInd = decoderRockTree.UnpackIndices(mesh.Indices);
            var layerBounds = decoderRockTree.UnpackOctantMaskAndOctantCountsAndLayerBounds(mesh.LayerAndOctantCounts,
                preInd, decoderRockTree.UnpackVertices(mesh.Vertices));
            for (int i = 0; i < preInd.Count - 2; i++)
            {
                if (i == layerBounds[3]) break;
                var a = preInd[i];
                var b = preInd[i + 1];
                var c = preInd[i + 2];
                if (a == b || a == c || b == c)
                {
                    continue;
                }

                if (i % 2 == 0)
                {
                    ind.Add(a);
                    ind.Add(b);
                    ind.Add(c);
                }
                else
                {
                    ind.Add(a);
                    ind.Add(c);
                    ind.Add(b);
                }
            }

            return ind;
        }

        private List<Normal> ToNormals(Mesh mesh)
        {
            var answer = new List<Normal>();
            var norm = decoderRockTree.UnpackForNormals(_nodeData);
            var res = decoderRockTree.UnpackNormals(mesh,
                norm.UnpackedForNormals, norm.Count);
            var normals = res.UnpackedForNormals;
            for (var i = 0; i < normals.Length; i += 4)
            {
                var x = normals[i + 0] - 127;
                var y = normals[i + 1] - 127;
                var z = normals[i + 2] - 127;
                var w = 0;

                double x1 = 0;
                double y1 = 0;
                double z1 = 0;
                double w1 = 0;

                x1 = x * _ma[0] + y * _ma[4] + z * _ma[8] + w * _ma[12];
                y1 = x * _ma[1] + y * _ma[5] + z * _ma[9] + w * _ma[13];
                z1 = x * _ma[2] + y * _ma[6] + z * _ma[10] + w * _ma[14];
                w1 = x * _ma[3] + y * _ma[7] + z * _ma[11] + w * _ma[15];
                answer.Add(new Normal(x1, y1, z1));
            }

            return answer;
        }

        public class GPSCoords
        {
            public double R, Latitude, Longitude, Height;

            public GPSCoords(double r, double latitude, double longtitude, double height)
            {
                R = r;
                Latitude = latitude;
                Longitude = longtitude;
                Height = height;
            }
        }

    

        private double lerp(short v1,short v2, double r)
        {
            return v1 * (1 - r) + v2 * r;
        }

        List<short> convert565ByteToRgb(short b)
        {
            var res = new List<short>();
            res.Add(Convert.ToInt16(Math.Round(Convert.ToDouble((b >> 11) & 31) * (255 / 31))));
            res.Add(Convert.ToInt16(Math.Round(Convert.ToDouble((b >> 5) & 63) * (255 / 63))));
            res.Add(Convert.ToInt16(Math.Round(Convert.ToDouble(b & 31) * (255 / 31))));
            return res;
        }

        public byte[] DecompressDXT1(byte[] input, int width, int height)
        {
            var rgba = new byte[width * height * 4];
            var height_4 = (height / 4) | 0;
            var width_4 = (width / 4) | 0;
            var offset = 0;

            for (var h = 0; h < height_4; h++)
            {
                if (offset > input.Length - 8) break;
                for (var w = 0; w < width_4; w++)
                {
                    if (offset > input.Length - 8) break;
                    var colorValues = interpolateColorValues(Convert.ToInt16(input[offset]), Convert.ToInt16(input[offset + 2]));
                    var colorIndices = Convert.ToInt32(input[offset + 4]);

                    for (var y = 0; y < 4; y++)
                    {
                        for (var x = 0; x < 4; x++)
                        {
                            var pixelIndex = (3 - x) + (y * 4);
                            var rgbaIndex = (h * 4 + 3 - y) * width * 4 + (w * 4 + x) * 4;
                            var colorIndex = (colorIndices >> (2 * (15 - pixelIndex))) & 0x03;
                            rgba[rgbaIndex] = Convert.ToByte(colorValues[colorIndex * 4]);
                            rgba[rgbaIndex + 1] =  Convert.ToByte(colorValues[colorIndex * 4 + 1]);
                            rgba[rgbaIndex + 2] =  Convert.ToByte(colorValues[colorIndex * 4 + 2]);
                            rgba[rgbaIndex + 3] =  Convert.ToByte(colorValues[colorIndex * 4 + 3]);
                        }
                    }

                    offset += 8;
                }
            }
            var bmpData = new List<byte>();
            for (var i = 0; i < rgba.Length; i += 4) {
                bmpData.Add(255);
                bmpData.Add(rgba[i + 2]);
                bmpData.Add(rgba[i + 1]);
                bmpData.Add(rgba[i + 0]);
            }

            StreamWriter writer = new StreamWriter("ex.bmp");
            foreach (var b in bmpData)
            {
                writer.Write(b);
            }
            
            return rgba;
        }

        List<short> interpolateColorValues(Int16 firstVal, Int16 secondVal)
        {
            List<short> firstColor = convert565ByteToRgb(firstVal);
            List<short> secondColor = convert565ByteToRgb(secondVal);
            var colorValues = new List<short>();
            var copy = new List<short>();
            copy.AddRange(firstColor);
            copy.Add(255);
            copy.AddRange(secondColor);
            copy.Add(255);
            colorValues.AddRange(copy);


            if (firstVal <= secondVal)
            {
                copy.Clear();
                copy.Add(Convert.ToByte(Math.Round(Convert.ToDouble((firstColor[0] + secondColor[0]) / 2))));
                copy.Add(Convert.ToByte(Math.Round(Convert.ToDouble((firstColor[1] + secondColor[1]) / 2))));
                copy.Add(Convert.ToByte(Math.Round(Convert.ToDouble((firstColor[2] + secondColor[2]) / 2))));
                copy.Add(255);

                copy.Add(0);
                copy.Add(0);
                copy.Add(0);
                copy.Add(0);
                colorValues.AddRange(copy);
            }
            else
            {
                copy.Clear();
                copy.Add(Convert.ToByte(Math.Round(lerp(firstColor[0], secondColor[0], 1 / 3))));
                copy.Add(Convert.ToByte(Math.Round(lerp(firstColor[1], secondColor[1], 1 / 3))));
                copy.Add(Convert.ToByte(Math.Round(lerp(firstColor[2], secondColor[2], 1 / 3))));
                copy.Add(255);


                copy.Add(Convert.ToByte(Math.Round(lerp(firstColor[0], secondColor[0], 2 / 3))));
                copy.Add(Convert.ToByte(Math.Round(lerp(firstColor[1], secondColor[1], 2 / 3))));
                copy.Add(Convert.ToByte(Math.Round(lerp(firstColor[2], secondColor[2], 2 / 3))));
                copy.Add(255);

                colorValues.AddRange(copy);
            }

            return colorValues;
        }
        
        
    }
}