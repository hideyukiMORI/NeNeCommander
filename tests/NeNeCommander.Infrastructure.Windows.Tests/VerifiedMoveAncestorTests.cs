using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the external-ancestor rule of ADR-0069 as revised for Issue #388: every directory from the parent of
/// the operation root up to the provider root is matched by its object component and must stay a non-link
/// directory, on the source and the target side, during verification and during deletion. Unrelated sibling
/// activity in an ancestor no longer stops a transfer. The WSL source is mapped onto an owned NTFS tree; the
/// source ancestor is <c>wsl\outer</c>, the target ancestor is the destination <c>windows\outer</c>.
/// </summary>
[TestClass]
public sealed class VerifiedMoveAncestorTests
{
    private const string SourceAncestor = "wsl\\outer";
    private const string TargetAncestor = "windows\\outer";

    /// <summary>
    /// Rejects an ancestor replaced by another directory or turned into a link, retaining the source. The
    /// recorded tree keeps its full identities in every case, so the ancestor comparison alone decides. A
    /// replaced WSL ancestor is modelled by the 9P facts reporting another inode at the same path, because a
    /// real relocation would also change the moved root's change time and be refused by the tree check.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("source", "verify", "replaced")]
    [DataRow("source", "verify", "link")]
    [DataRow("source", "delete", "replaced")]
    [DataRow("source", "delete", "link")]
    [DataRow("target", "verify", "replaced")]
    [DataRow("target", "verify", "link")]
    [DataRow("target", "delete", "replaced")]
    [DataRow("target", "delete", "link")]
    public async Task ExecuteAsyncWhenAncestorIsReplacedOrLinkedRetainsSource(string side, string phase, string change)
    {
        bool replacedInode = false;
        using AncestorScenario scenario = AncestorScenario.Create(path => replacedInode &&
            path.EndsWith("\\" + SourceAncestor, StringComparison.OrdinalIgnoreCase));
        string ancestor = side == "source" ? SourceAncestor : TargetAncestor;

        FileOperationOutcome outcome = await scenario.MoveAsync(phase, root =>
        {
            if (change == "link")
            {
                Directory.Move(root.Resolve(ancestor), root.Resolve(ancestor + "-real"));
                _ = root.CreateJunction(ancestor, ancestor + "-real");
            }
            else if (side == "source")
            {
                replacedInode = true;
            }
            else
            {
                Directory.Move(root.Resolve(ancestor), root.Resolve(ancestor + "-old"));
                _ = root.CreateDirectory(ancestor);
                Directory.Move(root.Resolve(ancestor + "-old\\directory"), root.Resolve(ancestor + "\\directory"));
            }
        });

        Assert.IsTrue(scenario.Changed);
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.IsNotNull(outcome.Failure);
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
        Assert.AreEqual(phase == "delete", outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
        Assert.AreEqual(phase == "verify", File.Exists(scenario.Root.Resolve(SourceAncestor + "\\directory\\nested\\a.txt")));
        Assert.AreEqual("second", File.ReadAllText(scenario.Root.Resolve(SourceAncestor + "\\directory\\nested\\b.txt")));
        Assert.AreEqual("first", File.ReadAllText(scenario.Root.Resolve(TargetAncestor + "\\directory\\nested\\a.txt")));
    }

    /// <summary>
    /// Completes the move when an unrelated entry is created, removed or renamed directly in a source or target
    /// ancestor during verification or deletion. The ancestor's last-write time is also advanced explicitly, so
    /// the case would stop under the former full-identity rule regardless of the clock tick. The unrelated
    /// entries are never swept up.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("source", "verify", "create")]
    [DataRow("source", "verify", "remove")]
    [DataRow("source", "verify", "rename")]
    [DataRow("source", "delete", "create")]
    [DataRow("source", "delete", "remove")]
    [DataRow("source", "delete", "rename")]
    [DataRow("target", "verify", "create")]
    [DataRow("target", "verify", "remove")]
    [DataRow("target", "verify", "rename")]
    [DataRow("target", "delete", "create")]
    [DataRow("target", "delete", "remove")]
    [DataRow("target", "delete", "rename")]
    public async Task ExecuteAsyncWhenUnrelatedAncestorEntryChangesCompletesMove(string side, string phase, string change)
    {
        using AncestorScenario scenario = AncestorScenario.Create(_ => false);
        string ancestor = side == "source" ? SourceAncestor : TargetAncestor;

        FileOperationOutcome outcome = await scenario.MoveAsync(phase, root =>
        {
            DateTime modified = Directory.GetLastWriteTimeUtc(root.Resolve(ancestor));
            if (change == "create")
            {
                _ = root.WriteFile(ancestor + "\\late.txt", "late");
            }
            else if (change == "remove")
            {
                File.Delete(root.Resolve(ancestor + "\\unrelated.txt"));
            }
            else
            {
                File.Move(root.Resolve(ancestor + "\\unrelated.txt"), root.Resolve(ancestor + "\\renamed.txt"));
            }
            Directory.SetLastWriteTimeUtc(root.Resolve(ancestor), modified.AddSeconds(1));
        });

        Assert.IsTrue(scenario.Changed);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsTrue(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
        Assert.IsFalse(Directory.Exists(scenario.Root.Resolve(SourceAncestor + "\\directory")));
        Assert.AreEqual("first", File.ReadAllText(scenario.Root.Resolve(TargetAncestor + "\\directory\\nested\\a.txt")));
        Assert.AreEqual("second", File.ReadAllText(scenario.Root.Resolve(TargetAncestor + "\\directory\\nested\\b.txt")));
        Assert.AreEqual(change == "create", File.Exists(scenario.Root.Resolve(ancestor + "\\late.txt")));
        Assert.AreEqual(change == "rename", File.Exists(scenario.Root.Resolve(ancestor + "\\renamed.txt")));
        Assert.AreEqual(change == "create", File.Exists(scenario.Root.Resolve(ancestor + "\\unrelated.txt")));
    }

    /// <summary>Owns one WSL-to-Windows move whose late change runs at a chosen point of verification or deletion.</summary>
    private sealed class AncestorScenario : IDisposable
    {
        private readonly WslToWindowsTransferFixture _fixture;
        private Action<string> _inspect = _ => { };

        private AncestorScenario(Func<string, bool> reportsOtherInode)
        {
            _fixture = WslToWindowsTransferFixture.Create(path =>
            {
                _inspect(path);
                WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
                return reportsOtherInode(path)
                    ? new WslHandleFacts(facts.Inode + 1, facts.Attributes, facts.ReparseTag, new WslRewriteFacts(
                        facts.EndOfFile, facts.NumberOfLinks, facts.LastWriteFileTimeUtc, facts.ChangeFileTimeUtc))
                    : facts;
            });
        }

        internal TestOwnedTemporaryRoot Root => _fixture.Root;

        internal bool Changed { get; private set; }

        /// <summary>Creates the scenario; <paramref name="reportsOtherInode"/> models a replaced WSL directory.</summary>
        internal static AncestorScenario Create(Func<string, bool> reportsOtherInode)
        {
            AncestorScenario scenario = new(reportsOtherInode);
            TestOwnedTemporaryRoot root = scenario.Root;
            _ = root.CreateDirectory(SourceAncestor + "\\directory\\nested");
            _ = root.WriteFile(SourceAncestor + "\\directory\\nested\\a.txt", "first");
            _ = root.WriteFile(SourceAncestor + "\\directory\\nested\\b.txt", "second");
            _ = root.WriteFile(SourceAncestor + "\\unrelated.txt", "unrelated");
            _ = root.CreateDirectory(TargetAncestor);
            _ = root.WriteFile(TargetAncestor + "\\unrelated.txt", "unrelated");
            return scenario;
        }

        /// <summary>
        /// Moves <c>/owned/outer/directory</c> into <c>windows\outer</c>. In the verify phase the change runs when
        /// verification first observes a recorded leaf, after both ancestor chains were captured; in the delete
        /// phase it runs at the first observation after the first recorded leaf was deleted.
        /// </summary>
        internal async Task<FileOperationOutcome> MoveAsync(string phase, Action<TestOwnedTemporaryRoot> change)
        {
            string firstLeaf = Root.Resolve(SourceAncestor + "\\directory\\nested\\a.txt");
            void Arm(Func<string, bool> due)
            {
                _inspect = path =>
                {
                    if (!Changed && due(path))
                    {
                        Changed = true;
                        change(Root);
                    }
                };
            }
            StepHookPort port = new(_fixture.Router)
            {
                AfterCopy = _ =>
                {
                    if (phase == "verify")
                    {
                        Arm(path => path.EndsWith("\\a.txt", StringComparison.Ordinal));
                    }
                    return Task.CompletedTask;
                },
                AfterVerify = _ =>
                {
                    if (phase == "delete")
                    {
                        Arm(path => !File.Exists(firstLeaf) && path.EndsWith("\\nested", StringComparison.Ordinal));
                    }
                    return Task.CompletedTask;
                },
            };
            using FileOperationGateway gateway = new(port);
            FileSystemPath destination = Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(Root.Resolve(TargetAncestor))).Path;
            MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                MoveRequest.Create([WslToWindowsTransferFixture.Source("outer\\directory")], destination)).Request);
            return await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
