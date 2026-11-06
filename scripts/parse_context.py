with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    data = f.read()

import re
# Look for sequences that are ASCII strings near StorageManager and PartitionManager
lines = []
for m in re.finditer(rb'(StorageManager|PartitionManager|VfsManager|FatFormatter)', data):
    start = max(0, m.start() - 200)
    end = min(len(data), m.end() + 200)
    chunk = data[start:end]
    # find words in chunk
    words = [w.decode('latin1') for w in re.findall(rb'[A-Za-z0-9_]{3,}', chunk)]
    lines.append(" ".join(words))

for l in lines[:15]:
    print(l)
