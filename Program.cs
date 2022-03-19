using GeoGlobetrotterProtoRocktree;
using Google.Protobuf;
using Google.Protobuf.Collections;

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

NodeData node;
var input = File.ReadAllBytes(
    "/Users/aishayakupova/RiderProjects/s1/raw/NodeData!1m2!1s20527061605273514!2u906!2e6!4b0.raw");
node = NodeData.Parser.ParseFrom(input);
List<NodeData> nodeDatas;
foreach (var mesh in node.Meshes) {
    Console.WriteLine(mesh.HasVertices);
}
Mesh mesh1 = node.Meshes[0];
Console.WriteLine(mesh1.Vertices.Memory);

