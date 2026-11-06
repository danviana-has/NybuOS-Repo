with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    data = f.read()

import re
idx = 0
while True:
    pos = data.find(b'BlockDeviceEntry', idx)
    if pos == -1: break
    print(f"At {pos}: {data[pos-50:pos+150]}")
    idx = pos + 16
