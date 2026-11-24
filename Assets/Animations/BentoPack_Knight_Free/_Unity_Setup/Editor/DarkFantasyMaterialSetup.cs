// Dark Fantasy character packs: one-click material setup for Unity (Built-in and URP).
// Copy this file ONCE into any folder named Editor in your project, then run Tools > Dark Fantasy > Set Up Materials.
// The FBX files cannot find the textures on their own, so without this step the characters import white.
// For every SK_*.fbx and SM_*.fbx with a Textures folder above it: sets the texture import settings, creates one
// material per FBX material in a Materials folder beside FBX, and remaps the FBX to them. Safe to run again.
// Texture folders: Textures/Common (BaseColor, Emission) + Textures/Unity (Normal_OpenGL, MetallicSmoothness,
// Occlusion); older packs keep everything in Textures/Unity. A Weapons folder does not repeat the character's own
// textures: they are found by name in the hero's Character folder, which must be imported into the project too.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class DarkFantasyMaterialSetup
{
    const float EmissionStrength = 2.5f;
    const string SharedNote = "SHARED_FROM_CHARACTER.txt";
    static Dictionary<string, string> byName;

    [MenuItem("Tools/Dark Fantasy/Set Up Materials")]
    public static void Run()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline == null ? "" : GraphicsSettings.currentRenderPipeline.GetType().Name;
        if (pipeline.Contains("HD"))
        {
            Debug.LogError("[Dark Fantasy] HDRP is not supported by this tool: its Lit shader needs a mask map the pack does not include. Nothing was changed.");
            return;
        }
        bool urp = pipeline.Contains("Universal");
        var shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null)
        {
            Debug.LogError("[Dark Fantasy] Shader not found: " + (urp ? "Universal Render Pipeline/Lit" : "Standard") + ". Nothing was changed.");
            return;
        }
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
            Debug.LogWarning("[Dark Fantasy] Project Settings > Player > Color Space is Gamma. Set it to Linear, or the steel renders as shiny plastic.");
        // every T_*.png in the project by file name: a Weapons folder finds its character's textures in the Character folder
        byName = new Dictionary<string, string>();
        foreach (var p in AssetDatabase.FindAssets("T_ t:Texture2D", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(x => x))
        {
            var f = Path.GetFileName(p);
            if (f.StartsWith("T_") && f.EndsWith(".png") && !byName.ContainsKey(f)) byName[f] = p;
        }
        int fbx = 0, made = 0, flat = 0, missing = 0;
        var doneTextures = new HashSet<string>();
        foreach (var path in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
        {
            var file = Path.GetFileName(path);
            if (!file.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) || !(file.StartsWith("SK_") || file.StartsWith("SM_"))) continue;
            var root = PackRoot(path);
            if (root == null) continue;
            foreach (var sub in new[] { "/Textures/Common", "/Textures/Unity" })
                if (AssetDatabase.IsValidFolder(root + sub) && doneTextures.Add(root + sub)) Textures(root + sub);
            var shared = SharedParts(root);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            bool changed = false;
            foreach (var src in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m => m.name).Concat(ExternalNames(importer)).Distinct().ToList())
            {
                var matPath = root + "/Materials/" + src + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                var part = src.StartsWith("M_") ? src.Substring(2) : src;
                bool hasBase = Find(root, part, "BaseColor") != null;
                if (!hasBase && shared.Contains("T_" + part))
                {
                    Debug.LogWarning($"[Dark Fantasy] {src}: the textures of T_{part} are in the hero's Character folder, not in {root}. Import that Character folder into this project too, then run Set Up Materials again.");
                    missing++;
                }
                // rebuild a material that was made flat (its Character folder was missing) once its textures are there
                if (mat == null || mat.shader != shader || (hasBase && mat.GetTexture(urp ? "_BaseMap" : "_MainTex") == null))
                {
                    bool textured;
                    var fresh = Build(shader, urp, src, root, out textured);
                    Directory.CreateDirectory(root + "/Materials");
                    AssetDatabase.DeleteAsset(matPath);
                    AssetDatabase.CreateAsset(fresh, matPath);
                    mat = fresh;
                    made++;
                    if (!textured) flat++;
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src), mat);
                changed = true;
            }
            if (changed) { importer.SaveAndReimport(); fbx++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Dark Fantasy] {(urp ? "URP" : "Built-in")}: {fbx} FBX remapped, {made} materials created ({flat} of them flat colour"
                  + (missing > 0 ? $"; {missing} wait for their Character folder" : " by design") + ").");
    }

    static IEnumerable<string> ExternalNames(ModelImporter importer) => importer.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)).Select(k => k.name);

    static string PackRoot(string assetPath)
    {
        var d = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        while (!string.IsNullOrEmpty(d) && d != "Assets")
        {
            if (AssetDatabase.IsValidFolder(d + "/Textures/Unity") || AssetDatabase.IsValidFolder(d + "/Textures/Common")) return d;
            d = Path.GetDirectoryName(d)?.Replace('\\', '/');
        }
        return null;
    }

    static HashSet<string> SharedParts(string root)
    {
        var note = root + "/Textures/" + SharedNote;
        var set = new HashSet<string>();
        if (File.Exists(note))
            foreach (var line in File.ReadAllLines(note))
                if (line.Trim().StartsWith("T_")) set.Add(line.Trim());
        return set;
    }

    static bool Settings(string p)
    {
        var ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) return false;
        bool normal = p.EndsWith("_Normal_OpenGL.png"), color = p.EndsWith("_BaseColor.png") || p.EndsWith("_Emission.png");
        var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ti.textureType == type && ti.sRGBTexture == color) return true;
        ti.textureType = type;
        ti.sRGBTexture = color;
        ti.SaveAndReimport();
        return true;
    }

    static void Textures(string tdir)
    {
        foreach (var p in Directory.GetFiles(tdir, "*.png").Select(x => x.Replace('\\', '/')))
            Settings(p);
    }

    // own folders first (Textures/Unity, then Textures/Common), then the same file name anywhere in the project
    static Texture2D Find(string root, string part, string kind)
    {
        var f = $"T_{part}_{kind}.png";
        foreach (var sub in new[] { "/Textures/Unity/", "/Textures/Common/" })
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(root + sub + f);
            if (t != null) return t;
        }
        string p;
        if (byName != null && byName.TryGetValue(f, out p) && Settings(p)) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        return null;
    }

    static Material Build(Shader shader, bool urp, string name, string root, out bool textured)
    {
        var part = name.StartsWith("M_") ? name.Substring(2) : name;
        Texture2D T(string kind) => Find(root, part, kind);
        var m = new Material(shader) { name = name };
        textured = T("BaseColor") != null;
        if (!textured)
        {
            var dark = new Color(0.035f, 0.032f, 0.03f);
            m.SetColor(urp ? "_BaseColor" : "_Color", dark);
            m.SetFloat(urp ? "_Smoothness" : "_Glossiness", 0.45f);
            return m;
        }
        m.SetTexture(urp ? "_BaseMap" : "_MainTex", T("BaseColor"));
        m.SetTexture("_BumpMap", T("Normal_OpenGL"));
        m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap", T("MetallicSmoothness"));
        m.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
        m.SetTexture("_OcclusionMap", T("Occlusion"));
        if (T("Emission") != null)
        {
            // above 1 on purpose: this is what a Bloom post-process picks up. Lower _EmissionColor on the material to taste.
            m.SetTexture("_EmissionMap", T("Emission"));
            m.SetColor("_EmissionColor", Color.white * EmissionStrength);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (urp)
        {
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_WorkflowMode", 1f);
            m.SetFloat("_Smoothness", 1f);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.EnableKeyword("_OCCLUSIONMAP");
        }
        else
        {
            m.SetFloat("_GlossMapScale", 1f);
        }
        return m;
    }
}
