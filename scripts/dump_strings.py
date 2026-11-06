with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    data = f.read()

# Locate '#Strings' stream
idx = data.find(b'#Strings\x00')
if idx != -1:
    print(f"Found #Strings header at {idx}")

# Or just split by \x00 in the metadata area
strings = [s.decode('utf-8', errors='ignore') for s in data.split(b'\x00') if len(s) >= 3]
print(f"Total strings: {len(strings)}")
interesting = [s for s in strings if any(k in s.lower() for k in ['partition', 'storage', 'vfs', 'format', 'device', 'fat', 'mbr', 'gpt', 'hdd', 'boot'])]
print("--- Interesting strings ---")
for s in sorted(set(interesting)):
    if any(k in s for k in ['Manager', 'Create', 'Add', 'Write', 'Init', 'Mount', 'Format', 'Block', 'Device', 'Disk']):
        print(s)
