using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeNeCommander.Complexity;

internal sealed class SourceFile
{
    internal SourceFile(string project, string path, string text)
    {
        Project = project;
        Path = path;
        Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        Tree = CSharpSyntaxTree.ParseText(text,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp14), path);
        Validate();
    }

    internal string Project { get; }
    internal string Path { get; }
    internal string Hash { get; }
    internal SyntaxTree Tree { get; }

    private void Validate()
    {
        Diagnostic? error = Tree.GetDiagnostics().FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (error is not null)
        {
            throw new InvalidOperationException("[CS-013 input] Invalid C#: " + error);
        }
        SyntaxNode root = Tree.GetRoot();
        if (root.DescendantTrivia().Any(trivia => trivia.IsDirective &&
            trivia.GetStructure() is ConditionalDirectiveTriviaSyntax or DefineDirectiveTriviaSyntax or UndefDirectiveTriviaSyntax or ElseDirectiveTriviaSyntax or EndIfDirectiveTriviaSyntax))
        {
            throw new InvalidOperationException("[CS-013 input] Conditional compilation is unsupported: " + Path);
        }
        if (root.DescendantNodes().Any(node => node is GlobalStatementSyntax ||
            node is TypeDeclarationSyntax type && type.ParameterList is not null))
        {
            throw new InvalidOperationException("[CS-013 input] Primary constructors/top-level statements are unsupported: " + Path);
        }
    }
}
