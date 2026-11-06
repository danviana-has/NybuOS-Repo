with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    dll = f.read()

import re
# Find references to FatFormatOptions
for m in re.finditer(rb'FatFormatOptions', dll):
    start = max(0, m.start() - 100)
    end = min(len(dll), m.end() + 100)
    print(dll[start:end])
