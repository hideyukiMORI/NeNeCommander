using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeNeCommander.Complexity;

internal sealed class RecursionGraph
{
    private readonly Dictionary<ISymbol, MemberMetric> _members = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<MemberMetric, HashSet<MemberMetric>> _edges = [];

    internal int UnresolvedInvocations { get; private set; }

    internal void Mark(IReadOnlyList<MemberMetric> members)
    {
        foreach (MemberMetric member in members)
        {
            if (member.Symbol is not null && member.Symbol.PartialImplementationPart is null)
            {
                _members[Normalize(member.Symbol)] = member;
            }
            _edges[member] = [];
        }
        foreach (MemberMetric member in members) { AddEdges(member); }
        foreach (MemberMetric member in members) { member.Recursive = IsCyclic(member); }
    }

    private void AddEdges(MemberMetric member)
    {
        IEnumerable<InvocationExpressionSyntax> invocations = member.Node.DescendantNodes(
            node => ReferenceEquals(node, member.Node) || !CallableNodes.Contains(node)).OfType<InvocationExpressionSyntax>();
        foreach (InvocationExpressionSyntax invocation in invocations)
        {
            SymbolInfo info = member.Model.GetSymbolInfo(invocation);
            if (info.Symbol is IMethodSymbol method) { AddTarget(member, method); }
            else
            {
                UnresolvedInvocations++;
                foreach (IMethodSymbol candidate in info.CandidateSymbols.OfType<IMethodSymbol>()) { AddTarget(member, candidate); }
            }
        }
    }

    private void AddTarget(MemberMetric owner, IMethodSymbol target)
    {
        if (_members.TryGetValue(Normalize(target), out MemberMetric? called)) { _ = _edges[owner].Add(called); }
    }

    private bool IsCyclic(MemberMetric origin)
    {
        HashSet<MemberMetric> visited = [];
        Stack<MemberMetric> pending = new(_edges[origin]);
        while (pending.TryPop(out MemberMetric? current))
        {
            if (ReferenceEquals(origin, current)) { return true; }
            if (!visited.Add(current)) { continue; }
            foreach (MemberMetric next in _edges[current]) { pending.Push(next); }
        }
        return false;
    }

    private static IMethodSymbol Normalize(IMethodSymbol symbol)
    {
        return (symbol.PartialDefinitionPart ?? symbol.ReducedFrom ?? symbol).OriginalDefinition;
    }
}
