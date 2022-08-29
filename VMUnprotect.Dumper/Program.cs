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

    // Try to load assembly and gather the ManifestModule
    Assembly? assembly = null;

    try
    {
        assembly = Assembly.LoadFile(target);
    }
    catch (BadImageFormatException)
    {
        Console.WriteLine("Target app probably has a different framework.");
        return;
    }

    var manifestModule = assembly.ManifestModule;

    // Quick load for .cctor search
    var module = ModuleDefinition.FromFile(target);

    // Resolve MethodBase of .cctor where vmp initializes itself
    var cctor =
        assembly.ManifestModule.ResolveMethod(module.TopLevelTypes[0].GetStaticConstructor()!.MetadataToken.ToInt32());

    // Get Module Base Address from loaded assembly
    var hInstance = Marshal.GetHINSTANCE(manifestModule);

    // Make sure static constructor exists
    if (cctor is not null)
    {
        // Force VMProtect to fix methods
        RuntimeHelpers.PrepareMethod(cctor.MethodHandle);

        // Load PEFile from disk
        var diskImage = PEFile.FromFile(target);

        // Get correct AddressOfEntrypoint and fix determining whether the image is a PE32 (32-bit) or a PE32+ (64-bit) image.
        var epFromDisk = diskImage.OptionalHeader.AddressOfEntrypoint;
        var magicDisk = diskImage.OptionalHeader.Magic;

        // Load decrypted PEFile from module base
        var runtimeImage = PEFile.FromModuleBaseAddress(hInstance, PEMappingMode.Mapped);

        var optionalHeader = runtimeImage.OptionalHeader;
        optionalHeader.Magic = magicDisk; // Fix the incorrect magic
        optionalHeader.AddressOfEntrypoint = epFromDisk; // Fix the incorrect AddressOfEntrypoint

        // Write fixed runtimeImage to disk
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