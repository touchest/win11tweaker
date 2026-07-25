using Microsoft.Win32;

namespace Win11Tweaker.Core;

public enum RiskLevel
{
    Safe,
    Moderate,
    Risky
}

public enum TweakState
{
    Off,
    On,
    Drifted,
    Unsupported
}

public enum RegistryRoot
{
    CurrentUser,
    LocalMachine
}

public sealed record RegistryWrite
{
    public required RegistryRoot Root { get; init; }

    public required string Key { get; init; }

    public string? Name { get; init; }

    public RegistryValueKind Kind { get; init; } = RegistryValueKind.DWord;

    public required object OnValue { get; init; }

    public object? OffValue { get; init; }

    public bool PresenceOnly { get; init; }

    public bool AbsentWhenOn { get; init; }

    public bool SkipIfKeyAbsent { get; init; }

    public string Display => (Root == RegistryRoot.CurrentUser ? "HKCU\\" : "HKLM\\")
        + Key + (Name is null ? string.Empty : "\\" + Name);
}

public readonly record struct BuildRange(int Min, int Max)
{
    public static readonly BuildRange Any = new(0, int.MaxValue);

    public bool Includes(int build) => build >= Min && build <= Max;
}

public sealed record TweakDefinition
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required RiskLevel Risk { get; init; }

    public required IReadOnlyList<RegistryWrite> Writes { get; init; }

    public bool NeedsShellRestart { get; init; }

    public bool NeedsSignOut { get; init; }

    public BuildRange AppliesTo { get; init; } = BuildRange.Any;
}
