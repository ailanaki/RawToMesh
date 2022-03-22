using Google.Protobuf.Collections;

namespace GeoGlobetrotterProtoRocktree;

public class Data
{

    public List<CurrentMesh> Meshes = new List<CurrentMesh>();
    private NodeData nodeData;
    private RepeatedField<double> ma; // nodeData.MatrixGlobeFromMesh

    public Data(NodeData nodeData)
    {
        this.nodeData = nodeData;
        this.ma = nodeData.MatrixGlobeFromMesh;
        foreach (var preMesh in nodeData.Meshes)
        {
            CurrentMesh cur = new CurrentMesh();
            cur.vertices = toVertice(preMesh);
        //    cur.normals = toNormals(preMesh);
            cur.indices = toIndices(preMesh);
            Meshes.Add(cur);
        }

    }

    public class Vertice
    {
        public double x, y, z; // position
        public byte w; // octant mask
        public double u, v; // texture coordinates

        public Vertice(double x, double y, double z, double u, double v)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.u = u;
            this.v = v;
        }
    }

    public class CurrentMesh
    {
        public List<Vertice> vertices;
        public List<short> indices;
        public List<int> normals;
    }

    private List<Vertice> toVertice(Mesh mesh)
    {
        DecoderRockTree decoderRockTree = new DecoderRockTree();
        List<Vertice> answer = new List<Vertice>();
        var vert = decoderRockTree.unpackVertices(mesh.Vertices);
        var uv_offset = new List<float>(2);
        var uv_scale = new List<float>(2);
        for (int i = 0; i < 2; i++)
        {
            uv_offset.Add(0);
            uv_scale.Add(0);
        }

        var result = decoderRockTree.unpackTexCoords(mesh.TextureCoordinates, vert, vert.Count, uv_offset, uv_scale);
        var preVer = result.vertices;
        var tex = mesh.Texture;
        for (int i = 0; i < preVer.Count; i++)
        {
            var x = preVer[i].x;
            var y = preVer[i].y;
            var z = preVer[i].z;
            var w = 1;
            double _x = x * ma[0] + y * ma[4] + z * ma[8] + w * ma[12];
            double _y = x * ma[1] + y * ma[5] + z * ma[9] + w * ma[13];
            double _z = x * ma[2] + y * ma[6] + z * ma[10] + w * ma[14];
            double _w = x * ma[3] + y * ma[7] + z * ma[11] + w * ma[15];
            var ut = 0.0;
            var vt = 0.0;
            if (mesh.UvOffsetAndScale != null && mesh.UvOffsetAndScale.Count >= 3) {
                var u1 = preVer[i + 4].u;
                var u2 = preVer[i + 5].u;
                var v1 = preVer[i + 6].v;
                var v2 = preVer[i + 7].v;

                var u = u2 * 256 + u1;
                var v = v2 * 256 + v1;

                 ut = (u + mesh.UvOffsetAndScale[0]) * mesh.UvOffsetAndScale[2];
                 vt = (v + mesh.UvOffsetAndScale[1]) * mesh.UvOffsetAndScale[3];

                if (tex[i].Format == Texture.Types.Format.CrnDxt1)
                {
                    vt = 1 - vt;
                }
            }

            answer.Add(new Vertice(_x, _y, _z, ut, vt));
        }

        return answer;
    }

    private List<short> toIndices(Mesh mesh)
    {
        DecoderRockTree decoderRockTree = new DecoderRockTree();
        return decoderRockTree.unpackIndices(mesh.Indices);
    }

    // private List<int> toNormals(Mesh mesh)
    // {
    //     
    // }


}