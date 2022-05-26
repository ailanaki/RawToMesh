using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using GeoGlobetrotterProtoRocktree;

namespace s1
{
    class Pair<T1, T2>
    {
        public T1 First;
        public T2 Second;

        public Pair(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }

    class Programm
    {
        static double min_x = Double.MaxValue, max_x = Double.MinValue;
        static double min_y = Double.MaxValue, max_y = Double.MinValue;
        static double min_z = Double.MaxValue, max_z = Double.MinValue;
        private static List<Vector3> _vertice;

        static Planetoid.Bulk getPlanetoid(Planetoid planetoid)
        {
            var bulk = planetoid.RootBulk;
            planetoid.GetBulk(bulk.Request, bulk);
            return bulk;
        }

        static List<List<double>> perspective(double fov_rad, double aspect_ratio, double near, double far)
        {
            var tan_half_fovy = Math.Tan(fov_rad / 2.0);

            List<List<double>> res = new List<List<double>>();
            for (int i = 0; i < 4; i++)
            {
                List<double> tmp = new List<double>();
                for (int j = 0; j < 4; j++)
                {
                    tmp.Add(0);
                }

                res.Add(tmp);
            }

            res[0][0] = 1.0 / (aspect_ratio * tan_half_fovy);
            res[1][1] = 1.0 / (tan_half_fovy);
            res[2][2] = -(far + near) / (far - near);
            res[3][2] = -1.0;
            res[2][3] = -(2.0 * far * near) / (far - near);
            return res;
        }


