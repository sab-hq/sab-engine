using SabEngine.Core;

namespace SabEngine.Modules;

/// <summary>
/// Projects a full manifest down to the narrower <see cref="ModuleCandidate"/>
/// shape SabAgent.ProposePlanAsync (PD-6) already knows how to consume.
/// This is what closes the loop PD-6 deliberately left open — PD-6 noted
/// there was no real module catalog loader yet, so the agent took its
/// available-module list as a plain parameter. A real catalog loader
/// (parsing every manifest in an OSML checkout, not itemized as its own
/// PD- entry yet) can now build that list from real manifests via this
/// extension, rather than hand-built <see cref="ModuleCandidate"/> instances.
/// </summary>
public static class ModuleManifestExtensions
{
    public static ModuleCandidate ToModuleCandidate(this ModuleManifest manifest) =>
        new(manifest.Id, manifest.Name, manifest.Version, manifest.ValidationStatus, manifest.Rollback.Tested);
}
