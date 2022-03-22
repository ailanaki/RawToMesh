using System.Diagnostics;
using Google.Protobuf;

namespace GeoGlobetrotterProtoRocktree;

public class DecoderRockTree
{
    const int MAX_LEVEL = 20;

    public class ResultOfUnpackVarInt
    {
        public ResultOfUnpackVarInt(short lenght, int offset)
        {
            this.lenght = lenght;
            this.offset = offset;
        }

        public short lenght;
        public int offset;
    }

// unpackVarInt unpacks variable length integer from proto (like coded_stream.h)
    public ResultOfUnpackVarInt unpackVarInt(ByteString packed, int index)
    {
        var data = packed.Memory.ToArray();
        short c = 0;
        int d = 1, e;
        do
        {
            e = data[index++];
            c += Convert.ToInt16((e & 0x7F) * d);
            d <<= 7;
        } while ((e & 0x80) != 0);

        return new ResultOfUnpackVarInt(c, index);
    }

// vertex is a packed struct for an 8-byte-per-vertex array
    public class vertex_t
    {
        public byte x, y, z; // position
        public byte w; // octant mask
        public Int16 u, v; // texture coordinates

        public vertex_t(byte x, byte y, byte z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    };


// unpackVertices unpacks vertices XYZ to new 8-byte-per-vertex array
    public List<vertex_t> unpackVertices(ByteString packed)
    {
        var data = packed.Memory.ToArray();
        var count = packed.Length / 3;
        var vtx = new List<vertex_t>(count);
        byte x = 0, y = 0, z = 0; // 8 bit for % 0x100;
        for (var i = 0; i < count; i++)
        {
            vtx.Add(new vertex_t(data[count * 0 + i], data[count * 1 + i],data[count * 2 + i]));
        }

        return vtx;
    }

    public class ResultOfUnpackTexCoords
    {
        public ResultOfUnpackTexCoords(List<float> uv_offset, List<float> uv_scale, List<vertex_t> vertices)
        {
            this.uv_offset = uv_offset;
            this.uv_scale = uv_scale;
            this.vertices = vertices;
        }

        public List<float> uv_offset;
        public List<float> uv_scale;
        public List<vertex_t> vertices;
    }

// unpackTexCoords unpacks texture coordinates UV to 8-byte-per-vertex-array
    public ResultOfUnpackTexCoords unpackTexCoords(ByteString packed, List<vertex_t> vertices, int
        vertices_len, List<float> uv_offset, List<float> uv_scale)
    {
        var data = new List<byte>(packed.Memory.ToArray());
        var count = vertices_len;
        var u_mod = 1 + (data.Count/16 + 0);
        var v_mod = 1 + (data.Count/16 + 2);
        data.Add(0);
        data.Add(0); 
        data.Add(0); 
        data.Add(0); 
        var vtx = vertices;
        ushort u = 0, v = 0;
        for (var i = 0; i < count; i++)
        {
            vtx[i].u = Convert.ToInt16(u + data[count * 0 + i] + (data[count * 2 + i] << 8)% u_mod);
            vtx[i].v = Convert.ToInt16(v + data[count * 1 + i] + (data[count * 3 + i] << 8) % v_mod);
        }

        uv_offset[0] = (float) 0.5;
        uv_offset[1] = (float) 0.5;
        uv_scale[0] = (float) 1.0/ u_mod;
        uv_scale[1] = (float) 1.0/ v_mod;
        return new ResultOfUnpackTexCoords(uv_offset, uv_scale, vertices);
    }

// unpackIndices unpacks indices to triangle strip
    public List<short> unpackIndices(ByteString packed)
    {
        var offset = 0;
        var res = unpackVarInt(packed, offset);
        var triangleStripLen = res.lenght;
        var triangleStrip = new List<short>(triangleStripLen);
        for (int i = 0; i < triangleStripLen; i++)
        {
            triangleStrip.Add(0);
        }
        var numNonDegenerateTriangles = 0;
        short zeros = 0, a  = 0, b = 0, c = 0;
        for (int i = 0; i < triangleStripLen; i++) {
            res = unpackVarInt(packed, offset);
            offset = res.offset;
            triangleStrip[i] = a;
            a = b;
            b = c;
            c = Convert.ToInt16(zeros - res.lenght);
            if (a != b && a != c && b != c) numNonDegenerateTriangles++;
            if (0 == res.lenght) zeros++;
        }
        return triangleStrip;
    }
    
    
// unpackOctantMaskAndOctantCountsAndLayerBounds unpacks the octant mask for vertices (W) and layer bounds and octant counts
    public void unpackOctantMaskAndOctantCountsAndLayerBounds(ByteString packed, List<byte> indices,  int
        indices_len, List<vertex_t> vertices, int vertices_len, int[] layer_bounds)
    {
        
        
        var res = unpackVarInt(packed, 0);
        var len = res.lenght;
        var offset = res.offset;
        var idx_i = 0;
        var k = 0;
        var m = 0;

        for (var i = 0; i < len; i++)
        {
            if (0 == i % 8)
            {
                layer_bounds[m++] = k;
            }

            var v = unpackVarInt(packed, offset);
            for (var j = 0; j < v.lenght; j++)
            {
                var idx = indices[idx_i++];
                vertices[idx].w = Convert.ToByte(i & 7);
            }

            offset = v.offset;
            k += v.lenght;
        }

        for (; 10 > m; m++) layer_bounds[m] = k;
    }
    

