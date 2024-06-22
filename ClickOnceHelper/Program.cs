using System;
using System.Diagnostics;
using System.Deployment.Application;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

class Program
{
    private static InPlaceHostingManager _inPlaceHostingManager = null;

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a command (install/uninstall) and necessary arguments.");
            return;
        }

        string command = args[0].ToLower();

        switch (command)
        {
            case "install":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: install <installer_path>");
                    return;
                }

                string installerPath = args[1];
                InstallApplication(installerPath);
                break;

            case "uninstall":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: uninstall <program_name>");
                    return;
                }

                string programName = args[1];
                Uninstall(programName);
                break;

            default:
                Console.WriteLine("Unknown command. Please use 'install' or 'uninstall'.");
                break;
        }
    }

    private static void CloseProgramByName(string programName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(programName);
            foreach (Process process in processes)
            {
                process.Kill();
                WriteLog($"[!] Closed program: {programName}, Process ID: {process.Id}");
            }

            if (processes.Length == 0)
            {
                WriteLog($"[-] No running program found with the name: {programName}");
            }
        }
        catch (Exception ex)
        {
            WriteLog($"[!] Error closing program: {ex.Message}");
        }
    }

    public static void InstallApplication(string deployManifestUriStr)
    {
        try
        {
            Uri deploymentManifestUri = new Uri(deployManifestUriStr);
            InPlaceHostingManager _inPlaceHostingManager = new InPlaceHostingManager(deploymentManifestUri, false);

            _inPlaceHostingManager.GetManifestCompleted += new EventHandler<GetManifestCompletedEventArgs>(GetManifestCompleted);
            _inPlaceHostingManager.DownloadProgressChanged += new EventHandler<DownloadProgressChangedEventArgs>(DownloadProgressChanged);
            _inPlaceHostingManager.DownloadApplicationCompleted += new EventHandler<DownloadApplicationCompletedEventArgs>(DownloadApplicationCompleted);

            _inPlaceHostingManager.GetManifestAsync();
            WriteLog("[!] Downloading and installing application...");
        }
        catch (UriFormatException uriEx)
        {
            WriteLog($"[-] Unable to install, invalid URL: {uriEx.Message}");
            Environment.Exit(0);
        }
        catch (PlatformNotSupportedException platformEx)
        {
            WriteLog($"[-] Unable to install, unsupported platform: {platformEx.Message}");
            Environment.Exit(0);
        }
        catch (ArgumentException argumentEx)
        {
            WriteLog($"[-] Unable to install, invalid argument: {argumentEx.Message}");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            WriteLog($"[-] Unexpected error: {ex.Message}");
            Environment.Exit(0);
        }
    }

    private static void GetManifestCompleted(object sender, GetManifestCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            WriteLog("[-] Error getting manifest: " + e.Error.Message);
            return;
        }

        InPlaceHostingManager _inPlaceHostingManager = (InPlaceHostingManager)sender;
        _inPlaceHostingManager.AssertApplicationRequirements(true);
        _inPlaceHostingManager.DownloadApplicationAsync();
    }

    private static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        WriteLog("[!] Download progress: " + e.ProgressPercentage + "%");
    }

    private static void DownloadApplicationCompleted(object sender, DownloadApplicationCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            WriteLog("[-] Error downloading application: " + e.Error.Message);
            return;
        }

        WriteLog("[+] Application installed successfully.");
    }

    private static void Uninstall(string applicationName)
    {
        try
        {
            foreach (Process p in Process.GetProcessesByName(applicationName))
            {
                p.Kill();
                break;
            }

            string uninstallString = GetUninstallCommand(applicationName);
            if (string.IsNullOrEmpty(uninstallString))
            {
                WriteLog($"[-] Application '{applicationName}' not found in registry");
                Environment.Exit(1);
            }

            if (uninstallString.Contains($"{applicationName}.application"))
            {
                WriteLog($"[?] Uninstall string: {uninstallString}");
                ExecuteUninstall(uninstallString);
            }
            else
            {
                WriteLog($"[-] Uninstall string for '{applicationName}' not found or invalid");
                Environment.Exit(1);
            }

            WriteLog("[+] Successfully uninstalled");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            WriteLog($"[-] Error uninstalling: {ex.InnerException}{Environment.NewLine}{Environment.NewLine}---------{ex.Message}{Environment.NewLine}{Environment.NewLine}---------{ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static string GetUninstallCommand(string applicationName)
    {
        string uninstallString = null;

        using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall"))
        {
            if (key == null) return null;

            foreach (string subKey in key.GetSubKeyNames())
            {
                using (RegistryKey appTMP = key.OpenSubKey(subKey))
                {
                    if (appTMP == null) continue;

                    foreach (string appKeyTMP in appTMP.GetValueNames().Where(x => x.Equals("DisplayName")))
                    {
                        if (appTMP.GetValue(appKeyTMP).Equals(applicationName))
                        {
                            uninstallString = appTMP.GetValue("UninstallString") as string;
                            break;
                        }
                    }
                }
            }
        }

        return uninstallString;
    }

    private static void WriteLog(string logText)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logText}");
    }

    private static void ExecuteUninstall(string uninstallString)
    {
        string[] extractedInfo = ExtractInfo(uninstallString);

        WriteLog($"[?] PublicKeyToken: {extractedInfo[0]}, Culture: {extractedInfo[1]}, ProcessorArchitecture: {extractedInfo[2]}");

        if (extractedInfo[0] == null || extractedInfo[1] == null || extractedInfo[2] == null)
        {
            WriteLog("[-] Some data is missing from uninstall string");
            Environment.Exit(1);
        }

        string textualSubId;
        if (uninstallString.Contains("rundll32.exe dfshim.dll,ShArpMaintain "))
        {
            textualSubId = uninstallString.Replace("rundll32.exe dfshim.dll,ShArpMaintain ", "");
            WriteLog("[?] Using new method");
        }
        else
        {
            textualSubId = $"{extractedInfo[0]}, Culture={extractedInfo[1]}, PublicKeyToken={extractedInfo[2]}";
            WriteLog("[?] Using old method");
        }

        var deploymentServiceCom = new System.Deployment.Application.DeploymentServiceCom();
        MethodInfo _r_m_GetSubscriptionState = typeof(System.Deployment.Application.DeploymentServiceCom).GetMethod("GetSubscriptionState", BindingFlags.NonPublic | BindingFlags.Instance);
        var subState = _r_m_GetSubscriptionState.Invoke(deploymentServiceCom, new object[] { textualSubId });
        var subscriptionStore = subState.GetType().GetProperty("SubscriptionStore").GetValue(subState);
        subscriptionStore.GetType().GetMethod("UninstallSubscription").Invoke(subscriptionStore, new object[] { subState });
    }

    private static string[] ExtractInfo(string uninstallString)
    {
        string[] info = new string[3];

        GroupCollection groups = Regex.Match(uninstallString, "PublicKeyToken=(\\w+)", RegexOptions.IgnoreCase).Groups;
        if (groups.Count > 0)
        {
            info[0] = groups[1].Value;
        }

        groups = Regex.Match(uninstallString, "Culture=(\\w+)", RegexOptions.IgnoreCase).Groups;
        if (groups.Count > 0)
        {
            info[1] = groups[1].Value;
        }

        groups = Regex.Match(uninstallString, "ProcessorArchitecture=(\\w+)", RegexOptions.IgnoreCase).Groups;
        if (groups.Count > 0)
        {
            info[2] = groups[1].Value;
        }

        return info;
    }
}
