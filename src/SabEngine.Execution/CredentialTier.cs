namespace SabEngine.Execution;

/// <summary>
/// A coarse credential tier — per Section 4.4's least-privilege
/// principle: never use one standing, maximally-privileged credential
/// for every module against every target. Deliberately kept simple
/// (two tiers, not a full permissions model) for Phase 1.
///
/// The mapping from a module's <c>risk_level</c> (SabEngine.Modules,
/// PD-12) to a tier is a decision for whoever calls this resolver — kept
/// out of SabEngine.Execution on purpose, so this project doesn't need
/// to depend on the module manifest schema just to pick a credential.
/// As a convention: Low/Medium risk modules should request
/// <see cref="Standard"/>; High risk modules (like apply-patches)
/// should request <see cref="Elevated"/>.
/// </summary>
public enum CredentialTier
{
    Standard,
    Elevated,
}
