using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    private static readonly string CLI_SOURCE_2_VIEWER_FOLDER_NAME = "cli-windows-x64";
    private static readonly string CLI_SOURCE_2_VIEWER_EXE_NAME = "Source2Viewer-CLI.exe";
    private static readonly string DEPOT_DOWNLOADER_FOLDER_NAME = "DepotDownloader";
    private static readonly string DEPOT_DOWNLOADER_EXE_NAME = "DepotDownloader.exe";
    private static readonly string REDUCED_CSDK_12_FOLDER_NAME = "Reduced_CSDK_12";
    private static readonly string REDUCED_CSDK_12_ZIP_NAME = "Reduced_CSDK_12.zip";
    private static readonly string REDUCED_CSDK_12_FILE_ID = "1-Z-4CszWQNudzwzs6e6abPsp5RGFOURS";

    private static readonly string DEADLOCK_GAME_CORE_PAK_01_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk";
    private static readonly string DEADLOCK_GAME_CITADEL_PAK_01_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\core\pak01_dir.vpk";

    private static string ProjectRoot => DirectoryHelper.FindSingletonProjectRoot();
    private static string CLI_SOURCE_2_VIEWER_EXE_PATH => Path.Combine(ProjectRoot, CLI_SOURCE_2_VIEWER_FOLDER_NAME, CLI_SOURCE_2_VIEWER_EXE_NAME);
    private static string CLI_SOURCE_2_VIEWER_FOLDER_PATH => Path.Combine(ProjectRoot, CLI_SOURCE_2_VIEWER_FOLDER_NAME);
    private static string DEPOT_DOWNLOADER_FOLDER_PATH => Path.Combine(ProjectRoot, DEPOT_DOWNLOADER_FOLDER_NAME);
    private static string DEPOT_DOWNLOADER_EXE_PATH => Path.Combine(ProjectRoot, DEPOT_DOWNLOADER_FOLDER_NAME, DEPOT_DOWNLOADER_EXE_NAME);
    private static string REDUCED_CSDK_12_FOLDER_PATH => Path.Combine(ProjectRoot, REDUCED_CSDK_12_FOLDER_NAME);
    private static string REDUCED_CSDK_12_GAME_CITADEL_PATH => Path.Combine(REDUCED_CSDK_12_FOLDER_PATH, "game", "citadel");
    private static string REDUCED_CSDK_12_GAME_CORE_PATH => Path.Combine(REDUCED_CSDK_12_FOLDER_PATH, "game", "core");
    private static string REDUCED_CSDK_12_GAME_CITADEL_PAK_01_PATH => Path.Combine(REDUCED_CSDK_12_FOLDER_PATH, "game", "citadel", "pak01_dir.vpk");
    private static string REDUCED_CSDK_12_ZIP_PATH => Path.Combine(ProjectRoot, REDUCED_CSDK_12_ZIP_NAME);

    static async Task Main(string[] args)
    { 

        Console.WriteLine("YOU'RE ABOUT TO CLEAR UP YOUR WHOLE CSDK FOLDER");
        Console.WriteLine("STOP NOW ? 5 SEC UNTIL RUN, NO INPUT WE'RE ALL IN!");
        await Task.Delay(5000);
        if (!await DownloadAndExtractCSDK12Zip()) return;
        EmptyAndExtractCSDK12();
        if (!await DownloadAndExtractDepotDownloader()) return;
        await RunDepotDownloaderAndExtractManifests();
        await DownloadAndExtractCLISourceViewer2();
        await ExtractCSDKGameCitadelPak01();
        ExtractCSDK12AndOverrideFiles();
        SwitchQwertyToAzerty(); 
    }

    private static void SwitchQwertyToAzerty()
    {
        string filePath = Path.Combine(DirectoryHelper.FindSingletonProjectRoot(), @"Reduced_CSDK_12\game\core\tools\keybindings\shared_tool_key_bindings.txt");
      
        try
        {
            // Read the file content
            string content = File.ReadAllText(filePath);

            // Define the replacements
            var replacements = new Dictionary<string, string>
            {
                // MoveCameraForward3D: W -> Z
                { @"m_Command = ""MoveCameraForward3D""\s+m_Input = ""W""", @"m_Command = ""MoveCameraForward3D""			m_Input = ""Z""" },

                // MoveCameraLeft3D: A -> Q
                { @"m_Command = ""MoveCameraLeft3D""\s+m_Input = ""A""", @"m_Command = ""MoveCameraLeft3D""				m_Input = ""Q""" },

                // MouseControlCamera3D_Toggle: Z -> W
                { @"m_Command = ""MouseControlCamera3D_Toggle""\s+m_Input = ""Z""", @"m_Command = ""MouseControlCamera3D_Toggle""	m_Input = ""W""" },

                // SnapCameraToSelection: Shift+A -> Shift+Q
                { @"m_Command = ""SnapCameraToSelection""\s+m_Input = ""Shift\+A""", @"m_Command = ""SnapCameraToSelection""			m_Input = ""Shift+Q""" },

                // FitAllInView: Ctrl+Shift+A -> Ctrl+Shift+Q
                { @"m_Command = ""FitAllInView""\s+m_Input = ""Ctrl\+Shift\+A""", @"m_Command = ""FitAllInView""					m_Input = ""Ctrl+Shift+Q""" }
            };

            // Apply each replacement
            foreach (var replacement in replacements)
            {
                content = Regex.Replace(content, replacement.Key, replacement.Value);
            }

            // Write the modified content back to the file
            File.WriteAllText(filePath, content);

            Console.WriteLine("Key bindings file has been successfully modified!");
            Console.WriteLine("Changes applied:");
            Console.WriteLine("  - MoveCameraForward3D: W → Z");
            Console.WriteLine("  - MoveCameraLeft3D: A → Q");
            Console.WriteLine("  - MouseControlCamera3D_Toggle: Z → W");
            Console.WriteLine("  - SnapCameraToSelection: Shift+A → Shift+Q");
            Console.WriteLine("  - FitAllInView: Ctrl+Shift+A → Ctrl+Shift+Q");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static void ClearUpAll()
    {
        ClearVpkFiles(REDUCED_CSDK_12_GAME_CITADEL_PATH);
        ClearVpkFiles(REDUCED_CSDK_12_GAME_CORE_PATH);
    }

    private static void ClearVpkFiles(string path)
    {
        var vpkFiles = Directory.GetFiles(path)
            .Where(f => f.Contains("pak01_") && f.EndsWith(".vpk"));

        foreach (var file in vpkFiles)
        {
            File.Delete(file);
        }
    }

    private static void EmptyAndExtractCSDK12()
    {
        if (Directory.Exists(REDUCED_CSDK_12_FOLDER_PATH))
        {
            Console.WriteLine("Reduced_CSDK_12 Directory already exists");
            var fileCount = Directory.GetFiles(REDUCED_CSDK_12_FOLDER_PATH).Length;
            Console.WriteLine($"File count: {fileCount}");

            if (fileCount > 0)
            {
                Console.WriteLine("Reduced_CSDK_12 was not empty! Deleting...");
                DirectoryHelper.Empty(new DirectoryInfo(REDUCED_CSDK_12_FOLDER_PATH));
                Directory.Delete(REDUCED_CSDK_12_FOLDER_PATH);
                Console.WriteLine("Deletion complete!");
            }
        }
        else
        {
            Console.WriteLine("Reduced_CSDK_12 is empty. Creating folder...");
            Directory.CreateDirectory(REDUCED_CSDK_12_FOLDER_PATH);
        }

        ZipFile.ExtractToDirectory(REDUCED_CSDK_12_ZIP_PATH, ProjectRoot);
        Console.WriteLine("Extraction complete!");
    }

    private static void ExtractCSDK12AndOverrideFiles()
    {
        if (!Directory.Exists(REDUCED_CSDK_12_FOLDER_PATH))
            return;

        var fileCount = Directory.GetFiles(REDUCED_CSDK_12_FOLDER_PATH).Length;
        if (fileCount > 0)
        {
            Console.WriteLine("Reduced_CSDK_12 exists and is not empty. Overriding files...");
            ZipFile.ExtractToDirectory(REDUCED_CSDK_12_ZIP_PATH, ProjectRoot, overwriteFiles: true);
            Console.WriteLine("Extraction override complete!");
        }
    }

    private static async Task ExtractCSDKGameCitadelPak01()
    {
        await RunCliSourceViewer(REDUCED_CSDK_12_GAME_CITADEL_PAK_01_PATH, REDUCED_CSDK_12_GAME_CITADEL_PATH);
    }

    private static async Task ExtractDeadlockGameCorePak01()
    {
        await RunCliSourceViewer(DEADLOCK_GAME_CORE_PAK_01_PATH, REDUCED_CSDK_12_GAME_CORE_PATH);
    }

    private static async Task ExtractDeadlockGameCitadelPak01()
    {
        await RunCliSourceViewer(DEADLOCK_GAME_CITADEL_PAK_01_PATH, REDUCED_CSDK_12_GAME_CITADEL_PATH);
    }

    private static async Task RunCliSourceViewer(string inputPath, string outputPath)
    {
        var process = Process.Start(CLI_SOURCE_2_VIEWER_EXE_PATH, $"-i {inputPath} -o {outputPath}");
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    private static async Task RunDepotDownloaderAndExtractManifests()
    {
        var depotDownloaderArgs = new[]
        {
            "-app 1422450 -depot 1422451 -manifest 2639812037154209539 -qr -dir " + REDUCED_CSDK_12_FOLDER_PATH,
            "-app 1422450 -depot 1422456 -manifest 6378769520310560496 -qr -dir " + REDUCED_CSDK_12_FOLDER_PATH
        };

        foreach (var args in depotDownloaderArgs)
        {
            var process = Process.Start(DEPOT_DOWNLOADER_EXE_PATH, args);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
    }

    private static async Task<bool> DownloadAndExtractCSDK12Zip()
    {
        try
        {
            Console.WriteLine($"Target path: {REDUCED_CSDK_12_ZIP_PATH}");

            if (!File.Exists(REDUCED_CSDK_12_ZIP_PATH))
            {
                await GoogleDriveDownloader.DownloadFileAsync(REDUCED_CSDK_12_FILE_ID, REDUCED_CSDK_12_ZIP_PATH);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> DownloadAndExtractDepotDownloader()
    {
        if (Directory.Exists(DEPOT_DOWNLOADER_FOLDER_PATH) && File.Exists(DEPOT_DOWNLOADER_EXE_PATH))
        {
            Console.WriteLine("DepotDownloader already exists!");
            return true;
        }

        return await DownloadAndExtractFromGitHub("SteamRE", "DepotDownloader", DEPOT_DOWNLOADER_FOLDER_PATH);
    }

    private static async Task<bool> DownloadAndExtractCLISourceViewer2()
    {
        if (Directory.Exists(CLI_SOURCE_2_VIEWER_FOLDER_PATH) && File.Exists(CLI_SOURCE_2_VIEWER_EXE_PATH))
        {
            Console.WriteLine("CLI Source 2 Viewer already exists!");
            return true;
        }

        return await DownloadAndExtractFromGitHub("ValveResourceFormat", "ValveResourceFormat", CLI_SOURCE_2_VIEWER_FOLDER_PATH);
    }

    private static async Task<bool> DownloadAndExtractFromGitHub(string owner, string repo, string extractionPath)
    {
        try
        {
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DotNetApp", "1.0"));

            Console.WriteLine("Fetching latest release info...");
            string json = await client.GetStringAsync(apiUrl);

            using JsonDocument doc = JsonDocument.Parse(json);
            var assets = doc.RootElement.GetProperty("assets");

            if (assets.GetArrayLength() == 0)
            {
                Console.WriteLine("No assets found.");
                return false;
            }

            var selectedAsset = SelectAssetFromUser(assets);
            if (selectedAsset == null)
                return false;

            string downloadUrl = selectedAsset.Value.GetProperty("browser_download_url").GetString();
            string fileName = selectedAsset.Value.GetProperty("name").GetString();
            string downloadedZipPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Console.WriteLine($"Downloading {fileName}...");
            await DownloadFile(client, downloadUrl, downloadedZipPath);

            if (Directory.Exists(extractionPath))
            {
                Directory.Delete(extractionPath, true);
            }

            Console.WriteLine("Extracting...");
            ZipFile.ExtractToDirectory(downloadedZipPath, extractionPath);
            File.Delete(downloadedZipPath);

            Console.WriteLine($"Done! Extracted to: {extractionPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private static JsonElement? SelectAssetFromUser(JsonElement assets)
    {
        Console.WriteLine("\nAvailable Assets:");
        for (int i = 0; i < assets.GetArrayLength(); i++)
        {
            string name = assets[i].GetProperty("name").GetString();
            Console.WriteLine($"{i + 1}. {name}");
        }

        Console.Write("\nSelect the number of the asset to download: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > assets.GetArrayLength())
        {
            Console.WriteLine("Invalid selection.");
            return null;
        }

        return assets[choice - 1];
    }

    private static async Task DownloadFile(HttpClient client, string downloadUrl, string filePath)
    {
        using var response = await client.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await response.Content.CopyToAsync(fs);
    }
}

public class ZipHelper
{
    public static void ExtractFolderFromZip(string zipPath, string targetFolderInZip, string destinationPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        string normalizedTarget = targetFolderInZip.TrimEnd('/');

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith(normalizedTarget + "/", StringComparison.OrdinalIgnoreCase))
                continue;

            string destinationFilePath = Path.Combine(destinationPath, entry.FullName);
            string directoryPath = Path.GetDirectoryName(destinationFilePath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!string.IsNullOrEmpty(entry.Name))
            {
                entry.ExtractToFile(destinationFilePath, overwrite: true);
            }
        }
    }
}

public static class DirectoryHelper
{
    private static string _projectRootCache;

    public static string FindSingletonProjectRoot()
    {
        if (!string.IsNullOrWhiteSpace(_projectRootCache))
            return _projectRootCache;

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

    public static void Empty(DirectoryInfo directory)
    {
        foreach (var file in directory.GetFiles())
            file.Delete();

        foreach (var subDirectory in directory.GetDirectories())
            subDirectory.Delete(true);
    }
}

public static class GoogleDriveDownloader
{
    public static async Task DownloadFileAsync(string fileId, string outputPath)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All
        };

        using var client = new HttpClient(handler);
        string initialUrl = $"https://drive.google.com/uc?export=download&id={fileId}";

        var initialResponse = await client.GetAsync(initialUrl);
        string html = await initialResponse.Content.ReadAsStringAsync();

        var confirmMatch = Regex.Match(html, @"name=""confirm"" value=""([^""]+)""");
        var uuidMatch = Regex.Match(html, @"name=""uuid"" value=""([^""]+)""");

        string downloadUrl = initialUrl;

        if (confirmMatch.Success && uuidMatch.Success)
        {
            string confirm = confirmMatch.Groups[1].Value;
            string uuid = uuidMatch.Groups[1].Value;
            downloadUrl = $"https://drive.usercontent.google.com/download?id={fileId}&export=download&confirm={confirm}&uuid={uuid}";
        }

        Console.WriteLine("Starting file download...");

        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await httpStream.CopyToAsync(fileStream);

        Console.WriteLine("Download completed.");
    }
}