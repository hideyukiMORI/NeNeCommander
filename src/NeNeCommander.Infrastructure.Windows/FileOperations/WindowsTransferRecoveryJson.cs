using System;
using System.IO;
using System.Text;
using System.Text.Json;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns the exact version-one recovery JSON schema; decoding requires canonical byte-equivalent text.</summary>
internal static class WindowsTransferRecoveryJson
{
    internal static byte[] WriteInitial(WindowsTransferRecoveryMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        return Serialize(writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteString("phase", "PreparedWithTargetPossiblyChanging");
            writer.WriteString("operation", mapping.Names.Token.ToString("N"));
            WriteEntry(writer, "source", mapping.Files.Source);
            WriteEntry(writer, "target", mapping.Files.Target);
            WriteEntry(writer, "backup", mapping.Files.Backup);
            WriteEntry(writer, "prepared", mapping.Files.Prepared);
            WriteContent(writer, "original", mapping.Original);
            WriteContent(writer, "desired", mapping.Desired);
            writer.WriteEndObject();
        });
    }

    internal static byte[] WriteCompletion(WindowsTransferRecoveryMapping mapping, FileContentHash initialFrameHash)
    {
        return Serialize(writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteString("phase", "DesiredContentVerified");
            writer.WriteString("operation", mapping.Names.Token.ToString("N"));
            writer.WriteString("initialFrameSha256", initialFrameHash.Value);
            WriteText(writer, "targetObjectIdentity", mapping.Files.Target.ObjectIdentity.Value);
            WriteContent(writer, "desired", mapping.Desired);
            writer.WriteEndObject();
        });
    }

    internal static WindowsTransferRecoveryMapping ReadInitial(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
            WindowsTransferRecoveryMapping mapping = ReadMapping(document.RootElement);
            return !string.Equals(json, Encoding.UTF8.GetString(WriteInitial(mapping)), StringComparison.Ordinal)
                ? throw new InvalidDataException("The recovery JSON is not the exact canonical schema.") : mapping;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The recovery JSON is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException("The recovery JSON property type is invalid.", exception);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("A recovery JSON value is invalid.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("A recovery mapping invariant is invalid.", exception);
        }
    }

    private static WindowsTransferRecoveryMapping ReadMapping(JsonElement root)
    {
        Guid token = Guid.ParseExact(Text(root, "operation"), "N");
        WindowsTransferRecoveryFiles files = WindowsTransferRecoveryFiles.Create(ReadEntry(Property(root, "source")),
            ReadEntry(Property(root, "target")), ReadEntry(Property(root, "backup")), ReadEntry(Property(root, "prepared")));
        return WindowsTransferRecoveryMapping.Create(token, files, ReadContent(Property(root, "original")), ReadContent(Property(root, "desired")));
    }

    private static VerifiedEntrySnapshot ReadEntry(JsonElement entry)
    {
        string pathText = Text(entry, "path");
        if (FileSystemPath.Parse(pathText) is not PathParseSuccess { Path: WindowsLocalPath path } ||
            !string.Equals(path.CanonicalText, pathText, StringComparison.Ordinal))
        {
            throw new InvalidDataException("A recovery path cannot round-trip exactly.");
        }
        FileIdentity identity = FileIdentity.Parse(Text(entry, "identity")) is FileIdentityAccepted accepted
            ? accepted.Identity : throw new InvalidDataException("A recovery full identity is invalid.");
        return VerifiedEntrySnapshot.Create(path, identity, FileObjectIdentity.Create(Text(entry, "objectIdentity")));
    }

    private static FileContentEvidence ReadContent(JsonElement entry)
    {
        return FileContentEvidence.Create((KnownEntrySize)EntrySize.Create(Property(entry, "length").GetInt64()),
            FileContentHash.Create(Text(entry, "sha256")));
    }

    private static JsonElement Property(JsonElement entry, string name)
    {
        return entry.TryGetProperty(name, out JsonElement value) ? value
            : throw new InvalidDataException("A required recovery property is absent.");
    }

    private static string Text(JsonElement entry, string name)
    {
        string text = Property(entry, name).GetString() ?? throw new InvalidDataException("Recovery text cannot be null.");
        return text;
    }

    private static void WriteEntry(Utf8JsonWriter writer, string name, VerifiedEntrySnapshot entry)
    {
        writer.WriteStartObject(name);
        WriteText(writer, "path", entry.Path.CanonicalText);
        WriteText(writer, "identity", entry.Identity.Value);
        WriteText(writer, "objectIdentity", entry.ObjectIdentity.Value);
        writer.WriteEndObject();
    }

    private static void WriteContent(Utf8JsonWriter writer, string name, FileContentEvidence content)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("length", content.Size.Bytes);
        writer.WriteString("sha256", content.Hash.Value);
        writer.WriteEndObject();
    }

    private static void WriteText(Utf8JsonWriter writer, string name, string text)
    {
        WindowsTransferRecoveryFrame.RequireText(text);
        writer.WriteString(name, text);
    }

    private static byte[] Serialize(Action<Utf8JsonWriter> write)
    {
        using MemoryStream output = new();
        using (Utf8JsonWriter writer = new(output, new JsonWriterOptions { Indented = true }))
        {
            write(writer);
        }
        return output.ToArray();
    }
}
