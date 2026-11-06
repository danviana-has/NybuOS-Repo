import pefile

pe = pefile.PE(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.hal\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.HAL.dll')

# The CLI metadata header is located via the IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR (entry 14)
cli_dir = pe.OPTIONAL_HEADER.DATA_DIRECTORY[14]
print(f"CLI Dir RVA: {hex(cli_dir.VirtualAddress)}, Size: {cli_dir.Size}")

# Let's extract all strings in the DLL that look like method names
with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.hal\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.HAL.dll', 'rb') as f:
    data = f.read()

import re
# Find all method-like or property-like strings
strings = re.findall(rb'[A-Za-z0-9_]{3,}', data)
candidates = set()
for s in strings:
    text = s.decode('latin1')
    if any(k in text.lower() for k in ['block', 'write', 'read', 'sector', 'device', 'storage', 'ata', 'drive']):
        candidates.add(text)

print(f"Candidates found: {len(candidates)}")
for c in sorted(candidates):
    print(c)
