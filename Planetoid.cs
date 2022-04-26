using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoGlobetrotterProtoRocktree;

namespace s1
{
    public class Planetoid
    {
        public enum TextureFormat
        {
            texture_format_rgb = 1,
            texture_format_dxt1 = 2,
        };

        public class Bulk
        {
            public BulkMetadataRequest Request;
            public Bulk Parent = null;
            public float[] Head_node_center = new float[3];

            public BulkMetadata _metadata;
            //  public int busy_ctr;

            public Dictionary<string, Node> Nodes;
            public Dictionary<string, Bulk> Bulks;
        }

        public class Node
        {
            public NodeDataRequest Request;
            public bool CanHaveData;
            public Bulk Parent;
            public float MetersPerTexel;
         // OrientedBoundingBox obb;
            public NodeData _data;
            public double[] matrix_globe_from_mesh;

            public class Mesh
            {
                public List<DecoderRockTree.VertexT> vertices; //uint8
                public List<UInt16> indices; //uint16
                public List<float> uv_offset;
                public List<float> uv_scale;
                // public List<int> texture; //uint8
                // public TextureFormat texture_format;
                // public int texture_width;
                // public int texture_height;
                // public int vertex_buffer;
                // public int index_buffer;
                // public int texture_buffer;
                public bool buffered;
            };

            public List<Mesh> meshes;
        }

        public Bulk RootBulk;
        public PlanetoidMetadata _metadata;
        public float Radius;
        private DecoderRockTree _decoder = new DecoderRockTree();

        public Planetoid(string fileName)
        {
            var input = File.ReadAllBytes(fileName);
            _metadata = PlanetoidMetadata.Parser.ParseFrom(input);
            PopulatePlanetoid();
        }

        BulkMetadataRequest createBulkMetadataRequest(string base_path, string path, uint epoch)
        {
            BulkMetadataRequest req = new BulkMetadataRequest();
            var key = new NodeKey();
            key.Path = base_path + path;
            key.Epoch = epoch;
            req.NodeKey = key;
            return req;
        }

        void GetBulk(BulkMetadataRequest req, Bulk b)
        {
            var path = req.NodeKey.Path;
            var epoch = req.NodeKey.Epoch;
            BulkMetadata bulk;
            bulk = BulkMetadata.Parser.ParseFrom(File.ReadAllBytes("BulkMetadata/pb=!1m2!1s" + path + "!2u" + epoch));
            populateBulk(b, bulk);
        }

        void GetNode(NodeDataRequest req, ref Node n)
        {
            var path = req.NodeKey.Path;
            string fileName;
            if (!req.HasImageryEpoch)
            {
                fileName = "NodeData/pb=!1m2!1s" + path + "!2u" + req.NodeKey + "!2e" + req.TextureFormat + "!4b0";
            }
            else
            {
                fileName = "NodeData/pb=!1m2!1s" + path + "!2u" + req.NodeKey.Epoch + "!2e" + req.TextureFormat +
                           "!3u" + req.ImageryEpoch + "!4b0";
            }

            NodeData node = NodeData.Parser.ParseFrom(File.ReadAllBytes(fileName));
            populateNode(n, node);
        }

        public class Llbounds
        {
            double n, s, w, e;
        };

        void PopulatePlanetoid()
        {
            var bulk = new Bulk();
            bulk.Parent = null;
            bulk.Request = createBulkMetadataRequest("", "", _metadata.RootNodeMetadata.Epoch);
            Radius = _metadata.Radius;
            RootBulk = bulk;
        }

