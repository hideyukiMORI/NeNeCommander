using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

internal static class NullGuardAssertions
{
    internal static void AssertNullGuard(Action action)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(action);
    }

    internal static BookmarkCategoryName Category(string value)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse(value)).Name;
    }

    internal static BookmarkDisplayName DisplayName(string value)
    {
        return Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(value)).Name;
    }

    internal static BookmarkPath BookmarkPath(string value)
    {
        return Assert.IsInstanceOfType<BookmarkPathAccepted>(
            Bookmarks.BookmarkPath.Parse(value)).Path;
    }

    internal static void AssertInternalConstructorNullGuard(Type type, object?[] arguments)
    {
        ConstructorInfo[] constructors = [.. type
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(constructor => constructor.GetParameters().Length == arguments.Length &&
                constructor.GetParameters().All(parameter => parameter.ParameterType != type))];
        Assert.HasCount(1, constructors);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructors[0].Invoke(arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    internal static void AssertInternalMethodNullGuard(Type type, string methodName, object? instance, object?[] arguments)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) ??
            throw new AssertFailedException("The internal method was not found.");
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    internal static void AssertConstructorNullGuard(ConstructorInfo constructor, object?[] arguments)
    {
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke(arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    internal static void AssertInstanceNullGuard(object instance, string methodName, object?[] arguments)
    {
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(instance.GetType(), methodName);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    internal static void AssertStaticNullGuard(Type type, string methodName, object?[] arguments)
    {
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(type, methodName);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    internal static MethodInfo GetSinglePublicStaticOrInstanceMethod(Type type, string methodName)
    {
        MethodInfo[] methods = [.. type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name.Equals(methodName, StringComparison.Ordinal))];
        Assert.HasCount(1, methods);
        return methods[0];
    }

    internal static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
