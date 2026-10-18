using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace NeNeCommander.RecordClosure;

/// <summary>Proves the checker accepts closure, rejects reopening and distinguishes an unrelated compile error.</summary>
internal static class RecordClosureSelfTest
{
    private const string ClosedSource = """
        using System;
        [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NonFriend.FriendProbe")]
        public abstract record Root
        {
            private protected abstract void RestrictDerivation();
            private Root(int payload) { Payload = payload; }
            public int Payload { get; }
            public abstract string Description { get; }
            public static Root Create(int payload) { return new Case(payload); }
            public sealed record Case : Root
            {
                internal Case(int payload) : base(payload) { }
                public override string Description => "known";
                private protected override void RestrictDerivation() { }
            }
        }
        public abstract record PrivateRoot
        {
            private protected abstract void RestrictDerivation();
            private PrivateRoot() { }
            public static PrivateRoot Known { get; } = new Case();
            private sealed record Case : PrivateRoot
            {
                private protected override void RestrictDerivation() { }
            }
        }
        public static class CopyProof
        {
            public static void Check()
            {
                Root original = Root.Create(7);
                Root copy = original with { };
                Root.Case concrete = (Root.Case)original;
                Root.Case concreteCopy = concrete with { };
                PrivateRoot nested = PrivateRoot.Known;
                PrivateRoot nestedCopy = nested with { };
                if (ReferenceEquals(original, copy) || original != copy || original.GetHashCode() != copy.GetHashCode()
                    || copy.Payload != 7 || copy.Description != "known" || original == Root.Create(8)
                    || ReferenceEquals(concrete, concreteCopy) || concrete != concreteCopy || concreteCopy.Payload != 7
                    || ReferenceEquals(nested, nestedCopy) || nested != nestedCopy || nested.GetHashCode() != nestedCopy.GetHashCode())
                {
                    throw new InvalidOperationException("Known record copy/equality/payload changed.");
                }
            }
        }
        """;

    internal static void Run(RecordClosureCompiler compiler, string output)
    {
        MetadataReference closed = compiler.Accept("FixtureClosed", ClosedSource);
        RecordClosureCompiler closedCompiler = compiler.With(closed);
        foreach (INamedTypeSymbol root in closedCompiler.Types(closed).Where(type => type.IsAbstract && type.IsRecord))
        {
            RecordClosureProbe.Verify(root, closedCompiler);
        }
        Assembly fixture = Assembly.Load(File.ReadAllBytes(Path.Combine(output, "FixtureClosed.dll")));
        MethodInfo check = fixture.GetType("CopyProof", true)?.GetMethod("Check") ?? throw new InvalidOperationException("Copy proof is absent.");
        _ = check.Invoke(null, null);
        _ = closedCompiler.Accept("FriendProbe", FriendSource());
        AssertReopened(compiler, "FixtureReopened", ClosedSource
            .Replace("private protected abstract void RestrictDerivation();", string.Empty, StringComparison.Ordinal)
            .Replace("private protected override void RestrictDerivation() { }", string.Empty, StringComparison.Ordinal));
        AssertReopened(compiler, "FixturePublicAbstract", ClosedSource.Replace("private protected", "public", StringComparison.Ordinal));
        AssertReopened(compiler, "FixtureProtectedAbstract", ClosedSource.Replace("private protected", "protected", StringComparison.Ordinal));
        AssertWrongDiagnostic(closedCompiler);
        AssertNoStaleBinary(closedCompiler);
        File.WriteAllText(Path.Combine(output, "result.json"), "{\"closedRoots\":2,\"reopenedHierarchiesRejected\":3,\"unrelatedDiagnosticRejected\":true,\"friendAccepted\":true,\"knownCopyRuntimePassed\":true,\"staleBinaryRemoved\":true}");
        Console.WriteLine("[CS-002] Self-proof passed: two closed roots, three rejected reopenings, wrong-diagnostic refusal, friend scope, runtime copy/equality/payload and stale-binary removal.");
    }

    private static void AssertReopened(RecordClosureCompiler compiler, string label, string source)
    {
        MetadataReference open = compiler.Accept(label, source);
        RecordClosureCompiler openCompiler = compiler.With(open);
        INamedTypeSymbol root = openCompiler.Types(open).Single(type => type.Name == "Root");
        try
        {
            RecordClosureProbe.Verify(root, openCompiler);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("external-copy unexpectedly accepted", StringComparison.Ordinal))
        {
            return;
        }
        throw new InvalidOperationException("[CS-002] The checker missed the reopened hierarchy: " + label);
    }

    private static void AssertWrongDiagnostic(RecordClosureCompiler compiler)
    {
        try
        {
            compiler.Reject("WrongReference", "public sealed record Probe : MissingRoot;", ["CS0534"]);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("wrong reason", StringComparison.Ordinal))
        {
            return;
        }
        throw new InvalidOperationException("[CS-002] An unrelated compiler failure was mistaken for closure.");
    }

    private static void AssertNoStaleBinary(RecordClosureCompiler compiler)
    {
        MetadataReference accepted = compiler.Accept("Recompiled", "public sealed record Probe;");
        string binary = accepted.Display ?? throw new InvalidOperationException("Compiled fixture path is absent.");
        compiler.Reject("Recompiled", """
            public sealed record Probe : Root
            {
                public Probe(Root original) : base(original) { }
                public override string Description => "external";
            }
            """, ["CS0534"]);
        if (File.Exists(binary))
        {
            throw new InvalidOperationException("[CS-002] A rejected recompilation retained its earlier successful binary.");
        }
    }

    private static string FriendSource()
    {
        return """
            public sealed record Friend : Root
            {
                public Friend(Root original) : base(original) { }
                public override string Description => "friend";
                private protected override void RestrictDerivation() { }
            }
            """;
    }
}
