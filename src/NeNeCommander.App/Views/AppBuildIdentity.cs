using System;
using System.Reflection;

namespace NeNeCommander.App.Views;

/// <summary>Reads only this executable's SDK-generated build attributes for its About display.</summary>
internal sealed class AppBuildIdentity
{
    internal const string RepositoryUrl = "https://github.com/hideyukiMORI/NeNeCommander";

    private AppBuildIdentity(string version, string copyright)
    {
        Version = version;
        Copyright = copyright;
    }

    internal string Version { get; }

    internal string Copyright { get; }

    internal static AppBuildIdentity Read()
    {
        Assembly assembly = typeof(CommanderApplication).Assembly;
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? throw new InvalidOperationException("The executable's build version is missing.");
        string copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
            ?? throw new InvalidOperationException("The executable's copyright is missing.");
        return new AppBuildIdentity(version, copyright);
    }
}
