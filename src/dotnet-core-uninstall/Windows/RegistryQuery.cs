﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Uninstall.Shared.BundleInfo;
using Microsoft.DotNet.Tools.Uninstall.Shared.BundleInfo.Versioning;
using Microsoft.DotNet.Tools.Uninstall.Shared.Utils;
using Microsoft.Win32;

namespace Microsoft.DotNet.Tools.Uninstall.Windows
{
    internal static class RegistryQuery
    {
        public static IEnumerable<Bundle> GetInstalledBundles()
        {
            var uninstalls = Registry.LocalMachine
                .OpenSubKey("SOFTWARE");

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64 || RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                uninstalls = uninstalls.OpenSubKey("WOW6432Node");
            }

            uninstalls = uninstalls
                .OpenSubKey("Microsoft")
                .OpenSubKey("Windows")
                .OpenSubKey("CurrentVersion")
                .OpenSubKey("Uninstall");

            var names = uninstalls.GetSubKeyNames();

            var bundles = names
                .Select(name => uninstalls.OpenSubKey(name))
                .Where(bundle => IsDotNetCoreBundle(bundle));

            return bundles
                .Select(bundle => WrapRegistryKey(bundle));
        }

        private static bool IsDotNetCoreBundle(RegistryKey registryKey)
        {
            return IsDotNetCoreBundleDisplayName(registryKey.GetValue("DisplayName") as string)
                && IsDotNetCoreBundlePublisher(registryKey.GetValue("Publisher") as string)
                && IsDotNetCoreBundleUninstaller(registryKey.GetValue("WindowsInstaller") as int?);
        }

        private static bool IsDotNetCoreBundleDisplayName(string displayName)
        {
            return displayName == null ?
                false :
                Regexes.BundleDisplayNameRegex.IsMatch(displayName);
        }

        private static bool IsDotNetCoreBundlePublisher(string publisher)
        {
            return publisher == null ?
                false :
                Regexes.BundlePublisherRegex.IsMatch(publisher);
        }

        private static bool IsDotNetCoreBundleUninstaller(int? windowsInstaller)
        {
            return windowsInstaller == null;
        }

        private static Bundle WrapRegistryKey(RegistryKey registryKey)
        {
            var displayName = registryKey.GetValue("DisplayName") as string;
            var uninstallCommand = registryKey.GetValue("QuietUninstallString") as string;
            var bundleCachePath = registryKey.GetValue("BundleCachePath") as string;

            ParseVersionAndArch(displayName, bundleCachePath, out var version, out var arch);

            return Bundle.From(version, arch, uninstallCommand, displayName);
        }

        private static void ParseVersionAndArch(string displayName, string bundleCachePath, out BundleVersion version, out BundleArch arch)
        {
            var match = Regexes.BundleDisplayNameRegex.Match(displayName);
            var cachePathMatch = Regexes.BundleCachePathRegex.Match(bundleCachePath);
            var archString = cachePathMatch.Groups[Regexes.ArchGroupName].Value ?? string.Empty;
            var versionString = cachePathMatch.Groups[Regexes.VersionGroupName].Value;
            var hasAuxVersion = cachePathMatch.Groups[Regexes.AuxVersionGroupName].Success;
            var footnote = hasAuxVersion ?
                string.Format(LocalizableStrings.HostingBundleFootnoteFormat, displayName, versionString) :
                null;

            switch (match.Groups[Regexes.TypeGroupName].Value)
            {
                case "SDK": version = new SdkVersion(versionString); break;
                case "Runtime": version = new RuntimeVersion(versionString); break;
                case "ASP.NET": version = new AspNetRuntimeVersion(versionString); break;
                case "Windows Server Hosting": version = new HostingBundleVersion(versionString, footnote); break;
                default: throw new ArgumentException();
            }

            switch (archString)
            {
                case "x64": arch = BundleArch.X64; break;
                case "x86": arch = BundleArch.X86; break;
                case "": arch = BundleArch.X64 | BundleArch.X86; break;
                default: throw new ArgumentException();
            }
        }
    }
}
