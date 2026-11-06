import re

with open(r'obj\Release\net10.0\linux-x64\cosmos\native\NybuOS.map.xml', 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read()

methods = re.findall(r'<MethodCode Name="([^"]+)"', text)
print(f"Total compiled methods: {len(methods)}")

storage_methods = [m for m in methods if any(k in m for k in ['StorageManager', 'PartitionManager', 'Partition', 'IBlockDevice', 'VfsManager', 'Fat'])]
print(f"Matched methods: {len(storage_methods)}")
for m in sorted(storage_methods):
    print(m)
