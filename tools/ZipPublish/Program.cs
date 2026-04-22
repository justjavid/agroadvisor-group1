using System.IO.Compression;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ZipPublish <sourceDir> <destZip>");
    return 1;
}

var src = Path.GetFullPath(args[0]);
var dst = Path.GetFullPath(args[1]);

if (File.Exists(dst)) File.Delete(dst);

// .NET 5+ writes entries with forward-slash separators per ZIP spec.
ZipFile.CreateFromDirectory(src, dst, CompressionLevel.Optimal, includeBaseDirectory: false);

Console.WriteLine($"Wrote {dst} ({new FileInfo(dst).Length} bytes)");
return 0;
