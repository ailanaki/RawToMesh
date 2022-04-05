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
                "/Users/aishayakupova/RiderProjects/s1/raw/BulkMetadata!1m2!1s20527061!2u919.raw");
            BulkMetadata node =  BulkMetadata.Parser.ParseFrom(input);
            DecoderRockTree decoderRockTree = new DecoderRockTree();

// foreach (var meta in node.NodeMetadata)
// { 
//     var path = decoderRockTree.unpackPathAndFlags(meta);
//     Console.WriteLine(path.path);
// }

            input = File.ReadAllBytes(
                    "/Users/aishayakupova/RiderProjects/s1/raw/NodeData!1m2!1s20527061605273514374!2u905!2e6!4b0.raw");
            var metadata = NodeData.Parser.ParseFrom(input);
            var data = new Data(metadata);
            var mesh = data.Meshes[0];
            var ver = mesh.Vertices;
            Console.WriteLine("Verticals");
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                Console.WriteLine((float) ver[i].X + " " + (float) ver[i].Y + " " + (float) ver[i].Z);
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