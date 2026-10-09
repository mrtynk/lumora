using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Original, deterministic low-poly art for the first playable region.</summary>
public static class IsikliVadiEnvironmentBuilder
{
    private const string MaterialFolder = "Assets/Materials/IsikliVadi";
    private const string MeshFolder = "Assets/Scenes/IsikliVadiData";

    public sealed class Layout
    {
        public Transform root;
        public Vector3 spawnPosition;
        public Vector3 npcPosition;
        public Vector3 seedPosition;
        public Vector3 portalPosition;
        public Vector3[] puzzlePositions;
        public Vector3[] mainPath;
    }

    // Low, gradual slopes work with the existing walking-only CharacterController.
    public static float GroundHeight(float x, float z)
    {
        float north = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 36f, z));
        float rolling = 0.25f * Mathf.Sin(x * 0.085f) * Mathf.Cos(z * 0.07f);
        float boundary = Mathf.Pow(Mathf.Clamp01((Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) - 35f) / 15f), 2f);
        float height = 0.85f + north * 1.35f + rolling + boundary * 2.4f;
        float river = Mathf.Exp(-Mathf.Pow((z + 5f) / 3.4f, 4f));
        return Mathf.Lerp(height, 0.08f, river);
    }

    public static Layout Build()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
        var layout = new Layout
        {
            root = new GameObject("IsikliVadiEnvironment_100x100").transform,
            spawnPosition = At(0f, -36f, 1.15f),
            npcPosition = At(-5f, -28f, 1f),
            seedPosition = At(-15f, 22f, 1.15f),
            portalPosition = At(20f, 37f, 2f),
            puzzlePositions = new[] { At(-25f, -24f, 0.3f), At(25f, 0f, 0.3f), At(-33f, 18f, 0.3f) }
        };
        Vector2[] route =
        {
            new Vector2(0, -36), new Vector2(-6, -26), new Vector2(2, -16),
            new Vector2(4, -13), new Vector2(4, -10), new Vector2(4, 0),
            new Vector2(4, 4), new Vector2(-4, 11), new Vector2(-15, 22),
            new Vector2(-5, 28), new Vector2(8, 29), new Vector2(20, 37)
        };
        layout.mainPath = Array.ConvertAll(route, p => new Vector3(p.x, WalkHeight(p.x, p.y), p.y));

        var ctx = new ArtContext(layout.root);
        Transform meadow = Area("01_BaslangicCayiri", layout.root);
        Transform creek = Area("02_DereVeKopru", layout.root);
        Transform grove = Area("03_IsikKorusu", layout.root);
        Transform portal = Area("04_PortalAcikligi", layout.root);
        Transform[] areas = { meadow, creek, grove, portal };

        BuildTerrain(ctx, layout.root);
        BuildPath(ctx, meadow, route, 3.6f);
        BuildPath(ctx, meadow, new[] { new Vector2(-6, -26), new Vector2(-16, -26), new Vector2(-25, -24) }, 1.8f);
        BuildPath(ctx, creek, new[] { new Vector2(4, 4), new Vector2(15, 3), new Vector2(25, 0) }, 1.8f);
        BuildPath(ctx, grove, new[] { new Vector2(-15, 22), new Vector2(-24, 23), new Vector2(-33, 18) }, 1.8f);
        BuildCreek(ctx, creek);
        BuildGrove(ctx, grove);
        BuildPortalClearing(ctx, portal);
        Populate(ctx, areas, route, layout);
        BuildBounds(ctx, layout.root);
        Sign(ctx, meadow, -1.5f, -31f, "IŞIKLI VADİ\nIşık Korusu →");
        Sign(ctx, creek, 8f, -13f, "Köprüden geç\nIşığı takip et");
        Sign(ctx, grove, -20f, 16f, "IŞIK KORUSU");
        Sign(ctx, portal, 15.5f, 31.5f, "SİSLİ ORMAN\nIşık tohumu portalı açar");
        ctx.Flush();
        AssetDatabase.SaveAssets();
        return layout;
    }

    private static Vector3 At(float x, float z, float offset = 0f)
    {
        return new Vector3(x, GroundHeight(x, z) + offset, z);
    }

    private static Transform Area(string name, Transform parent)
    {
        Transform result = new GameObject(name).transform;
        result.SetParent(parent, false);
        return result;
    }

    private static float WalkHeight(float x, float z)
    {
        if (Mathf.Abs(x - 4f) < 3.2f && z >= -13f && z <= 4f)
        {
            if (z < -10f) return Mathf.Lerp(GroundHeight(x, -13f) + 0.04f, 1.4f, (z + 13f) / 3f);
            if (z > 0f) return Mathf.Lerp(1.4f, GroundHeight(x, 4f) + 0.04f, z / 4f);
            return 1.4f;
        }
        return GroundHeight(x, z) + 0.04f;
    }

    private static void BuildTerrain(ArtContext ctx, Transform parent)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        // Independent triangle vertices preserve a soft faceted low-poly silhouette.
        const int steps = 50;
        for (int z = 0; z < steps; z++)
        for (int x = 0; x < steps; x++)
        {
            float px = -50f + x * 2f;
            float pz = -50f + z * 2f;
            Quad(vertices, triangles, At(px, pz), At(px, pz + 2f), At(px + 2f, pz + 2f), At(px + 2f, pz));
        }
        Mesh mesh = SaveMesh("ValleyTerrain", MeshOf(vertices, triangles));
        GameObject terrain = MeshObject("WalkableTerrain_100x100", parent, mesh, ctx.grass);
        terrain.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static void BuildPath(ArtContext ctx, Transform area, Vector2[] points, float width)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (int segment = 1; segment < points.Length; segment++)
        {
            Vector2 from = points[segment - 1];
            Vector2 to = points[segment];
            Vector2 dir = (to - from).normalized;
            Vector2 side = new Vector2(-dir.y, dir.x) * width * 0.5f;
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / 0.75f);
            for (int i = 0; i < steps; i++)
            {
                Vector2 a = Vector2.Lerp(from, to, (float)i / steps);
                Vector2 b = Vector2.Lerp(from, to, (float)(i + 1) / steps);
                Quad(vertices, triangles, PathPoint(a - side), PathPoint(b - side), PathPoint(b + side), PathPoint(a + side));
            }
        }
        ctx.Add(area, ctx.path, MeshOf(vertices, triangles), Matrix4x4.identity, true);
    }

    private static Vector3 PathPoint(Vector2 p)
    {
        return new Vector3(p.x, GroundHeight(p.x, p.y) + 0.045f, p.y);
    }

    private static void BuildCreek(ArtContext ctx, Transform area)
    {
        // Opaque unlit water: one surface, no realtime reflections or expensive transparency.
        ctx.Cube(area, ctx.water, new Vector3(0, 0.48f, -5), new Vector3(98, 0.05f, 5.4f));
        for (int i = 0; i < 19; i++)
        {
            float x = -44f + i * 5f;
            ctx.Cube(area, ctx.foam, new Vector3(x, 0.516f, -5.4f + Mathf.Sin(i * 2f) * 1.3f), new Vector3(1.6f, 0.012f, 0.07f), Quaternion.Euler(0, -12, 0));
        }

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float[] zRows = { -13f, -10f, 0f, 4f };
        for (int i = 1; i < zRows.Length; i++)
        {
            float a = zRows[i - 1];
            float b = zRows[i];
            Quad(vertices, triangles,
                new Vector3(1.3f, WalkHeight(4, a), a), new Vector3(1.3f, WalkHeight(4, b), b),
                new Vector3(6.7f, WalkHeight(4, b), b), new Vector3(6.7f, WalkHeight(4, a), a));
        }
        Mesh deck = SaveMesh("BridgeDeck", MeshOf(vertices, triangles));
        GameObject bridge = MeshObject("WideBridgeAndGentleRamps", area, deck, ctx.wood);
        bridge.AddComponent<MeshCollider>().sharedMesh = deck;
        for (int i = 0; i < 13; i++)
        {
            float z = -9.7f + i * 0.78f;
            ctx.Cube(area, ctx.woodLight, new Vector3(4, 1.41f, z), new Vector3(5.35f, 0.025f, 0.62f));
        }
        foreach (float x in new[] { 1.35f, 6.65f })
        {
            Box("BridgeRail", area, new Vector3(x, 1.9f, -5), new Vector3(0.22f, 1f, 10.2f));
            ctx.Cube(area, ctx.woodLight, new Vector3(x, 2.38f, -5), new Vector3(0.2f, 0.16f, 10.5f));
            for (int i = 0; i < 5; i++)
                ctx.Cube(area, ctx.wood, new Vector3(x, 1.82f, -10f + i * 2.5f), new Vector3(0.24f, 1.25f, 0.24f));
        }
        // Decorative stepping stones are an optional route; no precision jump is required.
        for (int i = 0; i < 5; i++)
        {
            float z = -8.5f + i * 1.8f;
            ctx.Rock(area, -25f + Mathf.Sin(i) * 0.4f, z, new Vector3(1.8f, 0.35f, 1.3f), false);
        }
    }

    private static void BuildGrove(ArtContext ctx, Transform area)
    {
        // The Light Tree stands behind the collectible, leaving the approach entirely open.
        float x = -16f;
        float z = 28f;
        Vector3 trunkBase = At(x, z);
        ctx.Add(area, ctx.woodLight, ctx.trunkMesh, Matrix4x4.TRS(trunkBase, Quaternion.identity, new Vector3(1.65f, 7.8f, 1.65f)));
        Capsule("LightTreeTrunkCollider", area, trunkBase + Vector3.up * 3.8f, 1.3f, 7.6f);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * Mathf.PI * 2f / 7f;
            Vector3 crown = trunkBase + new Vector3(Mathf.Cos(angle) * 3f, 7.4f + (i % 3) * 0.8f, Mathf.Sin(angle) * 2.7f);
            ctx.Blob(area, i % 2 == 0 ? ctx.mint : ctx.lightLeaves, crown, new Vector3(4.8f, 3.4f, 4.2f));
            Vector3 end = crown - Vector3.up;
            ctx.Branch(area, ctx.woodLight, trunkBase + Vector3.up * 4.5f, end, 0.42f);
        }
        ctx.Blob(area, ctx.lightLeaves, trunkBase + Vector3.up * 10.1f, new Vector3(4f, 3.3f, 3.7f));
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            ctx.Branch(area, ctx.woodLight, trunkBase + Vector3.up * 0.3f,
                At(x + Mathf.Cos(angle) * 4f, z + Mathf.Sin(angle) * 4f, 0.08f), 0.35f);
        }
        // Glowing geometry supplies the magical accent without additional lights.
        for (int i = 0; i < 14; i++)
        {
            float a = i * 2.39996f;
            float r = 6f + (i % 3) * 1.9f;
            float cx = -15f + Mathf.Cos(a) * r;
            float cz = 22f + Mathf.Sin(a) * r;
            if (cz < 18f && cx > -18f && cx < -10f) continue;
            ctx.Crystal(area, cx, cz, 0.7f + (i % 4) * 0.3f);
        }
        // A flush petal-shaped stone dais frames the seed without creating a step.
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            ctx.Blob(area, ctx.paleStone, At(-15f + Mathf.Cos(a) * 1.7f, 22f + Mathf.Sin(a) * 1.7f, 0.07f), new Vector3(0.85f, 0.1f, 0.85f));
        }
    }

    private static void BuildPortalClearing(ArtContext ctx, Transform area)
    {
        // A ring of broad paving stones and crystals guides attention to the runtime portal.
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f;
            float x = 20f + Mathf.Cos(a) * 5f;
            float z = 37f + Mathf.Sin(a) * 4.8f;
            ctx.Blob(area, ctx.paleStone, At(x, z, 0.06f), new Vector3(1.4f, 0.1f, 1f));
        }
        ctx.Crystal(area, 15f, 39.5f, 2.1f);
        ctx.Crystal(area, 25f, 39.5f, 2.6f);
        ctx.Rock(area, 13f, 43f, new Vector3(3.8f, 2.6f, 2.6f), true);
        ctx.Rock(area, 28f, 43f, new Vector3(4.8f, 3.4f, 3.8f), true);
    }

    private static void Populate(ArtContext ctx, Transform[] areas, Vector2[] route, Layout layout)
    {
        var random = new System.Random(1107);
        // Keep the whole main route and every gameplay anchor free of tree/rock collision.
        var clearings = new List<Vector2>
        {
            new Vector2(0, -36), new Vector2(-5, -28), new Vector2(-15, 22),
            new Vector2(20, 37), new Vector2(-25, -24), new Vector2(25, 0), new Vector2(-33, 18)
        };
        for (int i = 0; i < 94; i++)
        {
            float x = Range(random, -45, 45);
            float z = Range(random, -44, 45);
            if (z > -11 && z < 1) continue;
            if (DistanceToPath(new Vector2(x, z), route) < 5.4f) continue;
            bool clear = false;
            foreach (Vector2 p in clearings) if (Vector2.Distance(p, new Vector2(x, z)) < 5f) clear = true;
            if (clear) continue;
            Transform area = ChooseArea(areas, x, z);
            float scale = Range(random, 0.85f, 1.45f);
            ctx.Tree(area, x, z, scale, z > 8 && x < 0);
        }
        for (int i = 0; i < 46; i++)
        {
            float x = Range(random, -45, 45);
            float z = Range(random, -43, 43);
            if (DistanceToPath(new Vector2(x, z), route) < 4.8f || z > -12 && z < 3) continue;
            bool clear = false;
            foreach (Vector2 p in clearings) if (Vector2.Distance(p, new Vector2(x, z)) < 4.5f) clear = true;
            if (clear) continue;
            float size = Range(random, 0.65f, 1.8f);
            ctx.Rock(ChooseArea(areas, x, z), x, z, new Vector3(size, size * 0.7f, size * 0.8f), true);
        }
        // Color dots and mushrooms along the walk form readable, low-cost guidance.
        for (int s = 1; s < route.Length; s++)
        {
            Vector2 dir = (route[s] - route[s - 1]).normalized;
            Vector2 side = new Vector2(-dir.y, dir.x);
            int count = Mathf.CeilToInt(Vector2.Distance(route[s], route[s - 1]) / 2.6f);
            for (int i = 0; i < count; i++)
            {
                Vector2 p = Vector2.Lerp(route[s - 1], route[s], (i + 0.5f) / count) + side * (i % 2 == 0 ? 2.9f : -2.9f);
                if (p.y > -12 && p.y < 3) continue;
                Transform area = ChooseArea(areas, p.x, p.y);
                ctx.Flower(area, p.x, p.y, i % 3 == 0);
                ctx.Flower(area, p.x + 0.4f, p.y - 0.3f, i % 3 == 0);
            }
        }
        for (int i = 0; i < layout.puzzlePositions.Length; i++)
        {
            Vector3 p = layout.puzzlePositions[i];
            Transform area = ChooseArea(areas, p.x, p.z);
            ctx.Tree(area, p.x - 4.5f, p.z + 3.6f, 1.1f, i == 2);
            ctx.Flower(area, p.x + 2f, p.z + 1.5f, true);
            ctx.Flower(area, p.x + 2.6f, p.z + 2f, true);
        }
    }

    private static Transform ChooseArea(Transform[] areas, float x, float z)
    {
        if (z < -13f) return areas[0];
        if (z < 6f) return areas[1];
        return x < 4f ? areas[2] : areas[3];
    }

    private static float DistanceToPath(Vector2 point, Vector2[] route)
    {
        float nearest = float.MaxValue;
        for (int i = 1; i < route.Length; i++)
        {
            Vector2 a = route[i - 1];
            Vector2 delta = route[i] - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
            nearest = Mathf.Min(nearest, Vector2.Distance(point, a + delta * t));
        }
        return nearest;
    }

    private static void BuildBounds(ArtContext ctx, Transform parent)
    {
        Transform bounds = Area("NaturalValleyBoundary", parent);
        Box("NorthBoundary", bounds, new Vector3(0, 8, 49.2f), new Vector3(100, 16, 1));
        Box("SouthBoundary", bounds, new Vector3(0, 8, -49.2f), new Vector3(100, 16, 1));
        Box("EastBoundary", bounds, new Vector3(49.2f, 8, 0), new Vector3(1, 16, 100));
        Box("WestBoundary", bounds, new Vector3(-49.2f, 8, 0), new Vector3(1, 16, 100));
        for (int i = 0; i < 11; i++)
        {
            float v = -48f + i * 9.6f;
            float height = 4f + (i % 3) * 1.7f;
            ctx.Blob(bounds, ctx.hill, At(v, 50f, 0f), new Vector3(9f, height, 7f));
            ctx.Blob(bounds, ctx.hill, At(v, -50f, -1f), new Vector3(9f, 3.5f, 7f));
            ctx.Blob(bounds, ctx.hill, At(50f, v, 0f), new Vector3(7f, height, 9f));
            ctx.Blob(bounds, ctx.hill, At(-50f, v, 0f), new Vector3(7f, height, 9f));
        }
    }

    private static void Sign(ArtContext ctx, Transform parent, float x, float z, string message)
    {
        Vector3 pos = At(x, z);
        ctx.Cube(parent, ctx.wood, pos + Vector3.up * 0.9f, new Vector3(0.18f, 1.8f, 0.18f));
        ctx.Cube(parent, ctx.woodLight, pos + Vector3.up * 1.65f, new Vector3(2.8f, 0.85f, 0.16f));
        GameObject textObject = new GameObject("WayfindingSign");
        textObject.transform.SetParent(parent, false);
        textObject.transform.position = pos + new Vector3(0, 1.65f, -0.095f);
        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.characterSize = 0.075f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.13f, 0.24f, 0.23f);
        textObject.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        textObject.isStatic = true;
    }

    private static void Box(string name, Transform parent, Vector3 center, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.AddComponent<BoxCollider>().size = size;
        go.isStatic = true;
    }

    private static void Capsule(string name, Transform parent, Vector3 center, float radius, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        CapsuleCollider collider = go.AddComponent<CapsuleCollider>();
        collider.radius = radius;
        collider.height = height;
        go.isStatic = true;
    }

    private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.isStatic = true;
        return go;
    }

    private static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int first = vertices.Count;
        vertices.AddRange(new[] { a, b, c, a, c, d });
        for (int i = 0; i < 6; i++) triangles.Add(first + i);
    }

    private static Mesh MeshOf(List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh { indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh SaveMesh(string name, Mesh mesh)
    {
        string path = MeshFolder + "/" + name + ".asset";
        Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved != null)
        {
            mesh.name = name;
            EditorUtility.CopySerialized(mesh, saved);
            UnityEngine.Object.DestroyImmediate(mesh);
            return saved;
        }
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        string parent = path.Substring(0, split);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
    }

    private static float Range(System.Random random, float low, float high)
    {
        return low + (float)random.NextDouble() * (high - low);
    }

    private sealed class ArtContext
    {
        public readonly Material grass, path, wood, woodLight, leaves, mint, lightLeaves;
        public readonly Material stone, paleStone, water, foam, gold, pink, crystal, hill;
        public readonly Mesh trunkMesh;
        private readonly Mesh blobMesh;
        private readonly Mesh cubeMesh;
        private readonly Dictionary<string, Batch> batches = new Dictionary<string, Batch>();
        private readonly List<Mesh> temporaryMeshes = new List<Mesh>();

        public ArtContext(Transform root)
        {
            grass = Material("MeadowGrass", new Color(0.52f, 0.78f, 0.39f));
            path = Material("WarmSandPath", new Color(0.91f, 0.80f, 0.54f));
            wood = Material("WarmWood", new Color(0.44f, 0.28f, 0.17f));
            woodLight = Material("HoneyWood", new Color(0.76f, 0.59f, 0.34f));
            leaves = Material("FreshLeaves", new Color(0.31f, 0.64f, 0.35f));
            mint = Material("MintLeaves", new Color(0.38f, 0.79f, 0.60f));
            lightLeaves = Material("LightTreeLeaves", new Color(0.81f, 0.92f, 0.44f), 0.12f);
            stone = Material("BlueGreyStone", new Color(0.51f, 0.65f, 0.66f));
            paleStone = Material("PaleStone", new Color(0.78f, 0.86f, 0.76f));
            water = Material("TurquoiseCreek", new Color(0.25f, 0.73f, 0.84f), 0f, true);
            foam = Material("CreekRipples", new Color(0.67f, 0.93f, 0.95f), 0f, true);
            gold = Material("SunPetals", new Color(1f, 0.79f, 0.27f));
            pink = Material("CoralMushrooms", new Color(0.92f, 0.47f, 0.51f));
            crystal = Material("LightCrystals", new Color(0.44f, 0.88f, 0.92f), 0.35f);
            hill = Material("ValleyHills", new Color(0.39f, 0.66f, 0.42f));
            blobMesh = CreateBlob();
            trunkMesh = CreateTrunk();
            temporaryMeshes.Add(blobMesh);
            temporaryMeshes.Add(trunkMesh);
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(primitive);
        }

        public void Add(Transform parent, Material material, Mesh mesh, Matrix4x4 transform, bool temporary = false)
        {
            string key = parent.name + "_" + material.name;
            if (!batches.TryGetValue(key, out Batch batch))
            {
                batch = new Batch { parent = parent, material = material };
                batches.Add(key, batch);
            }
            batch.instances.Add(new CombineInstance { mesh = mesh, transform = transform });
            if (temporary) temporaryMeshes.Add(mesh);
        }

        public void Cube(Transform parent, Material material, Vector3 position, Vector3 scale, Quaternion? rotation = null)
        {
            Add(parent, material, cubeMesh, Matrix4x4.TRS(position, rotation ?? Quaternion.identity, scale));
        }

        public void Blob(Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            Add(parent, material, blobMesh, Matrix4x4.TRS(position, Quaternion.identity, scale));
        }

        public void Tree(Transform parent, float x, float z, float scale, bool magical)
        {
            Vector3 p = At(x, z);
            Add(parent, wood, trunkMesh, Matrix4x4.TRS(p, Quaternion.identity, new Vector3(0.42f * scale, 3.3f * scale, 0.42f * scale)));
            Blob(parent, magical ? mint : leaves, p + new Vector3(0, 3.7f * scale, 0), new Vector3(2.3f, 2.1f, 2.2f) * scale);
            Blob(parent, magical ? lightLeaves : mint, p + new Vector3(0.8f, 4.6f, 0.1f) * scale, new Vector3(1.55f, 1.7f, 1.65f) * scale);
            Capsule("TreeTrunkCollider", parent, p + Vector3.up * 1.5f * scale, 0.42f * scale, 3f * scale);
        }

        public void Rock(Transform parent, float x, float z, Vector3 scale, bool collision)
        {
            Vector3 center = At(x, z, scale.y * 0.35f);
            Blob(parent, stone, center, scale);
            if (collision) Box("RockCollider", parent, center, new Vector3(scale.x * 1.3f, scale.y * 1.2f, scale.z * 1.3f));
        }

        public void Flower(Transform parent, float x, float z, bool mushroom)
        {
            Vector3 p = At(x, z);
            Add(parent, woodLight, trunkMesh, Matrix4x4.TRS(p, Quaternion.identity, new Vector3(0.06f, 0.36f, 0.06f)));
            Blob(parent, mushroom ? pink : gold, p + Vector3.up * 0.38f, mushroom ? new Vector3(0.34f, 0.16f, 0.34f) : new Vector3(0.2f, 0.1f, 0.2f));
        }

        public void Crystal(Transform parent, float x, float z, float size)
        {
            Blob(parent, crystal, At(x, z, size * 0.8f), new Vector3(size * 0.35f, size, size * 0.38f));
            Blob(parent, gold, At(x + size * 0.45f, z + 0.15f, size * 0.4f), new Vector3(size * 0.22f, size * 0.55f, size * 0.25f));
        }

        public void Branch(Transform parent, Material material, Vector3 from, Vector3 to, float radius)
        {
            Vector3 delta = to - from;
            Add(parent, material, trunkMesh, Matrix4x4.TRS(from, Quaternion.FromToRotation(Vector3.up, delta), new Vector3(radius, delta.magnitude, radius)));
        }

        public void Flush()
        {
            foreach (KeyValuePair<string, Batch> entry in batches)
            {
                Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(entry.Value.instances.ToArray(), true, true);
                mesh.RecalculateBounds();
                Mesh saved = SaveMesh(entry.Key, mesh);
                MeshObject("StaticArt_" + entry.Value.material.name, entry.Value.parent, saved, entry.Value.material);
            }
            foreach (Mesh mesh in temporaryMeshes) UnityEngine.Object.DestroyImmediate(mesh);
        }

        private static Material Material(string name, Color color, float emission = 0f, bool unlit = false)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material saved = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (saved != null) return saved;
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")
                : Shader.Find(unlit ? "Unlit/Color" : "Standard");
            if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            if (shader == null) throw new InvalidOperationException("Işıklı Vadi için uygun shader bulunamadı.");
            var result = new Material(shader) { name = name, color = color };
            if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", color);
            if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", 0.15f);
            if (result.HasProperty("_Glossiness")) result.SetFloat("_Glossiness", 0.15f);
            if (emission > 0 && result.HasProperty("_EmissionColor"))
            {
                result.EnableKeyword("_EMISSION");
                result.SetColor("_EmissionColor", color * emission);
            }
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static Mesh CreateBlob()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 7;
            const int rows = 4;
            for (int row = 0; row < rows; row++)
            for (int side = 0; side < sides; side++)
            {
                Vector3 a = SpherePoint(row, side, rows, sides);
                Vector3 b = SpherePoint(row + 1, side, rows, sides);
                Vector3 c = SpherePoint(row + 1, side + 1, rows, sides);
                Vector3 d = SpherePoint(row, side + 1, rows, sides);
                Quad(vertices, triangles, a, d, c, b);
            }
            return MeshOf(vertices, triangles);
        }

        private static Vector3 SpherePoint(int row, int side, int rows, int sides)
        {
            float elevation = row * Mathf.PI / rows;
            float azimuth = side * 2f * Mathf.PI / sides;
            return new Vector3(Mathf.Sin(elevation) * Mathf.Cos(azimuth), Mathf.Cos(elevation), Mathf.Sin(elevation) * Mathf.Sin(azimuth));
        }

        private static Mesh CreateTrunk()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                float b = (i + 1) * Mathf.PI * 2f / 7f;
                Vector3 bottomA = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 bottomB = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                Vector3 topA = new Vector3(bottomA.x * 0.7f, 1, bottomA.z * 0.7f);
                Vector3 topB = new Vector3(bottomB.x * 0.7f, 1, bottomB.z * 0.7f);
                Quad(vertices, triangles, bottomA, topA, topB, bottomB);
                Quad(vertices, triangles, Vector3.up, topB, topA, Vector3.up);
            }
            return MeshOf(vertices, triangles);
        }

        private sealed class Batch
        {
            public Transform parent;
            public Material material;
            public readonly List<CombineInstance> instances = new List<CombineInstance>();
        }
    }
}
