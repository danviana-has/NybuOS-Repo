using System;
using Cosmos.Kernel.System.Network;

class Program
{
    static void Main()
    {
        var ip = new Address4(192, 168, 252, 100);
        var mask = new Address4(255, 255, 255, 0);
        var gw = new Address4(192, 168, 252, 2);
        
        var adapter = NetworkManager.GetAdapter(0);
        
        // Let's test IPConfig.Enable overloads
        IPConfig.Enable(adapter, ip, mask, gw);
    }
}
