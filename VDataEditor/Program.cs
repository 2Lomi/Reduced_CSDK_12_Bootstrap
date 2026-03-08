using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Serialization.KeyValues;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using KVObject = ValveResourceFormat.Serialization.KeyValues.KVObject;
using KVValue = ValveResourceFormat.Serialization.KeyValues.KVValue;




var projectRoot = DirectoryHelper.FindSingletonProjectRoot();
var configurationFile = Path.Combine(projectRoot, "resources", "heroesmodification.yaml");

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var heroModifications = deserializer.Deserialize<HeroModificationCollection>(
    File.ReadAllText(configurationFile));
 
 //Todo move the file automatically ()
var resource = new Resource();
var filePath = Path.Combine(new DirectoryInfo(projectRoot).Parent.FullName, @"ConsoleApp1\Reduced_CSDK_12\game\citadel\scripts\heroes.vdata_c");

//var filePath = @"C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\heroes.vdata_c";
using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
resource.Read(stream, verifyFileSize: false);


var dataBlock = resource.GetBlockByType(BlockType.DATA) as BinaryKV3;

foreach (var heroKey in heroModifications.GetHeroesReferences())
{
    if (dataBlock.Data.Properties.ContainsKey(heroKey))
    {
        var heroData = dataBlock.Data.GetSubCollection(heroKey);
        var modification = heroModifications.GetHeroModification(heroKey);
        ApplyHeroModifications(heroData, modification);
        System.Console.WriteLine(heroKey + " modified");
    }
}

var kv3File = new KV3File(
    dataBlock.Data,
    encoding: KV3IDLookup.Get("text"),
    format: KV3IDLookup.Get("generic")
);

var output = kv3File.ToString();
File.WriteAllText("heroes_modified.vdata", output);

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
    public decimal? Stamina { get; set; }
    public decimal? StaminaRegeneration { get; set; }
    public decimal? MoveSpeed { get; set; }
    public bool? RemoveHeavyMelee {get;set;}
    public bool? RemoveJump {get;set;}
    public int? MoveAcceleration { get; set; }
}

public class HeroModificationCollection
{
    public List<HeroModification> NonImportantHeroes { get; set; }
    public List<HeroModification> NicheUntouchedHeroes { get; set; }
    public List<HeroModification> ImportantHeroes { get; set; }

    public HashSet<string> GetHeroesReferences() =>
        NonImportantHeroes
            .Select(c => c.ReferenceName)
            .Concat(NicheUntouchedHeroes.Select(c => c.ReferenceName))
            .Concat(ImportantHeroes.Select(c => c.ReferenceName))
            .ToHashSet();

    public List<HeroModification> GetAllHeroModifications() =>
        NonImportantHeroes
            .Concat(NicheUntouchedHeroes)
            .Concat(ImportantHeroes)
            .ToList();

    public HeroModification GetHeroModification(string reference) =>
        GetAllHeroModifications().First(c => c.ReferenceName == reference);
}