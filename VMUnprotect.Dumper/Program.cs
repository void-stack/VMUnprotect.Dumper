// See https://aka.ms/new-console-template for more information
using AsmResolver.IO;
using AsmResolver.PE.File;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

if (args.Length > 0 && File.Exists(args[0])) {
    var target = args[0];
    var output = $"{Path.GetFileNameWithoutExtension(target)}-decrypted.exe";
    var assembly = Assembly.LoadFile(target);
    var moduleHandle = assembly.ManifestModule.ModuleHandle;

    Console.WriteLine("[+] Decrypting methods");
    RuntimeHelpers.RunModuleConstructor(moduleHandle);
    var hInstanceFixed = Marshal.GetHINSTANCE(assembly.ManifestModule);

    Console.WriteLine("[+] Reading decrypted module");
    var decryptedPeFile = PEFile.FromModuleBaseAddress(hInstanceFixed);

    foreach (var section in decryptedPeFile.Sections)
        Console.WriteLine("[+] Sections: " + section.Name);

    Console.WriteLine("[+] Writing file");
    using (var fs = File.Create(output)) {
        decryptedPeFile.Write(new BinaryStreamWriter(fs));
    }

    Console.WriteLine("[+] Decrypted all methods!");
}
else 
    Console.WriteLine("File either doesn't exist or you didn't provide it (VMProtect.Dumper File.exe)");

Console.ReadKey();