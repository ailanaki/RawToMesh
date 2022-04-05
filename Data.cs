using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Collections;

namespace GeoGlobetrotterProtoRocktree
{
    public class Data
{
    public List<CurrentMesh> Meshes = new List<CurrentMesh>();
    private DecoderRockTree decoderRockTree = new DecoderRockTree();
    private NodeData _nodeData;
    private RepeatedField<double> _ma; // nodeData.MatrixGlobeFromMesh
    private int SCALE = 10;

    public Data(NodeData nodeData)
    {
        _nodeData = nodeData;
        _ma = nodeData.MatrixGlobeFromMesh;
        foreach (var preMesh in nodeData.Meshes)
        {
            var cur = new CurrentMesh(ToVertice(preMesh), ToIndices(preMesh), ToNormals(preMesh));
            Meshes.Add(cur);
        }
    }

    public Data(string nameFile)
    {  var input = File.ReadAllBytes(nameFile);
        var nodeData = NodeData.Parser.ParseFrom(input);
        _nodeData = nodeData;
        _ma = nodeData.MatrixGlobeFromMesh;
        foreach (var preMesh in nodeData.Meshes)
        {
            var cur = new CurrentMesh(ToVertice(preMesh), ToIndices(preMesh), ToNormals(preMesh));
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

        public CurrentMesh(List<Vertice> vertices, List<int> indices, List<Normal> normals)
        {
            Vertices = vertices;
            Indices = indices;
            Normals = normals;
        }
    }

    private List<Vertice> ToVertice(Mesh mesh)
    {
        double min_x = Double.MaxValue, max_x = Double.MinValue;
        double min_y = Double.MaxValue, max_y = Double.MinValue;
        double min_z = Double.MaxValue, max_z = Double.MinValue;
        var answer = new List<Vertice>();
        var vert = decoderRockTree.UnpackVertices(mesh.Vertices);
        var uvOffset = new List<float>(2);
        var uvScale = new List<float>(2);
        for (var i = 0; i < 2; i++)
        {
            uvOffset.Add(0);
            uvScale.Add(0);
        }

      //  var result = decoderRockTree.UnpackTexCoords(mesh.TextureCoordinates, vert, vert.Count, uvOffset, uvScale);
        var preVer = mesh.Vertices.ToByteArray();
        var tex = mesh.Texture;
        for (var i = 0; i < preVer.Length; i+=8)
        {
            var x = preVer[i];
            var y = preVer[i + 1];
            var z = preVer[i + 2];
            var w = 1;
            var x1 = x * _ma[0] + y * _ma[4] + z * _ma[8] + w * _ma[12];
            var y1 = x * _ma[1] + y * _ma[5] + z * _ma[9] + w * _ma[13];
            var z1 = x * _ma[2] + y * _ma[6] + z * _ma[10] + w * _ma[14];
            var w1 = x * _ma[3] + y * _ma[7] + z * _ma[11] + w * _ma[15];
            
            min_x = Math.Min(x1, min_x);
            min_y = Math.Min(y1, min_y);
            min_z = Math.Min(z1, min_z);
            max_x = Math.Max(x1, max_x);
            max_y = Math.Max(y1, max_y);
            max_z = Math.Max(z1, max_z);
            
            var ut = 0.0;
            var vt = 0.0;
            if (mesh.UvOffsetAndScale != null && mesh.UvOffsetAndScale.Count >= 3)
            {
                var u1 = preVer[i + 4];
                var u2 = preVer[i + 5];
                var v1 = preVer[i + 6];
                var v2 = preVer[i + 7];

                var u = u2 * 256 + u1;
                var v = v2 * 256 + v1;

                ut = (u + mesh.UvOffsetAndScale[0]) * mesh.UvOffsetAndScale[2];
                vt = (v + mesh.UvOffsetAndScale[1]) * mesh.UvOffsetAndScale[3];

                if (tex[i].Format == Texture.Types.Format.CrnDxt1)
                {
                    vt = 1 - vt;
                }
            }

            answer.Add(new Vertice(x1, y1, z1, ut, vt));
        }
        var center_x = (max_x + min_x) / 2;
        var center_y = (max_y + min_y) / 2;
        var center_z = (max_z + min_z) / 2;
        var distance_x = Math.Abs(max_x - min_x);
        var distance_y = Math.Abs(max_y - min_y);
        var distance_z = Math.Abs(max_z - min_z);
        var max_distance = Math.Max(Math.Max(distance_x, distance_y), distance_z);
        
        foreach (var vertex in answer)
        {
            vertex.X = (vertex.X - center_x) / max_distance * SCALE;
            vertex.Y = (vertex.Y - center_y) / max_distance * SCALE;
            vertex.Z = (vertex.Z - center_z) / max_distance * SCALE;
        }
        
        return answer;
    }

    private List<int> ToIndices(Mesh mesh)
    { 
        var preInd = decoderRockTree.UnpackIndices(mesh.Indices);
        var triangle_groups = new List<List<int>>();
        for (int i = 0; i < preInd.Count - 2; i += 1) {
            var a = preInd[i + 0];
            var b = preInd[i + 1];
            var c = preInd[i + 2];
            if (a == b || a == c || b == c) {
                continue;
            }
            if ((i & 1) == 0)
            {
                triangle_groups.Add(new List<int>{a, c, b});
            }
            else {
                triangle_groups.Add(new List<int>{a, b, c});
            }
        }

        var ind = new List<int>();

        foreach (var k in triangle_groups){
            ind.Add(k[0]);
            ind.Add(k[1]);
            ind.Add(k[2]);
        }
        return ind;
    }

    private List<Normal> ToNormals(Mesh mesh)
    {
        var answer = new List<Normal>();
        var res = decoderRockTree.UnpackNormals(mesh, decoderRockTree.UnpackForNormals(_nodeData).UnpackedForNormals);
        var normals = res.UnpackedForNormals;
        for (var i = 0; i < res.Count; i += 4)
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
}
}

