using System.Diagnostics;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Serialization.KeyValues;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using KVObject = ValveResourceFormat.Serialization.KeyValues.KVObject;
using KVValue = ValveResourceFormat.Serialization.KeyValues.KVValue;


if (args.Length == 1 && args[0] == "addSky")
{
    await AddSkyAsync();
    return;
}

if (args.Length != 2)
{
    PrintUsage();
    return;
}

string sdk = args[0];
string map = args[1];

switch (sdk)
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

string? configFile = Paths.GetConfigFile(map);
if (configFile == null)
{
    PrintUsage();
    return;
}

await AddHeroDataAsync(Paths.ExtractedHeroes, configFile, map);



static async Task ExtractHeroes(string inputPath)
{
    var process = Process.Start(
        Paths.ExtractorExe,
        $@"-i ""{inputPath}"" -o ""{Paths.Resources}"" -f scripts/heroes.vdata_c");

    if (process != null)
        await process.WaitForExitAsync();
}

static async Task AddSkyAsync()
{
    using var resource = new Resource();
    using var stream = new FileStream(Paths.ExtractedHeroes, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    resource.Read(stream, verifyFileSize: false);

    if (resource.DataBlock is not BinaryKV3 binaryKv)
        return;

    var root = binaryKv.Data;

    foreach (string mapName in new[] { "jumpschool", "jumpcontrol" })
    {
        string configFile = Paths.GetConfigFile(mapName)!;
        var heroMods = LoadHeroModifications(configFile);

        foreach (var mod in heroMods.Heroes ?? Enumerable.Empty<HeroModification>())
        {
            if (!root.Properties.TryGetValue(mod.ReferenceName, out var original))
                continue;

            string copyName = $"{mod.ReferenceName}_copy_{mapName}";
            Console.WriteLine($"Cloning hero: {copyName}");

            var clone = KVDeepCopyExtensions.DeepClone(original);
            root.AddProperty(copyName, clone);

            var clonedHero = root.GetSubCollection(copyName);
            ApplyHeroModifications(clonedHero, mod);
        }
    }

    string outputText = RemoveHeroBlock(binaryKv.ToString(), "hero_pk_runner");

    string skyrunnerBlock = ExtractSkyrunnerBlock(Paths.SkyHeroes);
    if (!string.IsNullOrEmpty(skyrunnerBlock))
    {
        int lastBrace = outputText.LastIndexOf('}');
        outputText = outputText.Substring(0, lastBrace)
            + "\thero_pk_runner = \n" + skyrunnerBlock + "\n}";
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Paths.OutputFile)!);
    await File.WriteAllTextAsync(Paths.OutputFile, outputText);
}

static string RemoveHeroBlock(string text, string heroName)
{
    var lines = text.Split('\n');
    var result = new System.Text.StringBuilder();
    int i = 0;

    while (i < lines.Length)
    {
        string trimmed = lines[i].TrimStart();
        if (trimmed.StartsWith(heroName) && trimmed.Contains('='))
        {
            i++; // skip the "heroName = " line
            // skip until we consume the full block
            int depth = 0;
            bool started = false;
            while (i < lines.Length)
            {
                foreach (char c in lines[i]) { if (c == '{') depth++; else if (c == '}') depth--; }
                if (!started && depth > 0) started = true;
                i++;
                if (started && depth == 0) break;
            }
        }
        else
        {
            result.Append(lines[i]);
            if (i < lines.Length - 1) result.Append('\n');
            i++;
        }
    }

    return result.ToString();
}

static string ExtractSkyrunnerBlock(string skyHeroesPath)
{
    var lines = File.ReadAllLines(skyHeroesPath);
    int startLine = -1;

    for (int i = 0; i < lines.Length; i++)
    {
        if (lines[i].TrimStart().StartsWith("hero_pk_runner"))
        {
            startLine = i + 1; // line with opening '{'
            break;
        }
    }

    if (startLine < 0 || startLine >= lines.Length)
        return string.Empty;

    var sb = new System.Text.StringBuilder();
    int depth = 0;
    bool started = false;

    for (int i = startLine; i < lines.Length; i++)
    {
        string line = lines[i];
        foreach (char c in line) { if (c == '{') depth++; else if (c == '}') depth--; }

        if (!started && depth > 0) started = true;

        if (started)
            sb.AppendLine('\t' + line.TrimStart());

        if (started && depth == 0)
            break;
    }

    return sb.ToString().TrimEnd();
}

static async Task AddHeroDataAsync(string filePath, string configFile, string map)
{
    var heroMods = LoadHeroModifications(configFile);

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

        string copyName = $"{mod.ReferenceName}_copy_{map}";
        Console.WriteLine($"Cloning hero: {copyName}");

        var clone = KVDeepCopyExtensions.DeepClone(original);
        root.AddProperty(copyName, clone);

        var clonedHero = root.GetSubCollection(copyName);
        ApplyHeroModifications(clonedHero, mod);
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Paths.OutputFile)!);
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

    if (mod.AirDashDistance.HasValue)
        UpdateStat(stats, "EAirDashDistanceInMeters", (double)mod.AirDashDistance.Value, KVValueType.FloatingPoint64);
    
    if (mod.AirDashDuration.HasValue)
        UpdateStat(stats, "EAirDashDuration", (double)mod.AirDashDuration.Value, KVValueType.FloatingPoint64);
    
    if (mod.GroundDashDistance.HasValue)
        UpdateStat(stats, "EGroundDashDistanceInMeters", (double)mod.GroundDashDistance.Value, KVValueType.FloatingPoint64);
    
    if (mod.GroundDashDuration.HasValue)
        UpdateStat(stats, "EGroundDashDuration", (double)mod.GroundDashDuration.Value, KVValueType.FloatingPoint64);

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

static HeroModificationCollection LoadHeroModifications(string configFile)
{
    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var yaml = File.ReadAllText(configFile);
    return deserializer.Deserialize<HeroModificationCollection>(yaml);
}


static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run csdk jumpcontrol");
    Console.WriteLine("  dotnet run csdk jumpschool");
    Console.WriteLine("  dotnet run deadlock jumpcontrol");
    Console.WriteLine("  dotnet run deadlock jumpschool");
    Console.WriteLine("  dotnet run addSky");
}

public class HeroModification
{
    public required string HeroName { get; set; }
    public required string ReferenceName { get; set; }

public decimal? Stamina { get; set; }
    public decimal? StaminaRegeneration { get; set; }
    public decimal? MoveSpeed { get; set; }
    public int? MoveAcceleration { get; set; }
    public decimal? GroundDashDistance { get; set; }
    public decimal? GroundDashDuration { get; set; }
    public decimal? AirDashDistance { get; set; }
    public decimal? AirDashDuration { get; set; }

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
    public static readonly string OutputFile = Path.Combine(ProjectRoot, "generated", "heroes_modified.vdata");

    public static string? GetConfigFile(string map) => map switch
    {
        "jumpcontrol" => Path.Combine(Resources, "heroesmodification_jumpcontrol.yaml"),
        "jumpschool"  => Path.Combine(Resources, "heroesmodification_jumpschool.yaml"),
        _             => null
    };

    public static readonly string DeadlockVpk =
        @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk";

    public static readonly string CsdkVpk =
        @"C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\pak01_dir.vpk";

    public static readonly string ExtractedHeroes =
        Path.Combine(Resources, "scripts", "heroes.vdata_c");

    public static readonly string ExtractorExe =
        @"C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\cli-windows-x64\Source2Viewer-CLI.exe";

    public static readonly string SkyHeroes =
        Path.Combine(Resources, "sky_heroes.vdata");
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