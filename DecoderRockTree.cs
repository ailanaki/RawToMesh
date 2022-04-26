using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.Protobuf;

namespace GeoGlobetrotterProtoRocktree
{
    public class DecoderRockTree
    {
// unpackVarInt unpacks variable length integer from proto (like coded_stream.h)
        private static int UnpackVarInt(ByteString packed, ref int index)
        {
            var data = packed.ToByteArray();
            var size = data.Length;
            int c = 0;
            int d = 1, e;
            do
            {
                e = data[index++];
                c += Convert.ToInt32((e & 0x7F) * d);
                d <<= 7;
            } while ((e & 0x80) != 0);

            return c;
        }

// vertex is a packed struct for an 8-byte-per-vertex array
        public class VertexT
        {
            public byte X, Y, Z; // position
            public byte W; // octant mask
            public Int16 U, V; // texture coordinates

            public VertexT(byte x, byte y, byte z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        };


// unpackVertices unpacks vertices XYZ to new 8-byte-per-vertex array
        public List<VertexT> UnpackVertices(ByteString packed)
        {
            var data = packed.Memory.ToArray();
            var count = packed.Length / 3;
            var vtx = new List<VertexT>(count); // 8 bit for % 0x100;
            for (var i = 0; i < count; i++)
            {
                vtx.Add(new VertexT(data[count * 0 + i], data[count * 1 + i], data[count * 2 + i]));
            }

            return vtx;
        }

        public class ResultOfUnpackTexCoords
        {
            public ResultOfUnpackTexCoords(List<float> uvOffset, List<float> uvScale, List<VertexT> vertices)
            {
                UvOffset = uvOffset;
                UvScale = uvScale;
                Vertices = vertices;
            }

            public List<float> UvOffset;
            public List<float> UvScale;
            public readonly List<VertexT> Vertices;
        }

// unpackTexCoords unpacks texture coordinates UV to 8-byte-per-vertex-array
        public ResultOfUnpackTexCoords UnpackTexCoords(ByteString packed, List<VertexT> vertices, int
            verticesLen, List<float> uvOffset, List<float> uvScale)
        {
            var data = new List<byte>(packed.Memory.ToArray());
            var count = verticesLen;
            var uMod = 1 + (data.Count / 16 + 0);
            var vMod = 1 + (data.Count / 16 + 2);
            data.Add(0);
            data.Add(0);
            data.Add(0);
            data.Add(0);
            var vtx = vertices;
            ushort u = 0, v = 0;
            for (var i = 0; i < count; i++)
            {
                vtx[i].U = Convert.ToInt16(u + data[count * 0 + i] + (data[count * 2 + i] << 8) % uMod);
                vtx[i].V = Convert.ToInt16(v + data[count * 1 + i] + (data[count * 3 + i] << 8) % vMod);
            }

            uvOffset[0] = (float) 0.5;
            uvOffset[1] = (float) 0.5;
            uvScale[0] = (float) 1.0 / uMod;
            uvScale[1] = (float) 1.0 / vMod;
            return new ResultOfUnpackTexCoords(uvOffset, uvScale, vertices);
        }

// unpackIndices unpacks indices to triangle strip
        public List<UInt16> UnpackIndices(ByteString packed)
        {
            var offset = 0;
            var triangleStripLen =  UnpackVarInt(packed, ref offset);
            var triangleStrip = new List<UInt16>(triangleStripLen);
            for (int i = 0; i < triangleStripLen; i++)
            {
                triangleStrip.Add(0);
            }
            var numNonDegenerateTriangles = 0;
            for (int i = 0, zeros = 0, a = 0, b = 0, c = 0; i < triangleStripLen - 2; i+=1)
            {
                var res = UnpackVarInt(packed, ref offset);
                a = b;
                b = c;
                c = zeros - res;
                triangleStrip[i] = Convert.ToUInt16(c); 
                if (a != b && a != c && b != c) numNonDegenerateTriangles++;
                if (0 == res) zeros++;
            }

            return triangleStrip;
        }


// unpackOctantMaskAndOctantCountsAndLayerBounds unpacks the octant mask for vertices (W) and layer bounds and octant counts
        public int[] UnpackOctantMaskAndOctantCountsAndLayerBounds(ByteString packed, List<UInt16> indices, List<VertexT> vertices)
        {
            var offset = 0;
            var len =  UnpackVarInt(packed, ref offset);
            var idxI = 0;
            var k = 0;
            var m = 0;
            var layerBounds = new int[len];

            for (var i = 0; i < len; i++)
            {
                if (0 == i % 8)
                {
                    layerBounds[m++] = k;
                }

                var v = UnpackVarInt(packed,ref offset);
                for (var j = 0; j < v; j++)
                {
                    var idx = indices[idxI++];
                    vertices[idx].W = Convert.ToByte(i & 7);
                }
                
                k += v;
            }

            for (; 10 > m; m++) layerBounds[m] = k;
            return layerBounds;
        }


