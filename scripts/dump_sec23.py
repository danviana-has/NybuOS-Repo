with open(r'output-x64\NybuOS.iso', 'rb') as f:
    iso = f.read()

import struct

sec = iso[23*2048 : 24*2048]
idx = 0
while idx < len(sec):
    rec_len = sec[idx]
    if rec_len == 0:
        idx += 1
        continue
    extent_lba = struct.unpack_from('<I', sec, idx + 2)[0]
    data_len = struct.unpack_from('<I', sec, idx + 10)[0]
    name_len = sec[idx + 32]
    name = sec[idx + 33 : idx + 33 + name_len].decode('latin1', errors='ignore')
    print(f"ISO File: '{name}', extent_lba={extent_lba}, length={data_len}")
    idx += rec_len
