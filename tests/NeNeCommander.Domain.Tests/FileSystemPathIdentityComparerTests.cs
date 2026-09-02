using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Domain.Tests;

/// <summary>Proves path equality follows provider-native identity rules.</summary>
[TestClass]
public sealed class FileSystemPathIdentityComparerTests
{
    /// <summary>Proves Windows local and UNC identities ignore casing without crossing providers.</summary>
    [TestMethod]
    public void EqualsWhenWindowsCasingVariesUsesCaseInsensitiveProviderIdentity()
    {
        FileSystemPath local = ParsePath("C:\\Work\\File.txt");
        FileSystemPath localCaseVariant = ParsePath("c:\\work\\file.TXT");
        FileSystemPath localOther = ParsePath("C:\\Work\\Other.txt");
        FileSystemPath unc = ParsePath("\\\\Server\\Share\\Folder");
        FileSystemPath uncCaseVariant = ParsePath("\\\\server\\share\\folder");
        FileSystemPath uncOther = ParsePath("\\\\server\\share\\other");
        FileSystemPathIdentityComparer comparer = FileSystemPathIdentityComparer.Instance;

        Assert.IsTrue(comparer.Equals(local, local));
        Assert.IsTrue(comparer.Equals(local, localCaseVariant));
        Assert.IsFalse(comparer.Equals(local, localOther));
        Assert.AreEqual(comparer.GetHashCode(local), comparer.GetHashCode(localCaseVariant));
        Assert.IsTrue(comparer.Equals(unc, uncCaseVariant));
        Assert.IsFalse(comparer.Equals(unc, uncOther));
        Assert.AreEqual(comparer.GetHashCode(unc), comparer.GetHashCode(uncCaseVariant));
        Assert.IsFalse(comparer.Equals(local, unc));
        Assert.IsFalse(comparer.Equals(unc, local));
    }

    /// <summary>Proves WSL distribution identity ignores casing while Linux paths preserve it.</summary>
    [TestMethod]
    public void EqualsWhenWslIdentityVariesUsesMixedProviderCasingRules()
    {
        FileSystemPath path = ParsePath("\\\\wsl.localhost\\Ubuntu\\home\\xi\\Case");
        FileSystemPath distroCaseVariant = ParsePath("\\\\wsl.localhost\\ubuntu\\home\\xi\\Case");
        FileSystemPath pathCaseVariant = ParsePath("\\\\wsl.localhost\\Ubuntu\\home\\xi\\case");
        FileSystemPath otherDistro = ParsePath("\\\\wsl.localhost\\Debian\\home\\xi\\Case");
        FileSystemPathIdentityComparer comparer = FileSystemPathIdentityComparer.Instance;

        Assert.IsTrue(comparer.Equals(path, distroCaseVariant));
        Assert.AreEqual(comparer.GetHashCode(path), comparer.GetHashCode(distroCaseVariant));
        Assert.IsFalse(comparer.Equals(path, pathCaseVariant));
        Assert.IsFalse(comparer.Equals(path, otherDistro));
    }

    /// <summary>Proves absent identities compare safely and cannot be hashed.</summary>
    [TestMethod]
    public void EqualsWhenIdentityIsAbsentUsesCollectionComparerContract()
    {
        FileSystemPath path = ParsePath("C:\\work");
        FileSystemPathIdentityComparer comparer = FileSystemPathIdentityComparer.Instance;

        Assert.IsTrue(comparer.Equals(null, null));
        Assert.IsFalse(comparer.Equals(null, path));
        Assert.IsFalse(comparer.Equals(path, null));

        MethodInfo method = typeof(FileSystemPathIdentityComparer).GetMethod(
            nameof(FileSystemPathIdentityComparer.GetHashCode),
            [typeof(FileSystemPath)]) ?? throw new AssertFailedException("The hash method was not found.");
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(comparer, [null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
