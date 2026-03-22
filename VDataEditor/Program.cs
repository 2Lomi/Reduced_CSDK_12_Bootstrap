using System.Diagnostics;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Serialization.KeyValues;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using KVObject = ValveResourceFormat.Serialization.KeyValues.KVObject;
using KVValue = ValveResourceFormat.Serialization.KeyValues.KVValue;


if (args.Length != 2 || args[0] != "addhero")
{
    PrintUsage();
    return;
}

switch (args[1])
{
    case "deadlock":
        await ExtractHeroes(Paths.DeadlockVpk);
        break;

    case "csdk":
        await ExtractHeroes(Paths.CsdkVpk);
        break;

    default:
        PrintUsage();
        return;
}

await AddHeroDataAsync(Paths.ExtractedHeroes);



static async Task ExtractHeroes(string inputPath)
{
    var process = Process.Start(
        Paths.ExtractorExe,
        $@"-i ""{inputPath}"" -o ""{Paths.Resources}"" -f scripts/heroes.vdata_c");

    if (process != null)
        await process.WaitForExitAsync();
}

static async Task AddHeroDataAsync(string filePath)
{
    var heroMods = LoadHeroModifications();

    using var resource = new Resource();
    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    resource.Read(stream, verifyFileSize: false);

    if (resource.DataBlock is not BinaryKV3 binaryKv)
        return;

    var root = binaryKv.Data;

    foreach (var mod in heroMods.Heroes ?? Enumerable.Empty<HeroModification>())
    {
        if (!root.Properties.TryGetValue(mod.ReferenceName, out var original))
            continue;

        Console.WriteLine($"Cloning hero: {mod.CopyReferenceName}");

        var clone = KVDeepCopyExtensions.DeepClone(original);
        root.AddProperty(mod.CopyReferenceName, clone);

        var clonedHero = root.GetSubCollection(mod.CopyReferenceName);
        ApplyHeroModifications(clonedHero, mod);
    }

    await File.WriteAllTextAsync(Paths.OutputFile, binaryKv.ToString());
   
}
 
 

    
static void ApplyHeroModifications(KVObject heroData, HeroModification mod)
{
    var stats = heroData.GetSubCollection("m_mapStartingStats");
    var abilities = heroData.GetSubCollection("m_mapBoundAbilities");

    if(mod.HeroId.HasValue)
    {
        UpdateStat(heroData, "m_HeroID", mod.HeroId, KVValueType.Int32);
        UpdateStat(heroData, "m_bDisabled", true, KVValueType.Boolean);
    }

    if (stats == null || abilities == null)
        return;

    if (mod.MoveAcceleration.HasValue)
        UpdateStat(stats, "EMoveAcceleration", mod.MoveAcceleration.Value, KVValueType.Int32);

    if (mod.MoveSpeed.HasValue)
        UpdateStat(stats, "EMaxMoveSpeed", (double)mod.MoveSpeed.Value, KVValueType.FloatingPoint64);

    if (mod.Stamina.HasValue)
        UpdateStat(stats, "EStamina", (double)mod.Stamina.Value, KVValueType.FloatingPoint64);

    if (mod.StaminaRegeneration.HasValue)
        UpdateStat(stats, "EStaminaRegenPerSecond", (double)mod.StaminaRegeneration.Value, KVValueType.FloatingPoint64);

    if (mod.RemoveHeavyMelee == true)
        UpdateStat(abilities, "ESlot_Weapon_Melee", string.Empty, KVValueType.String);

    if (mod.RemoveJump == true)
        UpdateStat(abilities, "ESlot_Ability_Jump", string.Empty, KVValueType.String);
    
    
    
}

static void UpdateStat(KVObject container, string key, object value, KVValueType type)
{
    if (!container.Properties.TryGetValue(key, out var existing))
        return;

    container.AddProperty(key, new KVValue(type, existing.Flag, value));
}

static HeroModificationCollection LoadHeroModifications()
{
    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var yaml = File.ReadAllText(Paths.ConfigFile);
    return deserializer.Deserialize<HeroModificationCollection>(yaml);
}