        public class ResultForNormals
        {
            public ResultForNormals(byte[] unpackedForNormals, int count)
            {
                UnpackedForNormals = unpackedForNormals;
                Count = count;
            }

            public byte[] UnpackedForNormals;
            public int Count;
        }

// unpackForNormals unpacks normals info for later mesh normals usage
        public ResultForNormals UnpackForNormals(NodeData nodeData)
        {
            int F1(int v, int l)
            {
                if (4 >= l)
                    return (v << l) + (v & (1 << l) - 1);
                if (6 >= l)
                {
                    var r = 8 - l;
                    return (v << l) + (v << l >> r) + (v << l >> r >> r) + (v << l >> r >> r >> r);
                }

                return -(v & 1);
            }

            byte F2(double c)
            {
                var cr = (int) Math.Round(c);
                if (cr < 0) return 0;
                if (cr > 255) return 255;
                return Convert.ToByte(cr);
            }

            var input = nodeData.ForNormals;
            var data = input.Memory.ToArray();
            var size = input.Length;
            var count = size / 2;
            int s = data[2];
            //     data += 3;

            var output = new byte[3 * count];

            for (var i = 0; i < count; i++)
            {
                double a = F1(data[0 + i], s) / 255.0;
                double f = F1(data[count + i], s) / 255.0;

                double b = a, c = f, g = b + c, h = b - c;
                int sign = 1;

                if (!(.5 <= g && 1.5 >= g && -.5 <= h && .5 >= h))
                {
                    sign = -1;
                    if (.5 >= g)
                    {
                        b = .5 - f;
                        c = .5 - a;
                    }
                    else
                    {
                        if (1.5 <= g)
                        {
                            b = 1.5 - f;
                            c = 1.5 - a;
                        }
                        else
                        {
                            if (-.5 >= h)
                            {
                                b = f - .5;
                                c = a + .5;
                            }
                            else
                            {
                                b = f + .5;
                                c = a - .5;
                            }
                        }
                    }

                    g = b + c;
                    h = b - c;
                }


                a = Math.Min(Math.Min(2 * g - 1, 3 - 2 * g), Math.Min(2 * h + 1, 1 - 2 * h)) * sign;
                b = 2 * b - 1;
                c = 2 * c - 1;
                var m = 127 / Math.Sqrt(a * a + b * b + c * c);

                output[3 * i + 0] = F2(m * a + 127);
                output[3 * i + 1] = F2(m * b + 127);
                output[3 * i + 2] = F2(m * c + 127);
            }

            return new ResultForNormals(output, 3 * count);
        }

// unpackNormals unpacks normals indices in mesh using normal data from NodeData
        public ResultForNormals UnpackNormals(Mesh mesh, byte[] unpackedForNormals)
        {
            var normals = mesh.Normals;
            byte[] newNormals;
            int count;
            if (mesh.HasNormals)
            {
                count = normals.Memory.ToArray().Length / 2;
                newNormals = new byte[count * 3/2];
                var input = normals.Memory.ToArray();
                for (var i = 0; i < newNormals.Length/4 - 4; ++i)
                {
                    int j = input[i] + (input[count + i] << 8);
                    newNormals[4 * i + 0] = unpackedForNormals[3 * j + 0];
                    newNormals[4 * i + 1] = unpackedForNormals[3 * j + 1];
                    newNormals[4 * i + 2] = unpackedForNormals[3 * j + 2];
                    newNormals[4 * i + 3] = 0;
                }
            }
            else
            {
                count = (mesh.Vertices.Length / 3) * 8;
                newNormals = new byte[count * 4];
                for (var i = 0; i < count; ++i)
                {
                    newNormals[4 * i + 0] = 127;
                    newNormals[4 * i + 1] = 127;
                    newNormals[4 * i + 2] = 127;
                    newNormals[4 * i + 3] = 0;
                }
            }

            return new ResultForNormals(newNormals, 4 * count);
        }

        public struct NodeDataPathAndFlagsT
        {
            public string Path;
            public int Flags;
            public int Level;

            public NodeDataPathAndFlagsT(string path, int flags, int level)
            {
                Path = path;
                Flags = flags;
                Level = level;
            }
        };

// unpackPathAndFlags unpacks path, flags and level (strlen(path)) from node metadata
        public NodeDataPathAndFlagsT UnpackPathAndFlags(NodeMetadata nodeMeta)
        {
            NodeDataPathAndFlagsT GetPathAndFlags(uint pathId)
            {
                var path = "";
                var level = 1 + (Convert.ToInt32(pathId) & 3);
                pathId >>= 2;
                for (int i = 0; i < level; i++)
                {
                    path += '0';
                    path += pathId & 7;
                    pathId >>= 3;
                }

                return new NodeDataPathAndFlagsT(path, (int) pathId, level);
            }

            NodeDataPathAndFlagsT result = GetPathAndFlags(nodeMeta.PathAndFlags);
            result.Path += '\0';
            return result;
        }
    }
}