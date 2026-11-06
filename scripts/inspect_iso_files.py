with open(r'output-x64\NybuOS.iso', 'rb') as f:
    iso = f.read()

import struct

for name in [b'LIMINE_B.SYS', b'LIMINE-B.SYS', b'LIMINE.SYS', b'LIMINE.CON', b'BOOTX64.EFI']:
    idx = 0
    while True:
        pos = iso.find(name, idx)
        if pos == -1: break
        # search backwards for record length
        for rec_start in range(pos - 40, pos):
            rec_len = iso[rec_start]
            if rec_len >= 33 and rec_start + rec_len <= len(iso):
                extent_lba = struct.unpack_from('<I', iso, rec_start + 2)[0]
                data_len = struct.unpack_from('<I', iso, rec_start + 10)[0]
                if 100 < data_len < 20000000 and 16 < extent_lba < 100000:
                    print(f"File {name.decode('ascii')}: extent LBA={extent_lba} ({hex(extent_lba*2048)}), len={data_len}")
                    break
        idx = pos + len(name)
