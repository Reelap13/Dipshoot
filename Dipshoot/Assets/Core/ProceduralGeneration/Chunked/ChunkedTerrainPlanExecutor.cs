using UnityEngine;

namespace Game.ProcGen.Chunked
{
    public static class ChunkedTerrainPlanExecutor
    {
        public static void Execute(MapBuildPlan plan, Transform parent, ChunkedTerrainRecipe recipe, string rootName, bool clearPreviousOutput)
        {
            if (clearPreviousOutput)
                DestroyExistingRoot(parent, rootName);

            Transform generatedRoot = new GameObject(rootName).transform;
            generatedRoot.SetParent(parent, false);

            Material sharedMaterial = recipe.TerrainMaterial != null ? recipe.TerrainMaterial : CreateFallbackMaterial();

            for (int i = 0; i < plan.TerrainChunks.Count; i++)
            {
                BuildTerrainChunk(plan.TerrainChunks[i], generatedRoot, sharedMaterial);
            }

            Transform propsRoot = new GameObject("Props").transform;
            propsRoot.SetParent(generatedRoot, false);

            for (int i = 0; i < plan.ObjectPlacements.Count; i++)
            {
                BuildProp(plan.ObjectPlacements[i], propsRoot, recipe);
            }
        }

        private static void DestroyExistingRoot(Transform parent, string rootName)
        {
            Transform existing = parent.Find(rootName);
            if (existing == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        private static void BuildTerrainChunk(MapTerrainChunkPlan chunkPlan, Transform parent, Material material)
        {
            GameObject chunkObject = new GameObject($"Chunk_{chunkPlan.GridCoordinate.x}_{chunkPlan.GridCoordinate.y}");
            chunkObject.transform.SetParent(parent, false);
            chunkObject.transform.localPosition = chunkPlan.Origin;

            MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();

            Mesh mesh = BuildChunkMesh(chunkPlan);
            mesh.name = $"ChunkMesh_{chunkPlan.GridCoordinate.x}_{chunkPlan.GridCoordinate.y}";

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            meshCollider.sharedMesh = mesh;
        }

        private static Mesh BuildChunkMesh(MapTerrainChunkPlan chunkPlan)
        {
            int samples = chunkPlan.SamplesPerAxis;
            float stepX = chunkPlan.ChunkSize.x / (samples - 1);
            float stepZ = chunkPlan.ChunkSize.y / (samples - 1);

            Vector3[] vertices = new Vector3[samples * samples];
            Vector2[] uvs = new Vector2[samples * samples];
            int[] triangles = new int[(samples - 1) * (samples - 1) * 6];

            for (int z = 0; z < samples; z++)
            {
                for (int x = 0; x < samples; x++)
                {
                    int index = z * samples + x;
                    vertices[index] = new Vector3(x * stepX, chunkPlan.Heights[index], z * stepZ);
                    uvs[index] = new Vector2((float)x / (samples - 1), (float)z / (samples - 1));
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < samples - 1; z++)
            {
                for (int x = 0; x < samples - 1; x++)
                {
                    int root = z * samples + x;
                    int nextRow = root + samples;

                    triangles[triangleIndex++] = root;
                    triangles[triangleIndex++] = nextRow;
                    triangles[triangleIndex++] = root + 1;

                    triangles[triangleIndex++] = root + 1;
                    triangles[triangleIndex++] = nextRow;
                    triangles[triangleIndex++] = nextRow + 1;
                }
            }

            Mesh mesh = new Mesh();
            if (vertices.Length > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void BuildProp(MapObjectPlacementPlan placement, Transform propsRoot, ChunkedTerrainRecipe recipe)
        {
            GameObject prefab = placement.PrefabIndex >= 0 && placement.PrefabIndex < recipe.TreeCandidates.Count
                ? recipe.TreeCandidates[placement.PrefabIndex]
                : null;

            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab, propsRoot);
                instance.transform.SetPositionAndRotation(placement.Position, placement.Rotation);
                instance.transform.localScale = Vector3.Scale(instance.transform.localScale, placement.Scale);
                return;
            }

            GameObject fallbackTree = CreateFallbackTree(propsRoot);
            fallbackTree.transform.SetPositionAndRotation(placement.Position, placement.Rotation);
            fallbackTree.transform.localScale = placement.Scale;
        }

        private static Material CreateFallbackMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = new Color(0.29f, 0.44f, 0.24f);
            return material;
        }

        private static GameObject CreateFallbackTree(Transform parent)
        {
            GameObject root = new GameObject("FallbackTree");
            root.transform.SetParent(parent, false);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1f, 0f);
            trunk.transform.localScale = new Vector3(0.25f, 1f, 0.25f);

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            crown.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);

            Renderer trunkRenderer = trunk.GetComponent<Renderer>();
            if (trunkRenderer != null)
                trunkRenderer.sharedMaterial = CreateColoredMaterial(new Color(0.36f, 0.24f, 0.14f));

            Renderer crownRenderer = crown.GetComponent<Renderer>();
            if (crownRenderer != null)
                crownRenderer.sharedMaterial = CreateColoredMaterial(new Color(0.18f, 0.50f, 0.20f));

            return root;
        }

        private static Material CreateColoredMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
