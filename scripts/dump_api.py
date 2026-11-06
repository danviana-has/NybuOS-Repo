import re

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    sys_dll = f.read()

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.hal\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.HAL.dll', 'rb') as f:
    hal_dll = f.read()

print("--- Cosmos.Kernel.System strings ---")
matches = [m.decode('latin1') for m in set(re.findall(rb'[A-Za-z0-9_]{5,}', sys_dll))]
for kw in ['StorageManager', 'Partition', 'Format', 'Vfs', 'Disk', 'Mbr']:
    found = [m for m in matches if kw.lower() in m.lower()]
    print(f"=== Keyword: {kw} ===")
    print(", ".join(sorted(found)[:30]))

print("\n--- Cosmos.Kernel.HAL strings ---")
hal_matches = [m.decode('latin1') for m in set(re.findall(rb'[A-Za-z0-9_]{5,}', hal_dll))]
for kw in ['BlockDevice', 'Ata', 'Disk', 'Storage', 'Ide']:
    found = [m for m in hal_matches if kw.lower() in m.lower()]
    print(f"=== Keyword: {kw} ===")
    print(", ".join(sorted(found)[:30]))