        void populateBulk(Bulk bulk, BulkMetadata bulkMetadata)
        {
            bulk._metadata = bulkMetadata;
            for (int i = 0; i < 3; i++) bulk.Head_node_center[i] = (float) bulk._metadata.HeadNodeCenter[i];
            foreach (var node_meta in bulk._metadata.NodeMetadata)
            {
                var aux = _decoder.UnpackPathAndFlags(node_meta);
                var has_data = node_meta.HasPathAndFlags; //!aux.Flags && NodeMetadata_Flags_NODATA
                var has_bulk = aux.Path.Length == 4 && node_meta.HasPathAndFlags;
                //strlen(aux.path) == 4 && !(aux.flags & NodeMetadata_Flags_LEAF);
                if (has_bulk)
                {
                    var epoch = node_meta.HasBulkMetadataEpoch
                        ? node_meta.BulkMetadataEpoch
                        : bulk._metadata.HeadNodeKey.Epoch;

                    var b = new Bulk();
                    b.Parent = bulk;
                    b.Request = createBulkMetadataRequest(bulk.Request.NodeKey.Path, aux.Path, epoch);
                    b.Bulks[aux.Path] = b;
                }

                if (has_data || node_meta.HasPathAndFlags && node_meta.HasOrientedBoundingBox)
                {
                    var meters_per_texel = node_meta.HasMetersPerTexel
                        ? node_meta.MetersPerTexel
                        : bulk._metadata.MetersPerTexel[aux.Level - 1];
                    //  var n = new Node();
                    // n->parent = bulk;
                    //  n->can_have_data = has_data;
                    // if (has_data)
                    // {
                    //     n->request = createNodeDataRequest(bulk->request.node_key().path(), *(bulk->_metadata),
                    //         node_meta);
                    // }
                    //
                    // n->meters_per_texel = meters_per_texel;
                    // n->obb = unpackObb(node_meta.oriented_bounding_box(), bulk->head_node_center, meters_per_texel);
                    // assert(bulk->nodes.insert(std::make_pair(aux.path, std::move(n))).second);
                }
            }

            bulk._metadata = null;
        }

        void populateNode(Node node, NodeData nodeData)
        {
            node.matrix_globe_from_mesh = new double[16]; 
            for (int i = 0; i < 16; i++) node.matrix_globe_from_mesh[i] = nodeData.MatrixGlobeFromMesh[i];
            DecoderRockTree decoder = new DecoderRockTree();
            foreach (var mesh in nodeData.Meshes)
            {
                Node.Mesh m = new Node.Mesh();
                m.indices = decoder.UnpackIndices(mesh.Indices);
                m.vertices = decoder.UnpackVertices(mesh.Vertices);

                decoder.UnpackTexCoords(mesh.TextureCoordinates, m.vertices, m.vertices.Count, m.uv_offset,
                    m.uv_scale);
                if (mesh.UvOffsetAndScale.Count == 4)
                {
                    m.uv_offset[0] = mesh.UvOffsetAndScale[0];
                    m.uv_offset[1] = mesh.UvOffsetAndScale[1];
                    m.uv_scale[0] = mesh.UvOffsetAndScale[2];
                    m.uv_scale[1] = mesh.UvOffsetAndScale[3];
                }
                else
                {
                    m.uv_offset[1] -= 1 / m.uv_scale[1];
                    m.uv_scale[1] *= -1;
                }

                int [] layer_bounds = decoder.UnpackOctantMaskAndOctantCountsAndLayerBounds(mesh.LayerAndOctantCounts, m.indices,
                    m.vertices);
                //m.indices_len = layer_bounds[3]; // enable
                Resize(m.indices, layer_bounds[3]);

                // var textures = mesh.Texture;
                // var texture = textures[0];
                // var tex = texture.Data[0];
                //
                // // maybe: keep compressed in memory?
                // if (texture.Format == Texture.Types.Format.Jpg)
                // {
                //     var data = tex;
                //     int width, height, comp;
                //     byte pixels = stbi_load_from_memory(&data[0], tex.size(), &width, &height, &comp, 0);
                //     assert(pixels != NULL);
                //     assert(width == texture.width() && height == texture.height() && comp == 3);
                //     m.texture = std::vector<uint8_t>(pixels, pixels + width * height * comp);
                //     stbi_image_free(pixels);
                //     m.texture_format = rocktree_t::texture_format_rgb;
                // }
                // else if (texture.Format ==Texture.Types.Format.CrnDxt1)
                // {
                //     auto src_size = tex.size();
                //     auto src = (uint8_t*) tex.data();
                //     auto dst_size = crn_get_decompressed_size(src, src_size, 0);
                //     assert(dst_size == ((texture.width() + 3) / 4) * ((texture.height() + 3) / 4) * 8);
                //     m.texture = std::vector<uint8_t>(dst_size);
                //     crn_decompress(src, src_size, m.texture.data(), dst_size, 0);
                //     m.texture_format = rocktree_t::texture_format_dxt1;
                // }
                // else
                // {
                //     fprintf(stderr, "unsupported texture format: %d\n", texture.format());
                //     abort();
                // }
                //
                // m.texture_width = texture.width();
                // m.texture_height = texture.height();

                m.buffered = false;
                node.meshes.Add(m);
            }
        }
        public static void Resize<T>(List<T> list, int size, T element = default(T))
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity)   // Optimization
                    list.Capacity = size;

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }
    }
}