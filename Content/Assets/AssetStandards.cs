using Godot;
using System.Collections.Generic;

namespace TheSignal.Content.Assets;

/// <summary>
/// B2: Asset Standards — GLTF import pipeline specs, material library,
/// VFX templates, audio bus configuration.
/// </summary>
[GlobalClass]
public partial class AssetStandards : Resource
{
    [ExportGroup("GLTF Import Pipeline")]
    [Export] public float ScaleFactor { get; set; } = 1.0f;
    [Export] public bool GenerateCollision { get; set; } = true;
    [Export] public int MaxLodLevels { get; set; } = 3;
    [Export] public float[] LodDistanceRatios { get; set; } = { 1.0f, 0.5f, 0.25f };
    [Export] public int MaxTriangleCount { get; set; } = 50000;
    [Export] public bool ImportAnimations { get; set; } = true;
    [Export] public bool ImportSkins { get; set; } = true;

    [ExportGroup("Material Library")]
    [Export] public float TextureScale { get; set; } = 1.0f;
    [Export] public float MetallicDefault { get; set; } = 0.0f;
    [Export] public float RoughnessDefault { get; set; } = 0.8f;
    [Export] public int AtlasSize { get; set; } = 2048;
    [Export] public bool UseSrgb { get; set; } = true;
    [Export] public bool GenerateMipmaps { get; set; } = true;

    [ExportGroup("VFX Templates")]
    [Export] public int ParticleCapacity { get; set; } = 256;
    [Export] public float ParticleLifeMin { get; set; } = 0.5f;
    [Export] public float ParticleLifeMax { get; set; } = 2.0f;
    [Export] public float EmissionRateDefault { get; set; } = 20f;
    [Export] public string VfxLibraryPath { get; set; } = "res://Assets/VFX/Library/";

    [ExportGroup("Audio Bus")]
    [Export] public float MasterVolume { get; set; } = 0f;
    [Export] public float MusicVolume { get; set; } = -6f;
    [Export] public float SfxVolume { get; set; } = 0f;
    [Export] public float AmbientVolume { get; set; } = -10f;
    [Export] public float DialogueVolume { get; set; } = 0f;

    // ========== MATERIAL DEFINITIONS ==========

    public enum MaterialCategory
    {
        Metal,
        Organic,
        Tech,
        Rust,
        Signal,
        Energy,
        Glass,
        Concrete,
        Flesh,
        Chrome
    }

    public static Dictionary<MaterialCategory, MaterialDefinition> GetMaterialDefaults()
    {
        return new Dictionary<MaterialCategory, MaterialDefinition>
        {
            [MaterialCategory.Metal] = new() { BaseColor = "#888888", Metallic = 0.9f, Roughness = 0.3f },
            [MaterialCategory.Organic] = new() { BaseColor = "#448844", Metallic = 0.0f, Roughness = 0.9f },
            [MaterialCategory.Tech] = new() { BaseColor = "#224466", Metallic = 0.7f, Roughness = 0.4f },
            [MaterialCategory.Rust] = new() { BaseColor = "#885522", Metallic = 0.5f, Roughness = 0.8f },
            [MaterialCategory.Signal] = new() { BaseColor = "#4488ff", Metallic = 0.3f, Roughness = 0.2f, Emission = "#4488ff" },
            [MaterialCategory.Energy] = new() { BaseColor = "#ff8844", Metallic = 0.2f, Roughness = 0.1f, Emission = "#ff8844" },
            [MaterialCategory.Glass] = new() { BaseColor = "#aaccee", Metallic = 0.0f, Roughness = 0.1f, Transparency = 0.5f },
            [MaterialCategory.Concrete] = new() { BaseColor = "#666666", Metallic = 0.0f, Roughness = 0.9f },
            [MaterialCategory.Flesh] = new() { BaseColor = "#cc6644", Metallic = 0.0f, Roughness = 0.7f },
            [MaterialCategory.Chrome] = new() { BaseColor = "#cccccc", Metallic = 1.0f, Roughness = 0.1f }
        };
    }

    // ========== AUDIO BUS SETUP ==========

    public static void ConfigureAudioBusses()
    {
        var master = AudioServer.GetBusIndex("Master");
        var music = AudioServer.GetBusIndex("Music");
        var sfx = AudioServer.GetBusIndex("SFX");
        var ambient = AudioServer.GetBusIndex("Ambient");
        var dialogue = AudioServer.GetBusIndex("Dialogue");

        // Set up bus routing
        // Master ← Music, SFX, Ambient, Dialogue
        // Master → Output

        if (master >= 0) AudioServer.SetBusVolumeDb(master, 0);
        if (music >= 0) AudioServer.SetBusVolumeDb(music, -6);
        if (sfx >= 0) AudioServer.SetBusVolumeDb(sfx, 0);
        if (ambient >= 0) AudioServer.SetBusVolumeDb(ambient, -10);
        if (dialogue >= 0) AudioServer.SetBusVolumeDb(dialogue, 0);

        // Add effects
        // Master: Limiter
        // SFX: Reverb
        // Dialogue: Compressor
    }

    // ========== LOD GENERATION ==========

    public static float GetLodDistance(int lodLevel, float baseDistance, float[] ratios)
    {
        if (lodLevel <= 0) return baseDistance;
        if (lodLevel >= ratios.Length) return baseDistance * ratios[^1];
        return baseDistance * ratios[lodLevel];
    }

    // ========== TEXTURE COMPRESSION ==========

    public static string GetCompressionFormat(bool hasAlpha, bool srgb)
    {
        if (hasAlpha)
            return srgb ? "S3TC DXT5" : "BPTC";
        return srgb ? "S3TC DXT1" : "BPTC";
    }
}

public class MaterialDefinition
{
    public string BaseColor { get; set; } = "#ffffff";
    public float Metallic { get; set; } = 0f;
    public float Roughness { get; set; } = 0.8f;
    public string Emission { get; set; } = "";
    public float Transparency { get; set; } = 0f;
    public string NormalMap { get; set; } = "";
    public string RoughnessMap { get; set; } = "";
    public string MetallicMap { get; set; } = "";
}

[GlobalClass]
public partial class VfxTemplate : Resource
{
    [Export] public string VfxId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public VfxCategory Category { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public float Lifetime { get; set; } = 2f;
    [Export] public bool Looping { get; set; } = false;
    [Export] public int MaxInstances { get; set; } = 10;

    [ExportGroup("Particle Settings")]
    [Export] public int ParticleCount { get; set; } = 64;
    [Export] public Vector3 EmitBox { get; set; } = Vector3.One;
    [Export] public float SpeedMin { get; set; } = 1f;
    [Export] public float SpeedMax { get; set; } = 5f;
    [Export] public Color ColorStart { get; set; } = Colors.White;
    [Export] public Color ColorEnd { get; set; } = Colors.Transparent;
    [Export] public float ScaleMin { get; set; } = 0.1f;
    [Export] public float ScaleMax { get; set; } = 0.5f;
    [Export] public float Gravity { get; set; } = -5f;
}

public enum VfxCategory
{
    Impact,
    Explosion,
    Beam,
    Buff,
    Debuff,
    Heal,
    Shield,
    Smoke,
    Fire,
    Resonance
}