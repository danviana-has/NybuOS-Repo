$dllPath = "C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll"
$code = @"
using System;
using System.Reflection;

class Program {
    static void Main() {
        var asm = Assembly.LoadFrom(@"$dllPath");
        foreach(var t in asm.GetExportedTypes()) {
            if (t.Namespace != null && t.Namespace.Contains("Graphics")) {
                Console.WriteLine("Type: " + t.FullName);
                foreach(var m in t.GetMethods()) {
                    if (m.Name.Contains("Draw") || m.Name.Contains("Font") || m.Name.Contains("Text") || m.Name.Contains("String")) {
                        Console.WriteLine("   Method: " + m.ToString());
                    }
                }
            }
        }
    }
}
"@
Set-Content -Path "InspectNetCore.cs" -Value $code
dotnet new console -n InspectNetCoreApp -f net10.0 -o InspectNetCoreApp --force | Out-Null
Set-Content -Path "InspectNetCoreApp\Program.cs" -Value $code
dotnet run --project InspectNetCoreApp