    public class ResultForNormals
    {
        public ResultForNormals(byte[,] unpacked_for_normals, int count)
        {
            this.unpacked_for_normals = unpacked_for_normals;
            this.count = count;
        }

        public byte[,] unpacked_for_normals;
        public int count;
    }
// unpackForNormals unpacks normals info for later mesh normals usage
    ResultForNormals unpackForNormals(NodeData nodeData, byte[,] unpacked_for_normals)
    {
        int f1 (int v, int l){
            if (4 >= l)
                return (v << l) + (v & (1 << l) - 1);
            if (6 >= l)
            {
                var r = 8 - l;
                return (v << l) + (v << l >> r) + (v << l >> r >> r) + (v << l >> r >> r >> r);
            }

            return -(v & 1);
        }
        ;
        byte f2 (double c){
            var cr = (int) Math.Round(c);
            if (cr < 0) return 0;
            if (cr > 255) return 255;
            return Convert.ToByte(cr);
        }
        ;
        var input = nodeData.ForNormals;
        var data = input.Memory.ToArray();
        var size = input.Length;
        var count = size / 16;
        int s = data[2];
   //     data += 3;

        var output = new byte[3 * count];

        for (var i = 0; i < count; i++)
        {
            double a = f1(data[0 + i], s) / 255.0;
            double f = f1(data[count + i], s) / 255.0;

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
            

            a = Math.Min(Math.Min(2 * g - 1, 3 - 2 * g),Math.Min(2 * h + 1, 1 - 2 * h)) * sign;
            b = 2 * b - 1;
            c = 2 * c - 1;
            var m = 127 / Math.Sqrt(a * a + b * b + c * c);

            output[3 * i + 0] = f2(m * a + 127);
            output[3 * i + 1] = f2(m * b + 127);
            output[3 * i + 2] = f2(m * c + 127);
        }
        
        return new ResultForNormals(unpacked_for_normals, 3 * count);
    }

// unpackNormals unpacks normals indices in mesh using normal data from NodeData
    public ResultForNormals unpackNormals(Mesh mesh, byte[] unpacked_for_normals,  int
        unpacked_for_normals_len, byte[,] unpacked_normals)
    {
        var normals = mesh.Normals;
        byte[,]  new_normals;
        int count = 0;
        if (mesh.HasNormals){
            count = normals.Length / 2;
            new_normals = new byte[count, 4];
            var input =  normals.Memory.ToArray();
            for (var i = 0; i < count; ++i)
            {
                int j = input[i] + (input[count + i] << 8);
                new_normals[4 * i, 0] = unpacked_for_normals[3 * j + 0];
                new_normals[4 * i, 1] = unpacked_for_normals[3 * j + 1];
                new_normals[4 * i, 2] = unpacked_for_normals[3 * j + 2];
                new_normals[4 * i, 3] = 0;
            }
        }else
        {
            count = (mesh.Vertices.Length / 3) * 8;
            new_normals = new byte[count, 4];
            for (var i = 0; i < count; ++i)
            {
                new_normals[4 * i , 0] = 127;
                new_normals[4 * i , 1] = 127;
                new_normals[4 * i , 2] = 127;
                new_normals[4 * i , 3] = 0; 
            }
        }
        
        return new ResultForNormals(new_normals, 4 * count);
    }

