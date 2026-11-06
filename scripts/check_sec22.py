with open(r'output-x64\NybuOS.iso', 'rb') as f:
    iso = f.read()

sec22 = iso[22*2048 : 23*2048]
print(f"Sector 22 contains NYBUOS: {'NYBUOS' in sec22.decode('latin1', errors='ignore')}")