        public static void Main(string[] args)
        {
            // var planetoid = new Planetoid("raw/PlanetoidMetadata.raw");
            // //  var bulk = getPlanetoid(planetoid);
            // //  var req = planetoid.createNodeDataRequest()
            // //var node = planetoid.GetNode()
            // var current_bulk = getPlanetoid(planetoid);
            // var planet_radius = planetoid.Radius;
            //
            // string[] octs =
            // {
            //     "0", "1", "2", "3", "4", "5", "6", "7"
            // };
            // var valid = new List<Pair<string, Planetoid.Bulk>>
            // {
            //     new Pair<string, Planetoid.Bulk>("", current_bulk)
            // };
            // var next_valid = new List<Pair<string, Planetoid.Bulk>>();
            // var potential_nodes = new Dictionary<string, Planetoid.Node>();
            // var potential_bulks = new Dictionary<string, Planetoid.Bulk>();
            //
            // // node culling and level of detail using breadth-first search
            // for (;;)
            // {
            //     foreach (var cur2 in valid)
            //     {
            //         var cur = cur2.First;
            //         var bulk = cur2.Second;
            //         if (cur.Length > 0 && cur.Length % 4 == 0)
            //         {
            //             var ind = (int) Math.Floor((double) ((cur.Length - 1) / 4));
            //             var rel = cur.Substring(ind * 4, 4);
            //             var bulk_kv = bulk.Bulks[rel];
            //             var has_bulk = bulk_kv != null;
            //             if (!has_bulk) continue;
            //             var b = bulk_kv;
            //             potential_bulks[cur] = b;
            //             planetoid.GetBulk(b.Request,b);
            //             bulk = b;
            //         }
            //
            //         potential_bulks[cur] = bulk;
            //         foreach (var o in octs)
            //         {
            //             var nxt = cur + o;
            //             var ind = (int) Math.Floor((float) ((nxt.Length - 1) / 4)) * 4;
            //             string nxt_rel;
            //             if (ind + 4 > nxt.Length) nxt_rel = nxt.Substring(ind, nxt.Length);
            //             else nxt_rel = nxt.Substring(ind, 4);
            //           //  Console.WriteLine(nxt_rel);
            //             if (!bulk.Nodes.ContainsKey(nxt_rel)) continue;
            //             var node_kv = bulk.Nodes[nxt_rel];
            //             var node = node_kv;
            //            
            //             // cull outside frustum using obb
            //             // todo: check if it could cull more
            //             // if (obb_frustum_outside == classifyObbFrustum(&node->obb, frustum_planes))
            //             // {
            //             //     continue;
            //             // }
            //
            //             // level of detail
            //             /*{
            //                 auto obb_center = node->obb.center;
            //                 auto obb_max_diameter = fmax(fmax(node->obb.extents[0], node->obb.extents[1]), node->obb.extents[2]);			
            //                 
            //                 auto t = Affine3d().Identity();
            //                 t.translate(Vector3d(obb_center.x(), obb_center.y(), obb_center.z()));
            //                 t.scale(obb_max_diameter);
            //                 Matrix4d viewprojection_d;
            //                 for(auto i = 0; i < 16; i++) viewprojection_d.data()[i] = viewprojection.data()[i];
            //                 auto m = viewprojection_d * t;
            //                 auto s = m(3, 3);
            //                 if (s < 0) s = -s; // ?
            //                 auto diameter_in_clipspace = 2 * (obb_max_diameter / s);  // *2 because clip space is -1 to +1
            //                 auto amplify = 4; // todo: meters per texel
            //                 if (diameter_in_clipspace < 0.5 / amplify) {
            //                     continue;
            //                 }
            //             }*/
            //
            //             // {
            //             //     auto t = Affine3d().Identity();
            //             //     t.translate(eye + (eye - node->obb.center).norm() * direction);
            //             //     auto m = viewprojection * t;
            //             //     auto s = m(3, 3);
            //             //     auto texels_per_meter = 1.0f / node->meters_per_texel;
            //             //     auto wh = 768; // width < height ? width : height;
            //             //     auto r = (2.0 * (1.0 / s)) * wh;
            //             //     if (texels_per_meter > r) continue;
            //             // }
            //
            //             next_valid.Add(new Pair<string, Planetoid.Bulk>(nxt, bulk));
            //
            //             if (node.CanHaveData)
            //             {
            //                 potential_nodes[nxt] = node;
            //                 //auto d = (node->obb.center - eye).squaredNorm();
            //                 //dist_nodes[d] = node;
            //                 //dist_nodes.insert(std::make_pair (d, node));
            //             }
            //         }
            //     }
            //     
            //
            //     if (next_valid.Count == 0) break;
            //     valid.Clear();
            //     valid =  new List<Pair<string, Planetoid.Bulk>>(next_valid);
            //     next_valid.Clear();
            // }
            // foreach (var kv in potential_nodes)
            // {
            //     // normal order
            //     //for (auto kv = potential_nodes.rbegin(); kv != potential_nodes.rend(); ++kv) { // reverse order
            //     //for (auto kv = dist_nodes.rbegin(); kv != dist_nodes.rend(); ++kv) { // reverse order
            //     //for (auto kv = dist_nodes.begin(); kv != dist_nodes.end(); ++kv) { // normal order
            //     var node = kv.Value;
            //     planetoid.GetNode(node.Request, ref node);
            // }
            //
            // var node = NodeData.Parser.ParseFrom(
            //     File.ReadAllBytes("raw/NodeData!1m2!1s20527061605273514040!2u905!2e6!4b0.raw"));
            // Data data = new Data(node);
            // var mesh = data.Meshes[1];
            // foreach (var ver in mesh.Vertices)
            // {
            //     Console.WriteLine(ver.X + " " + ver.Y + " "+ ver.Z);
            // }
            //Console.WriteLine(data.Meshes.Count);
            // var add = 1 + 3267;
            // for (int i = 0; i < mesh.Indices.Count - 2; i += 1)
            // {
            //     // normal order
            //     //for (auto kv = potential_nodes.rbegin(); kv != potential_nodes.rend(); ++kv) { // reverse order
            //     //for (auto kv = dist_nodes.rbegin(); kv != dist_nodes.rend(); ++kv) { // reverse order
            //     //for (auto kv = dist_nodes.begin(); kv != dist_nodes.end(); ++kv) { // normal order
            //     var node = kv.Value;
            //     planetoid.GetNode(node.Request, ref node);
            // }
            MakeMesh();
        }