    public struct NodeDataPathAndFlagsT
    {
        public String path;
        public int flags;
        public int level;
        public uint path_id;
        public NodeDataPathAndFlagsT(String path, int flags, int level, uint pathId)
        {
            this.path = path;
            this.flags = flags;
            this.level = level;
            this.path_id = pathId;
        }
    };

// unpackPathAndFlags unpacks path, flags and level (strlen(path)) from node metadata
    public NodeDataPathAndFlagsT unpackPathAndFlags(NodeMetadata node_meta)
    {
        NodeDataPathAndFlagsT getPathAndFlags(uint path_id){
            String path = "";
            int level = 0, flags = 0;
            level = 1 + (Convert.ToInt32(path_id) & 3);
            path_id >>= 2;
            for (int i = 0; i < level; i++)
            {
                path += '0';
                path += (path_id & 7);
                path_id >>= 3;
            }
            return new NodeDataPathAndFlagsT(path,flags,level,path_id);
        }

        NodeDataPathAndFlagsT result = getPathAndFlags(node_meta.PathAndFlags);
        result.path += '\0';
        return result;
    }

     // struct OrientedBoundingBox
     // {
     //     Vector3d center;
     //     Vector3d extents;
     //     Matrix3d orientation;
     // };
//
//     OrientedBoundingBox unpackObb(ByteString packed, Vector3f head_node_center, float meters_per_texel) {
//         Debug.Assert(packed.Length == 15);
//         auto data = (uint8_t*) packed.data();
//         OrientedBoundingBox obb;
//         obb.center[0] = *(int16_t*) (data + 0) * meters_per_texel + head_node_center[0];
//         obb.center[1] = *(int16_t*) (data + 2) * meters_per_texel + head_node_center[1];
//         obb.center[2] = *(int16_t*) (data + 4) * meters_per_texel + head_node_center[2];
//         obb.extents[0] = *(uint8_t*) (data + 6) * meters_per_texel;
//         obb.extents[1] = *(uint8_t*) (data + 7) * meters_per_texel;
//         obb.extents[2] = *(uint8_t*) (data + 8) * meters_per_texel;
//         Vector3f euler;
//         euler[0] = *(uint16_t*) (data + 9) * M_PI / 32768.0f;
//         euler[1] = *(uint16_t*) (data + 11) * M_PI / 65536.0f;
//         euler[2] = *(uint16_t*) (data + 13) * M_PI / 32768.0f;
//         double c0 = cosf(euler[0]);
//         double s0 = sinf(euler[0]);
//         double c1 = cosf(euler[1]);
//         double s1 = sinf(euler[1]);
//         double c2 = cosf(euler[2]);
//         double s2 = sinf(euler[2]);
//         auto orientation = obb.orientation.data();
//         orientation[0] = c0 * c2 - c1 * s0 * s2;
//         orientation[1] = c1 * c0 * s2 + c2 * s0;
//         orientation[2] = s2 * s1;
//         orientation[3] = -c0 * s2 - c2 * c1 * s0;
//         orientation[4] = c0 * c1 * c2 - s0 * s2;
//         orientation[5] = c2 * s1;
//         orientation[6] = s1 * s0;
//         orientation[7] = -c0 * s1;
//         orientation[8] = c1;
//
//         return obb;
//     }
}