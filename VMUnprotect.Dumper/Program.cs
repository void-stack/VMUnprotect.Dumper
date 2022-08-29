// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.IO;
using AsmResolver.PE.File;
using Sharprompt;

const string asciiArt = @"
_________                __                 
\_   ___ \  ____________/  |_  ____ ___  ___
/    \  \/ /  _ \_  __ \   __\/ __ \\  \/  /
\     \___(  <_> )  | \/|  | \  ___/ >    < 
 \______  /\____/|__|   |__|  \___  >__/\_ \
        \/                        \/      \/
                VMUnprotect.Dumper
          https://github.com/void-stack
            Credits: wwh1004, MrToms
";

Console.Title = "VMUnprotect.Dumper";
Console.WriteLine(asciiArt);

string? target;

if (args.Length > 0 && File.Exists(args[0]))
    target = Prompt.Input<string>("Enter file path", args[0]);
else
    target = Prompt.Input<string>("Enter file path");

if (File.Exists(target))
{
    var output = $"{Path.GetFileNameWithoutExtension(target)}-decrypted.exe";
    Assembly? assembly = null;

    try
    {
        assembly = Assembly.LoadFile(target);
    }
    catch (BadImageFormatException)
    {
        Console.WriteLine("Target app probably has a different framework.");
    }

    var manifestModule = assembly.ManifestModule;

    var module = ModuleDefinition.FromFile(target);

    var cctor =
        assembly.ManifestModule.ResolveMethod(module.TopLevelTypes[0].GetStaticConstructor()!.MetadataToken.ToInt32());

    var hInstance = Marshal.GetHINSTANCE(manifestModule);

    if (cctor != null)
    {
        RuntimeHelpers.PrepareMethod(cctor.MethodHandle);

        var diskImage = PEFile.FromFile(target);

        var epFromDisk = diskImage.OptionalHeader.AddressOfEntrypoint;
        var runtimeImage = PEFile.FromModuleBaseAddress(hInstance, PEMappingMode.Mapped);
        var optionalHeader = runtimeImage.OptionalHeader;

        optionalHeader.Magic = diskImage.OptionalHeader.Magic;
        optionalHeader.AddressOfEntrypoint = epFromDisk;

        using (var fs = File.Create(output))
        {
            runtimeImage.Write(new BinaryStreamWriter(fs));
        }

        Console.WriteLine($"Saved as: {Path.GetFullPath(output)}");
    }
    else
    {
        Console.WriteLine("Failed to prepare .cctor");
    }
}
else
{
    Console.WriteLine("File either doesn't exist or you didn't provide it (VMProtect.Dumper File.exe)");
}

Console.ReadKey();