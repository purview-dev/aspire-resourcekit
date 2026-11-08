using Purview.Architecture.Testing;

namespace Purview.Aspire.ResourceKit.Repository;

/// <summary>
/// Enforces <c>.config/architecture-policy.json</c> - the dependency direction between the runtime
/// model, the build-time Roslyn components and the sample hosts - as a failing build rather than a
/// convention a reviewer repeats.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>[InheritsTests]</c> is load-bearing.</b> TUnit does not walk base types when it discovers
/// tests, so without it this class compiles, the run is green, and no rule is checked.
/// </para>
/// <para>
/// There is no <c>RepositoryRoot</c> override: the default probe walks up from this test binary for the
/// policy and stops at the <c>.git</c> boundary, which is the same resolution a contributor gets.
/// </para>
/// </remarks>
[InheritsTests]
public sealed class ArchitectureTests : PurviewArchitectureTests;
