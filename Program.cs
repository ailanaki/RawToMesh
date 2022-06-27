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
    class Programm
    {
        private static List<Vector3> _vertice;


        public static void Main(string[] args)
        {
            MakeMesh();
        }


        static void MakeMesh()
        {
            var dataList = createDataList(
                "/Users/aishayakupova/RiderProjects/s1/raw/");


            foreach (var data in dataList)
            {
                var meshList = data.Meshes;
                foreach (var curMesh in meshList)
                {
                    var sizeT = 0;
                    _vertice = new List<Vector3>();
                    List<Data.Vertice> ver = new List<Data.Vertice>();
                    var triangles = new List<int>();
                    AddCurrentVertice(curMesh, ref ver);
                    AddCurrentIndices(sizeT, curMesh, ref triangles);
                    sizeT += curMesh.Vertices.Count;
                    foreach (var vertex in ver)
                    {
                        var x = (vertex.X);
                        var y = (vertex.Y);
                        var z = (vertex.Z);
                        _vertice.Add(new Vector3((float) x, (float) y, (float) z));
                    }

                    var geoCoord = curMesh.toGPS(curMesh.Vertices);
                    var converter = new GeoCoordsConverter(geoCoord[0].Latitude, geoCoord[0].Longitude);
                    var newCoords = new List<Vector3>();
                    foreach (var coord in geoCoord)
                    {
                        newCoords.Add(converter.MapToScene(coord));
                    }

                    printCoords(newCoords);
                }
            }
        }

        static void printTriangles(List<int> triangles)
        {
            var output = new StreamWriter("out.txt");
            for (int i = 0; i < triangles.Count - 2; i += 3)
            {
                output.WriteLine(triangles[i] + " " +
                                 triangles[i + 1] + " " +
                                 triangles[i + 2]);
            }

            output.Close();
        }

        static void printCoords(List<Vector3> coords)
        {
            var output = new StreamWriter("out.txt");

            foreach (var ver in coords)
            {
                output.WriteLine((ver.X) + " " +
                                 (ver.Y) + " " +
                                 (ver.Z));
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

        static void printGPS(List<Data.GPSCoords> coords)
        {
            var output = new StreamWriter("out.txt");


            for (int i = 0; i < coords.Count; i += 1)
            {
                output.WriteLine((coords[i].Latitude) + " " +
                                 (coords[i].Longitude) + " " +
                                 (coords[i].Height));
            }

            output.Close();
        }

        static void printNormals(List<Data.Normal> normals)
        {
            var output = new StreamWriter("out.txt");


            for (int i = 0; i < normals.Count; i += 1)
            {
                output.WriteLine((normals[i].X) + " " +
                                 (normals[i].Y) + " " +
                                 (normals[i].Z));
            }

            output.Close();
        }

        static void printUV(List<Data.Vertice> uv)
        {
            var output = new StreamWriter("out.txt");
            for (int i = 0; i < uv.Count; i += 1)
            {
                output.WriteLine(uv[i].U + " " +
                                 uv[i].V);
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
                ver.Add(new Data.Vertice(vertex.X, vertex.Y, vertex.Z, vertex.U, vertex.V));
            }
        }

        static void AddCurrentIndices(int add, Data.CurrentMesh currentMesh, ref List<int> _triangles)
        {
            foreach (var inx in currentMesh.Indices)
            {
                _triangles.Add(inx + add);
            }
        }
    }
}