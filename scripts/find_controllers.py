with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.hal\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.HAL.dll', 'rb') as f:
    dll = f.read()

import re
matches = [m.decode('latin1') for m in set(re.findall(rb'[A-Za-z0-9_]{3,}(?:Controller|Driver|Ata|Ahci|Nvme|Scsi|Ide|Sata)', dll))]
print("Storage drivers in HAL:")
for m in sorted(matches):
    print(m)
