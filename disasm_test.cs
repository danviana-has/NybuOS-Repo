
using System;
using System.IO;

public class ProgramGzip {
    public static void Main() {
        string text = File.ReadAllText(@"C:\Users\dvian\.nuget\packages\cosmos.build.common\3.0.95\tools\Limine\limine-bios-hdd.h");
        var matches = System.Text.RegularExpressions.Regex.Matches(text, "0x([0-9a-fA-F]{2})");
        byte[] bytes = new byte[matches.Count];
        for (int i = 0; i < matches.Count; i++) {
            bytes[i] = Convert.ToByte(matches[i].Groups[1].Value, 16);
        }

        // Look for 0x1f 0x8b (gzip) or other magic
        for (int i = 0; i < bytes.Length - 10; i++) {
            if (bytes[i] == 0x1F && bytes[i+1] == 0x8B) {
                Console.WriteLine("GZIP magic at 0x" + i.ToString("X"));
            }
        }
    }
}
