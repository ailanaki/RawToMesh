using GeoGlobetrotterProtoRocktree;
using Google.Protobuf;
using Google.Protobuf.Collections;;

void PrintMessage(IMessage message)
{
    var descriptor = message.Descriptor;
    foreach (var field in descriptor.Fields.InDeclarationOrder())
    {
        Console.WriteLine(
            "Field {0} ({1})",
            field.FieldNumber,
            field.Name);
    }
}


BulkMetadata node;
var input = File.ReadAllBytes(
    "/Users/aishayakupova/RiderProjects/s1/raw/BulkMetadata!1m2!1s20527061!2u919.raw");
node = BulkMetadata.Parser.ParseFrom(input);
DecoderRockTree decoderRockTree = new DecoderRockTree();

// foreach (var meta in node.NodeMetadata)
// { 
//     var path = decoderRockTree.unpackPathAndFlags(meta);
//     Console.WriteLine(path.path);
// }

input = File.ReadAllBytes(
    "/Users/aishayakupova/RiderProjects/s1/raw/NodeData!1m2!1s20527061605273514!2u906!2e6!4b0.raw");
NodeData metadata = NodeData.Parser.ParseFrom(input);
Data data = new Data(metadata);
var mesh = data.Meshes[0];
var ver = mesh.vertices;
for (int i = 0; i < 10; i++)
{   Console.WriteLine(ver[i].x);    
    Console.WriteLine(ver[i].y);
    Console.WriteLine(ver[i].z);
    Console.WriteLine(ver[i].u);
    Console.WriteLine(ver[i].v);
}







