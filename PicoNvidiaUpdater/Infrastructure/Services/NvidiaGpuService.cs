using Microsoft.Win32;
using PicoNvidiaUpdater.Infrastructure.Helpers;
using PicoNvidiaUpdater.Infrastructure.Schemas;
using System.Management;
using System.Text.RegularExpressions;

namespace PicoNvidiaUpdater.Infrastructure.Services;

public interface INvidiaGpuService
{
    bool IsDCHDriver();
    bool IsSystemSupportsDCH();
    NvidiaOS DetermineOS(IEnumerable<NvidiaOS> oses);
    (NvidiaGpu gpu, string version) DetermineGPU(IEnumerable<NvidiaGpu> gpus, int chasisType = 0);
}

public class NvidiaGpuService : INvidiaGpuService
{
    public readonly int[] NOTEBOOK_CHASIS_TYPES = [1, 8, 9, 10, 11, 12, 14, 18, 21, 31, 32];

    public bool IsDCHDriver()
    {
        using var regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm", false);
        return regKey != null && regKey.GetValue("DCHUVen") != null;
    }

    public bool IsSystemSupportsDCH()
    {
        return Environment.Version.Build > 10240;
    }

    public NvidiaOS DetermineOS(IEnumerable<NvidiaOS> oses)
    {
        var osBit = Environment.Is64BitOperatingSystem ? "64" : "32";
        var osVersion = $"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}";

        if (osVersion == "10.0" && Environment.OSVersion.Version.Build >= 22000)
        {
            return oses.First(os => RegexPatterns.IsWindows11().IsMatch(os.Name));
        }

        return oses.First(os => os.Code == osVersion && Regex.IsMatch(os.Name, osBit));
    }

    public (NvidiaGpu gpu, string version) DetermineGPU(IEnumerable<NvidiaGpu> gpus, int chasisType = 0)
    {
        // check if the computer is a notebook
        var kind = NOTEBOOK_CHASIS_TYPES.Contains(chasisType) ? NvidiaGpuKind.Notebook : NvidiaGpuKind.Desktop;
        if (chasisType == 0)
        {
            var systemEnclosure = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure").Get();
            kind = systemEnclosure
                .Cast<ManagementBaseObject>()
                .Select(x => x["ChassisTypes"] as ushort[])
                .SelectMany(x => x)
                .Any(x => NOTEBOOK_CHASIS_TYPES.Contains(x)) ? NvidiaGpuKind.Notebook : NvidiaGpuKind.Desktop;
        }

        var gpuName = "";
        var version = "";

        // get local GPU
        var installedGpus = new ManagementObjectSearcher("SELECT Name, DriverVersion FROM Win32_VideoController").Get();
        foreach (var gpu in installedGpus)
        {
            var name = gpu["Name"].ToString();

            if (!RegexPatterns.ContainsNvidia().IsMatch(name) || !RegexPatterns.GPUName().IsMatch(name))
            {
                continue;
            }

            var rawDriverVersion = gpu["DriverVersion"].ToString().Replace(".", string.Empty);
            version = rawDriverVersion.Substring(rawDriverVersion.Length - 5, 5).Insert(3, ".");
            gpuName = RegexPatterns.GPUName().Match(name).Value.Trim().Replace("Super", "SUPER");
            break;
        }

        // find gpu
        return (gpus.First(x => x.Name == gpuName && x.Kind == kind), version);
    }

}
