using System.Collections.Generic;
using System.Diagnostics;

namespace ModernX2Launcher.ModDiscovery;

/// <summary>
/// A directory that could be a mod root directory
/// </summary>
[DebuggerDisplay("{DirectoryPath} V={IsValid}")]
public sealed class ModRootCandidate
{
    public readonly string DirectoryPath;
    
    /// <summary>
    /// Mod manifest is a `.XComMod` file. The names here do not contain the path, but do include the extension
    /// </summary>
    public readonly IReadOnlyCollection<string> ModManifestsNames;

    public ModRootCandidate(string directoryPath, IReadOnlyCollection<string> modManifestsNames)
    {
        DirectoryPath = directoryPath;
        ModManifestsNames = modManifestsNames;
    }

    public bool IsValid => ModManifestsNames.Count == 1;
}