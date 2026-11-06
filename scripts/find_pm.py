with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    data = f.read()

import re
pos = 0
while True:
    idx = data.find(b'PartitionManager', pos)
    if idx == -1: break
    print(f"Found PartitionManager at {idx}")
    print(data[idx-100:idx+200])
    pos = idx + 16
