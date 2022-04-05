using System;
using System.IO;
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
            Console.WriteLine("Verticals");
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                Console.WriteLine( ver[i].X + " " +  ver[i].Y + " " + ver[i].Z);
            }

            var ind = mesh.Indices;
            Console.WriteLine("Indices");
            Console.WriteLine(ind.Count);
            // for (var i = 0; i < 10; i++)
            // {
            //     Console.WriteLine(ind[i]);
            // }

            // var norm = mesh.Normals;
            // Console.WriteLine("Normals");
            // for (var i = 0; i < 10; i++)
            // {
            //     Console.WriteLine(norm[i].X + " " +norm[i].Y + "  " + norm[i].Z);
            // }

        }
    }
}