static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run addhero deadlock");
    Console.WriteLine("  dotnet run addhero csdk");
}

public class HeroModification
{
    public required string HeroName { get; set; }
    public required string ReferenceName { get; set; }

    public string CopyReferenceName => $"{ReferenceName}_copy";

    public decimal? Stamina { get; set; }
    public decimal? StaminaRegeneration { get; set; }
    public decimal? MoveSpeed { get; set; }
    public int? MoveAcceleration { get; set; }

    public bool? RemoveHeavyMelee { get; set; }
    public bool? RemoveJump { get; set; }
    public int? HeroId { get; set; }
}

public class HeroModificationCollection
{
    public List<HeroModification> Heroes { get; set; } = new();

    public HeroModification GetHeroModification(string reference)
    {
        if(Heroes == null)
        {
            throw new Exception("No heroes modificaiton in collection");
        }
        
        var m = Heroes.FirstOrDefault(h => h.ReferenceName == reference);

        if(m == null)
        {
            throw new Exception($"No hero with reference '{reference}'");
        }

        return m;
    }
}

public static class DirectoryHelper
{
    private static string? _cachedRoot;

    public static string FindSingletonProjectRoot()
    {
        if (!string.IsNullOrWhiteSpace(_cachedRoot))
            return _cachedRoot;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Any())
                return _cachedRoot = dir.FullName;

            dir = dir.Parent;
        }

        throw new Exception("Couldn't locate project root");
    }
}

static class Paths
{
    public static readonly string ProjectRoot = DirectoryHelper.FindSingletonProjectRoot();

    public static readonly string Resources = Path.Combine(ProjectRoot, "resources");
    public static readonly string ConfigFile = Path.Combine(Resources, "heroesmodification.yaml");
    public static readonly string OutputFile = Path.Combine("generated", "heroes_modified.vdata");

    public static readonly string DeadlockVpk =
        @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk";

    public static readonly string CsdkVpk =
        @"C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\pak01_dir.vpk";

    public static readonly string ExtractedHeroes =
        Path.Combine(Resources, "scripts", "heroes.vdata_c");

    public static readonly string ExtractorExe =
        @"C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\cli-windows-x64\Source2Viewer-CLI.exe";
}




public static class KVDeepCopyExtensions
{
    /// <summary>
    /// Creates a deep copy of a KVValue.
    /// </summary>
    public static KVValue DeepClone(this KVValue kvValue)
    {
        if (kvValue.Value == null)
        {
            return new KVValue(kvValue.Type, kvValue.Flag, null);
        }

        // Recursively deep copy if the value is a KVObject
        if (kvValue.Value is KVObject kvObject)
        {
            return new KVValue(kvValue.Type, kvValue.Flag, kvObject.DeepClone());
        }

        // If the value is a byte array (BinaryBlob), duplicate the array
        if (kvValue.Value is byte[] byteArray)
        {
            var newArray = new byte[byteArray.Length];
            Buffer.BlockCopy(byteArray, 0, newArray, 0, byteArray.Length);
            return new KVValue(kvValue.Type, kvValue.Flag, newArray);
        }

        // For all other types (strings, ints, floats, booleans), they are 
        // either value types or immutable reference types, so it's safe to just pass the value.
        return new KVValue(kvValue.Type, kvValue.Flag, kvValue.Value);
    }

    /// <summary>
    /// Creates a deep copy of a KVObject.
    /// </summary>
    public static KVObject DeepClone(this KVObject source)
    {
        if (source == null)
        {
            return null;
        }

        // Initialize a new KVObject with the same Key, IsArray flag, and capacity
        var clone = new KVObject(source.Key, source.IsArray, source.Count);

        foreach (var kvp in source.Properties)
        {
            // KVObject.AddProperty handles IsArray logic internally by making up string keys based on Count.
            // We just pass null for the key if it's an array, or the original key if it's a standard object.
            if (source.IsArray)
            {
                clone.AddProperty(null, kvp.Value.DeepClone());
            }
            else
            {
                clone.AddProperty(kvp.Key, kvp.Value.DeepClone());
            }
        }

        return clone;
    }
}