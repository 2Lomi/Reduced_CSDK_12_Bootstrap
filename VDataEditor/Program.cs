using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Serialization.KeyValues;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using KVObject = ValveResourceFormat.Serialization.KeyValues.KVObject;
using KVValue = ValveResourceFormat.Serialization.KeyValues.KVValue;


 

var entryArgument = args[0];

var projectRoot = DirectoryHelper.FindSingletonProjectRoot();
string heroesVdataCPath = "";
// if (entryArgument == "csdk")
// {
//     heroesVdataCPath = Path.Combine(new DirectoryInfo(projectRoot).Parent.FullName, @"ConsoleApp1\Reduced_CSDK_12\game\citadel\scripts\heroes.vdata_c");
//     OverrideHeroDatas(heroesVdataCPath);
// }
// else if (entryArgument == "deadlock")
// {
//     await ExtractHeroesVdataC(@"C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk", @"C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\resources");
//     heroesVdataCPath = @"C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\heroes.vdata_c";
//     OverrideHeroDatas(heroesVdataCPath);
// }
System.Console.WriteLine("Valid execution :");
System.Console.WriteLine("dotnet run addhero deadlock");
System.Console.WriteLine("dotnet run addhero csdk");
if(entryArgument == "addhero")
{
    if(args.Length != 2)
    {
        return;
    }
    if(args[1] == "deadlock")
    {
        await ExtractHeroesVdataC(@"""C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk""", @"C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\resources");
        heroesVdataCPath = @"C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\resources\scripts\heroes.vdata_c";  
        await AddHeroDataAsync(heroesVdataCPath);    
    }
    else if(args[1] == "csdk")
    {
        await ExtractHeroesVdataC(@"C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\pak01_dir.vpk", @"C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\resources");
        heroesVdataCPath = @"C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\resources\scripts\heroes.vdata_c";  
        await AddHeroDataAsync(heroesVdataCPath); 
    }
    else
    {
        return;

    } 
} 
else
{
    System.Console.WriteLine("input should be either 'deadlock' or 'csdk' or 'addhero'");
    return;
}

static async Task ExtractHeroesVdataC(string inputPath, string outputPath)
{       
    var process = Process.Start(
        @"C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\cli-windows-x64\Source2Viewer-CLI.exe",  
        $@"-i {inputPath} -o {outputPath} -f scripts/heroes.vdata_c");

    if (process != null)
    {
        await process.WaitForExitAsync();
    }
}

async Task AddHeroDataAsync(string filePath)
{ 
    var outputPath = Path.Combine("generated", "heroes_modified.vdata");
    
    var projectRoot = DirectoryHelper.FindSingletonProjectRoot();
    var configurationFile = Path.Combine(projectRoot, "resources", "heroesmodification.yaml");
    
    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var heroModifications = deserializer.Deserialize<HeroModificationCollection>(File.ReadAllText(configurationFile));

    using var resource = new Resource();
    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    resource.Read(stream, verifyFileSize: false); 
    if (resource.DataBlock is BinaryKV3 binaryKv)
    {
        var root = binaryKv.Data;

        foreach (var heroModification in heroModifications.Heroes)
        {
            if (root.Properties.TryGetValue(heroModification.ReferenceName, out var originalValue))
            {
                
                Console.WriteLine($"Cloning compiled hero: {heroModification.CopyReferenceName}...");
           
                // 3. Deep Copy
                var clonedValue = DeepCopyKVValue(originalValue);
                  
                root.AddProperty($"{heroModification.CopyReferenceName}", clonedValue);

                ApplyHeroModifications(root.GetSubCollection($"{heroModification.CopyReferenceName}") ,heroModification); 

                var mm = root.GetSubCollection($"{heroModification.CopyReferenceName}");
            }
        }
 
        var kv3Text = binaryKv.ToString();
        
        File.WriteAllText(outputPath, kv3Text);
    }
}
 
