import re

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    dll = f.read()

# Search for classes implementing IVfsFileSystemType or registered filesystems
types = [m.decode('latin1') for m in set(re.findall(rb'[A-Za-z0-9_]{3,}(?:FileSystem|Fs|Vfs)', dll))]
print("FileSystem types:")
for t in sorted(types):
    print(t)
