import pefile

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    dll = f.read()

import re
# Look for methods in VfsManager
vfs_methods = []
for m in re.finditer(rb'VfsManager\x00', dll):
    # around VfsManager
    start = max(0, m.start() - 300)
    end = min(len(dll), m.end() + 300)
    words = [w.decode('latin1') for w in re.findall(rb'[A-Za-z0-9_]{3,}', dll[start:end])]
    vfs_methods.extend(words)

print("Words around VfsManager:")
for w in sorted(set(vfs_methods)):
    print(w)
