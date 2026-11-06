with open(r'output-x64\NybuOS.iso', 'rb') as f:
    iso = f.read()

print(f"ISO size: {len(iso)} bytes ({len(iso)//2048} 2K sectors, {len(iso)//512} 512B sectors)")

# Search for 'NybuOS.elf'
idx = 0
found = []
while True:
    pos = iso.find(b'NYBUOS.ELF', idx)
    if pos == -1: break
    found.append(pos)
    idx = pos + 10

print("Occurrences of NYBUOS.ELF in ISO:", [hex(p) for p in found])

# Look at ISO9660 Directory Record for NybuOS.elf
# In ISO9660, directory record has:
# Byte 0: length of record
# Byte 1: extended attribute record length
# Bytes 2..9: Location of extent (LBA, little-endian then big-endian)
# Bytes 10..17: Data length (size, little-endian then big-endian)
import struct
for p in found:
    # search backwards up to 33 bytes for start of record
    rec_start = p - 33
    rec_len = iso[rec_start]
    extent_lba = struct.unpack_from('<I', iso, rec_start + 2)[0]
    data_len = struct.unpack_from('<I', iso, rec_start + 10)[0]
    print(f"Record at {hex(rec_start)}: extent LBA={extent_lba} ({hex(extent_lba*2048)}), data_len={data_len}")
