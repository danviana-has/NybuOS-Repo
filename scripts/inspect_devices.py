with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.hal\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.HAL.dll', 'rb') as f:
    hal = f.read()

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    sys = f.read()

import re
hal_types = [m.decode('latin1') for m in set(re.findall(rb'[A-Za-z0-9_]{3,}Device', hal))]
sys_types = [m.decode('latin1') for m in set(re.findall(rb'[A-Za-z0-9_]{3,}Device', sys))]

print("HAL Device types:", sorted(hal_types))
print("SYS Device types:", sorted(sys_types))
