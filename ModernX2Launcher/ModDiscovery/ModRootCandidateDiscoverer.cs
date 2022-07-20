using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace ModernX2Launcher.ModDiscovery;

public interface IModRootCandidateCollector
{
    IEnumerable<ModRootCandidate> DiscoverCandidates(string modRootsPath);
    Task<IReadOnlyList<ModRootCandidate>> DiscoverCandidatesAsync(string modRootsPath);
}

public class ModRootCandidateDiscoverer : IModRootCandidateCollector
{
    private readonly IFileSystem _fileSystem;

    public ModRootCandidateDiscoverer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public ModRootCandidateDiscoverer() : this(new FileSystem())
    {
    }

    public IEnumerable<ModRootCandidate> DiscoverCandidates(string modRootsPath)
    {
        return _fileSystem.Directory.EnumerateDirectories(modRootsPath)
            .Select(modRootPath =>
            {
                IEnumerable<string> modManifests = _fileSystem.Directory.EnumerateFiles(modRootPath, "*.XComMod")
                    .Select(rootedPath => _fileSystem.Path.GetFileName(rootedPath));

                return new ModRootCandidate(modRootPath, modManifests.ToArray());
            });
    }

    public Task<IReadOnlyList<ModRootCandidate>> DiscoverCandidatesAsync(string modRootsPath)
    {
        return Task.Run(() => (IReadOnlyList<ModRootCandidate>) DiscoverCandidates(modRootsPath).ToArray());
    }
}