        static void MakeMesh()
        {
            var dataList = createDataList(
                "/Users/aishayakupova/RiderProjects/s1/raw/");

            var sizeT = 0;
             _vertice = new List<Vector3>();
             List<Data.Vertice> ver = new List<Data.Vertice>();
            var _triangles = new List<int>();


            foreach (var data in dataList)
            {
                var meshList = data.Meshes;
                foreach (var curMesh in meshList)
                {
                //var curMesh = data.Meshes[0];
                    AddCurrentVertice(curMesh, ref ver);
                    AddCurrentIndices(sizeT, curMesh, ref _triangles);
                    sizeT += curMesh.Vertices.Count;
               }
            }
            _vertice = Center_Scale(ref ver);
            printVerticals();
            
        }

        static void printTriangles(List<int> _triangles)
        {
            var output = new StreamWriter("out.txt");
            for (int i = 0; i < _triangles.Count - 2; i += 3)
            {
                output.WriteLine((_triangles[i]) + " " +
                                 (_triangles[i + 1]) + " " +
                                 (_triangles[i + 2]));
            }
            
            output.Close();
        }

        static void printVerticals()
        {
            var output = new StreamWriter("out.txt");
                

            for (int i = 0; i < _vertice.Count; i += 1)
            {
                output.WriteLine((_vertice[i].X) + " " +
                                 (_vertice[i].Y) + " " +
                                 (_vertice[i].Z));
            }

            output.Close();
        }
        static void printUV(List<Vector2> _uv)
        {
            var output = new StreamWriter("out.txt");
                

            for (int i = 0; i < _uv.Count; i += 1)
            {
                output.WriteLine((_uv[i].X) + " " +
                                 (_uv[i].Y));
            }

            output.Close();
        }
        

        static List<Data> createDataList(string prefix)
        {
            var ans = new List<Data>();
            var lines = File.ReadAllLines(prefix + "FileNames.txt");
            foreach (var line in lines)
            {
                if (!String.IsNullOrEmpty(line)) ans.Add(new Data(prefix + line));
            }

            return ans;
        }

        static void AddCurrentVertice(Data.CurrentMesh currentMesh, ref List<Data.Vertice> ver)
        {
            foreach (var vertex in currentMesh.Vertices)
            {
                var _x = vertex.X;
                var _y = vertex.Y;
                var _z = vertex.Z;
                ver.Add(new Data.Vertice(_x, _y, _z, 0, 0));
                min_x = Math.Min(_x, min_x);         
                min_y = Math.Min(_y, min_y);
                min_z = Math.Min(_z, min_z);
                max_x = Math.Max(_x, max_x);
                max_y = Math.Max(_y, max_y);
                max_z = Math.Max(_z, max_z);
            }
        }

        static void AddCurrentIndices(int add, Data.CurrentMesh currentMesh, ref List<int> _triangles)
        {
            foreach (var inx in currentMesh.Indices)
            {
                _triangles.Add(inx + add);
            }
        }

        static List<Vector3> Center_Scale(ref List<Data.Vertice> ver)
        {
            var center_x = (max_x + min_x) / 2;
            var center_y = (max_y + min_y) / 2;
            var center_z = (max_z + min_z) / 2;
            var distance_x = Math.Abs(max_x - min_x);
            var distance_y = Math.Abs(max_y - min_y);
            var distance_z = Math.Abs(max_z - min_z);
            var max_distance = Math.Max(Math.Max(distance_x, distance_y), distance_z);
            var SCALE = 10;
            var ans = new List<Vector3>();
            foreach (var vertex in ver)
            {
                var x = (vertex.X - center_x) / max_distance * SCALE;
                var y = (vertex.Y - center_y) / max_distance * SCALE;
                var z = (vertex.Z - center_z) / max_distance * SCALE;
                ans.Add(new Vector3((float) x, (float)y, (float) z));
            }

            return ans;
        }
    }
}