using System;
using System.Diagnostics;
using Cosmos.Build.API.Attributes;

namespace NybuOS.Core;

[Plug(typeof(Debugger))]
public static class DebuggerPlug
{
    [PlugMember]
    public static void Break()
    {
    }
}
