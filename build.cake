#addin nuget:?package=Cake.Git

var gitRepository = "https://github.com/Furtif/POGOProtos-1.git";
var branch = EnvironmentVariable("POGOPROTOS_TAG") ?? "master";

var dirProtos = "./POGOProtos";
var dirTools = "./tools";
var dirSource = "./src";
var dirSourceCopy = "./srcCopy";

Information(branch);

Task("Clean").Does(() => {
    if (DirectoryExists(dirProtos)) {
        DeleteDirectory(dirProtos, true);
    }

    if (DirectoryExists(dirSourceCopy)) {
        DeleteDirectory(dirSourceCopy, true);
    }
});

Task("Copy").Does(() => {
    CopyDirectory(dirSource, dirSourceCopy);
});

Task("POGOProtos-Tools").Does(() => {
    NuGetInstall("Google.Protobuf.Tools", new NuGetInstallSettings {
        ExcludeVersion = true,
        OutputDirectory = dirTools,
        Version = "3.5.1"
    });
});

Task("POGOProtos-Clone").Does(() => {
    Information("Cloning branch '" + branch + "'...");

    StartProcess("git.exe", new ProcessSettings()
        .WithArguments(args => 
            args.Append("clone")
                .Append("--quiet")                   
                .Append("--branch")
                .AppendQuoted(branch)
                .Append(gitRepository)
                .Append("POGOProtos")));
});

Task("POGOProtos-Compile").Does(() => {
    StartProcess("python.exe", new ProcessSettings()
        .WithArguments(args => 
            args.AppendQuoted(System.IO.Path.GetFullPath(dirProtos + "/compile.py"))
                .Append("-p")
                .AppendQuoted(System.IO.Path.GetFullPath(dirTools + "/Google.Protobuf.Tools/tools/windows_x64/protoc.exe"))
                .Append("-o")
                .AppendQuoted(System.IO.Path.GetFullPath(dirProtos + "/out"))
                .Append("csharp")));
});

Task("POGOProtos-Move").Does(() => {
    CopyDirectory(dirProtos + "/out/POGOProtos", dirSourceCopy + "/POGOProtos.Core");
});

Task("Version").Does(() =>
{
    // Read version
    var version = System.IO.File.ReadAllText(dirProtos + "/.current-version");
    // Fix version
    version = System.Text.RegularExpressions.Regex.Replace(version, @"\s+", string.Empty);

    // Apply version
    var projectFile = dirSourceCopy + "/POGOProtos.Core/POGOProtos.Core.csproj";
    var updatedProjectFile = System.IO.File
        .ReadAllText(projectFile)
        .Replace("<Version>1.0.0-rc</Version>", "<Version>" + version + "</Version>");

    System.IO.File.WriteAllText(projectFile, updatedProjectFile);

    Information("Applied version '" + version + "' to '" + projectFile + "'.");
});

Task("Default")
  .IsDependentOn("Clean")
  .IsDependentOn("Copy")
  .IsDependentOn("POGOProtos-Tools")
  .IsDependentOn("POGOProtos-Clone")
  .IsDependentOn("POGOProtos-Compile")
  .IsDependentOn("POGOProtos-Move")
  .IsDependentOn("Version")
  .Does(() =>
{
  DotNetCoreRestore(dirSourceCopy + "/POGOProtos.Core");
  DotNetCorePack(dirSourceCopy + "/POGOProtos.Core", new DotNetCorePackSettings {
      Configuration = "Release"
  });
});

RunTarget("Default");
