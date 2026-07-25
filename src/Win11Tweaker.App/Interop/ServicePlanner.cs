using System.Collections.Generic;
using System.Linq;

namespace Win11Tweaker.App.Interop;

public static class ServicePlanner
{
    public static (List<(string Name, int Value)> Plan, int ProtectedByChain) Build(
        IEnumerable<string> keepSeed, IEnumerable<string> dropSeed)
    {
        var keep = new HashSet<string>(keepSeed, StringComparer.OrdinalIgnoreCase);
        var drop = new HashSet<string>(dropSeed, StringComparer.OrdinalIgnoreCase);

        drop.ExceptWith(keep);
        drop.RemoveWhere(s => !ServiceControl.Exists(s));

        var enabled = ServiceControl.EnabledServices();
        var stayingSeed = enabled.Where(e => !drop.Contains(e));

        var protectedSet = ServiceDependencies.ForwardClosure(stayingSeed);

        var candidates = drop.Where(enabled.Contains).ToList();
        var toDisable = candidates.Where(s => !protectedSet.Contains(s)).ToList();
        var protectedByChain = candidates.Count - toDisable.Count;

        var toRestore = keep
            .Where(s => ServiceControl.Exists(s) && ServiceControl.IsDisabled(s))
            .ToList();

        List<(string Name, int Value)> plan =
        [
            .. toDisable.Select(s => (s, ServiceControl.Disabled)),
            .. toRestore.Select(s => (s, ServiceControl.Manual))
        ];

        return (plan, protectedByChain);
    }
}