static void OverrideHeroDatas(string filePath)
{
    var projectRoot = DirectoryHelper.FindSingletonProjectRoot();
    var configurationFile = Path.Combine(projectRoot, "resources", "heroesmodification.yaml");

    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var heroModifications = deserializer.Deserialize<HeroModificationCollection>(
        File.ReadAllText(configurationFile));
     
    var resource = new Resource();

    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    resource.Read(stream, verifyFileSize: false);


    var dataBlock = resource.GetBlockByType(BlockType.DATA) as BinaryKV3;

    foreach (var hero in heroModifications.Heroes)
    {
        if (dataBlock.Data.Properties.ContainsKey(hero.ReferenceName))
        {
            var heroData = dataBlock.Data.GetSubCollection(hero.ReferenceName);
            var modification = heroModifications.GetHeroModification(hero.ReferenceName);
            ApplyHeroModifications(heroData, modification);
            System.Console.WriteLine(hero.ReferenceName + " modified");
        }
    }

    var kv3File = new KV3File(
        dataBlock.Data,
        encoding: KV3IDLookup.Get("text"),
        format: KV3IDLookup.Get("generic")
    );

    var output = kv3File.ToString();
    var outputPath = Path.Combine("generated", "heroes_modified.vdata");
    File.WriteAllText(outputPath, output);
}


static KVValue DeepCopyKVValue(KVValue original)
{
    if (original.Value is KVObject originalObj)
    {
        var clonedObj = DeepCopyKVObject(originalObj);
        return new KVValue(original.Type, clonedObj);
    }
    return new KVValue(original.Type, original.Value);
}

static KVObject DeepCopyKVObject(KVObject original)
{
    // Capacity constructor: KVObject(string name, int capacity)
    var copy = new KVObject(null, original.Properties.Count);
    foreach (var kvp in original.Properties)
    {
        copy.AddProperty(kvp.Key, DeepCopyKVValue(kvp.Value));
    }
    return copy;
}

static void ApplyHeroModifications(KVObject heroData, HeroModification modification)
{
    var stats = heroData.GetSubCollection("m_mapStartingStats");
    var abilties = heroData.GetSubCollection("m_mapBoundAbilities");
    
 
    if (modification.MoveAcceleration.HasValue)
    {
        UpdateStat(stats, "EMoveAcceleration", modification.MoveAcceleration.Value, KVValueType.Int32);
    }

    if (modification.MoveSpeed.HasValue)
    {
        UpdateStat(stats, "EMaxMoveSpeed", Convert.ToDouble(modification.MoveSpeed.Value), KVValueType.FloatingPoint64);
    }

    if (modification.Stamina.HasValue)
    {
        UpdateStat(stats, "EStamina", modification.Stamina.Value, KVValueType.FloatingPoint64);
    }

    if (modification.StaminaRegeneration.HasValue)
    {
        UpdateStat(stats, "EStaminaRegenPerSecond", Convert.ToDouble(modification.StaminaRegeneration.Value), KVValueType.FloatingPoint64);
    }

    if(modification.RemoveHeavyMelee.HasValue && modification.RemoveHeavyMelee == true)
    { 
        UpdateStat(abilties, "ESlot_Weapon_Melee", String.Empty, KVValueType.String);
    }

    if(modification.RemoveJump.HasValue && modification.RemoveJump == true)
    { 
        UpdateStat(abilties, "ESlot_Ability_Jump", String.Empty, KVValueType.String);
    }

    
}

static void UpdateStat(KVObject stats, string statName, object value, KVValueType valueType)
{
    var existingValue = stats.Properties[statName];
    stats.AddProperty(statName, new KVValue(valueType, existingValue.Flag, value));
}

public static class DirectoryHelper
{
    private static string _projectRootCache;

    public static string FindSingletonProjectRoot()
    {
        if (!string.IsNullOrWhiteSpace(_projectRootCache))
        {
            return _projectRootCache;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Any())
            {
                _projectRootCache = dir.FullName;
                return _projectRootCache;
            }

            dir = dir.Parent;
        }

        throw new Exception("Couldn't locate project root");
    }

    public static void Empty(this DirectoryInfo directory)
    {
        foreach (var file in directory.GetFiles())
            file.Delete();

        foreach (var subDirectory in directory.GetDirectories())
            subDirectory.Delete(true);
    }
}

public class HeroModification
{
    public string HeroName { get; set; }
    public string ReferenceName { get; set; }
    public string CopyReferenceName => ReferenceName + "_copy";
    public decimal? Stamina { get; set; }
    public decimal? StaminaRegeneration { get; set; }
    public decimal? MoveSpeed { get; set; }
    public bool? RemoveHeavyMelee {get;set;}
    public bool? RemoveJump {get;set;}
    public int? MoveAcceleration { get; set; }
}

public class HeroModificationCollection
{
    public List<HeroModification> Heroes { get; set; }
 
    public HeroModification GetHeroModification(string reference) =>
        Heroes.First(c => c.ReferenceName == reference);
}