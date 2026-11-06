with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    data = f.read()

import re
strings = re.findall(rb'[A-Za-z0-9_]{3,}', data)
candidates = set()
for s in strings:
    text = s.decode('latin1')
    if any(k in text.lower() for k in ['storage', 'partition', 'device', 'format', 'fat', 'vfs']):
        candidates.add(text)

print(f"Candidates found: {len(candidates)}")
for c in sorted(candidates):
    if any(m in c.lower() for m in ['storagemanager', 'devices', 'partitionmanager', 'format', 'mount', 'vfs']):
        print(c)
