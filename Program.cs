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

void printVertices(float[,] vertices, int i)
{
    Console.WriteLine(vertices[i,0]);
    Console.WriteLine(vertices[i,1]);
    Console.WriteLine(vertices[i,2]);
}
int vc(int c,int  d)
{
    return 0 == d ? c + 1 & -2 : 1 == d ? c | 1 : c + 2;
};

float[,] decode(Mesh d)
{
    var vert = d.Vertices;
    int size = (vert.Length - 3) / 8;
    float[,] result = new float[size,3];
    for (int i = 0; i < size; i+=3)
    {
        result[i,0] = toFLoat(vert, i);
        result[i,1] = toFLoat(vert, i + 1);
        result[i,2] = toFLoat(vert, i + 2);
    }

    return result;
}

float toFLoat(ByteString byteString, int i)
{
    byte[] bytes = new byte[8];
    for (int j = 0; j < 8; j++)
    {
        bytes[j] = byteString[i + j];
    }

    return BitConverter.ToSingle(bytes);
}

BulkMetadata node;
var input = File.ReadAllBytes(
    "/Users/aishayakupova/RiderProjects/s1/raw/BulkMetadata!1m2!1s20527061!2u919.raw");
node = BulkMetadata.Parser.ParseFrom(input);
DecoderRockTree decoderRockTree = new DecoderRockTree();

foreach (var meta in node.NodeMetadata)
{
    var path = decoderRockTree.unpackPathAndFlags(meta);
    Console.WriteLine(path.path);
}
