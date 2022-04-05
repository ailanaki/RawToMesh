using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoGlobetrotterProtoRocktree;
using Google.Protobuf;

namespace s1
{
    class Programm
    {
        public static void Main(string[] args)
        {
            var input = File.ReadAllBytes(
                "/Users/aishayakupova/RiderProjects/s1/raw/NodeData!1m2!1s20527061605273514!2u906!2e6!4b0.raw");
            var metadata = NodeData.Parser.ParseFrom(input);
            var data = new Data(metadata);
            var mesh = data.Meshes[0];
            var ver = mesh.Vertices;
            Console.WriteLine("Vertices");
            // for (var i = 0; i < mesh.Vertices.Count; i++)
            // {
            //     Console.WriteLine( ver[i].X + " " +  ver[i].Y + " " + ver[i].Z);
            // }
            Console.WriteLine(ver.Count);

            var ind = mesh.Indices;
            Console.WriteLine("Indices");
            // for (var i = 0; i < 10; i++)
            // {
            //     Console.WriteLine(ind[i]);
            // }
            var norm = mesh.Normals;
            Console.WriteLine("Normals");
            Console.WriteLine(metadata.Meshes[0].Normals.Length);
            Console.WriteLine(metadata.Meshes[0].Vertices.Length);

        }
    }
}