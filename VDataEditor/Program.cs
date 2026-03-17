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

        var clone = DeepCopyKVValue(original);
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

static KVValue DeepCopyKVValue(KVValue original)
{    
    if (original.Value is KVObject obj)
        return new KVValue(original.Type, DeepCopyKVObject(obj));
    
    return new KVValue(original.Type, original.Value);
}

static KVObject DeepCopyKVObject(KVObject original)
{
    var copy = new KVObject(null, original.Properties.Count);

    foreach (var kv in original.Properties)
        copy.AddProperty(kv.Key, DeepCopyKVValue(kv.Value));

    return copy;
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
