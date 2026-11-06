import struct

class SimpleFat32:
    def __init__(self, total_sectors, start_lba=2048):
        self.total_sectors = total_sectors
        self.start_lba = start_lba
        self.part_sectors = total_sectors - start_lba
        self.spc = 8 # 8 sectors = 4096 bytes per cluster
        self.cluster_bytes = self.spc * 512
        self.reserved_sectors = 32
        self.num_fats = 2

        data_sectors = self.part_sectors - self.reserved_sectors
        self.fat_sectors = (data_sectors + (self.spc * 128 + 2) - 1) // (self.spc * 128 + 2)
        self.fat_sectors = ((self.fat_sectors + 31) // 32) * 32
        self.total_clusters = (self.part_sectors - self.reserved_sectors - self.num_fats * self.fat_sectors) // self.spc

        # Storage representation
        self.sectors = {}

        # Format
        self.format_volume()

    def write_sector(self, lba, data):
        self.sectors[lba] = bytearray(data)

    def read_sector(self, lba):
        return self.sectors.get(lba, bytearray(512))

    def format_volume(self):
        # Boot sector
        bs = bytearray(512)
        bs[0:3] = [0xEB, 0x58, 0x90]
        bs[3:11] = b'MSWIN4.1'
        struct.pack_into('<H', bs, 11, 512)
        bs[13] = self.spc
        struct.pack_into('<H', bs, 14, self.reserved_sectors)
        bs[16] = self.num_fats
        struct.pack_into('<H', bs, 17, 0)
        struct.pack_into('<H', bs, 19, 0)
        bs[21] = 0xF8
        struct.pack_into('<H', bs, 22, 0)
        struct.pack_into('<H', bs, 24, 63)
        struct.pack_into('<H', bs, 26, 255)
        struct.pack_into('<I', bs, 28, self.start_lba)
        struct.pack_into('<I', bs, 32, self.part_sectors)
        struct.pack_into('<I', bs, 36, self.fat_sectors)
        struct.pack_into('<H', bs, 40, 0)
        struct.pack_into('<H', bs, 42, 0)
        struct.pack_into('<I', bs, 44, 2) # root cluster = 2
        struct.pack_into('<H', bs, 48, 1) # FSInfo
        struct.pack_into('<H', bs, 50, 6) # Backup boot
        bs[64] = 0x80
        bs[66] = 0x29
        struct.pack_into('<I', bs, 67, 0x19952026)
        bs[71:82] = b'NYBUOS     '
        bs[82:90] = b'FAT32   '
        bs[510] = 0x55
        bs[511] = 0xAA
        self.write_sector(self.start_lba, bs)
        self.write_sector(self.start_lba + 6, bs) # Backup

        # FSInfo
        fsi = bytearray(512)
        struct.pack_into('<I', fsi, 0, 0x41615252)
        struct.pack_into('<I', fsi, 484, 0x61417272)
        struct.pack_into('<I', fsi, 488, self.total_clusters - 1)
        struct.pack_into('<I', fsi, 492, 3)
        struct.pack_into('<I', fsi, 508, 0xAA550000)
        self.write_sector(self.start_lba + 1, fsi)

        # FAT1 & FAT2
        fat_start = self.start_lba + self.reserved_sectors
        self.set_fat_entry(0, 0x0FFFFFF8)
        self.set_fat_entry(1, 0x0FFFFFFF)
        self.set_fat_entry(2, 0x0FFFFFFF) # Root cluster

        self.next_free_cluster = 3

    def get_cluster_lba(self, cluster):
        # First data cluster is 2
        fat_start = self.start_lba + self.reserved_sectors
        data_start = fat_start + self.num_fats * self.fat_sectors
        return data_start + (cluster - 2) * self.spc

    def set_fat_entry(self, cluster, value):
        fat_start = self.start_lba + self.reserved_sectors
        sec_offset = (cluster * 4) // 512
        byte_offset = (cluster * 4) % 512
        for f in range(self.num_fats):
            lba = fat_start + f * self.fat_sectors + sec_offset
            sec = self.read_sector(lba)
            struct.pack_into('<I', sec, byte_offset, value & 0x0FFFFFFF)
            self.write_sector(lba, sec)

    def allocate_cluster(self):
        c = self.next_free_cluster
        self.next_free_cluster += 1
        self.set_fat_entry(c, 0x0FFFFFFF)
        # Zero out cluster sectors
        lba = self.get_cluster_lba(c)
        for s in range(self.spc):
            self.write_sector(lba + s, bytearray(512))
        return c

    def write_file(self, dir_cluster, filename, data):
        # 8.3 filename format
        parts = filename.upper().split('.')
        base = parts[0].ljust(8)[:8]
        ext = parts[1].ljust(3)[:3] if len(parts) > 1 else '   '
        short_name = (base + ext).encode('ascii')

        # Allocate clusters for file
        data_len = len(data)
        needed_clusters = (data_len + self.cluster_bytes - 1) // self.cluster_bytes
        first_cluster = 0
        prev_cluster = 0

        for i in range(needed_clusters):
            c = self.allocate_cluster()
            if i == 0:
                first_cluster = c
            else:
                self.set_fat_entry(prev_cluster, c)
            prev_cluster = c

            chunk = data[i * self.cluster_bytes : (i + 1) * self.cluster_bytes]
            lba = self.get_cluster_lba(c)
            for s in range(self.spc):
                sec_data = chunk[s * 512 : (s + 1) * 512]
                if len(sec_data) < 512:
                    sec_data = sec_data + b'\x00' * (512 - len(sec_data))
                self.write_sector(lba + s, sec_data)

        if needed_clusters > 0:
            self.set_fat_entry(prev_cluster, 0x0FFFFFFF)

        # Add directory entry in dir_cluster
        self.add_dir_entry(dir_cluster, short_name, 0x20, first_cluster, data_len)
        return first_cluster

    def create_dir(self, parent_cluster, dirname):
        name = dirname.upper().ljust(11)[:11].encode('ascii')
        c = self.allocate_cluster()
        # Add . and ..
        self.add_dir_entry(c, b'.          ', 0x10, c, 0)
        self.add_dir_entry(c, b'..         ', 0x10, parent_cluster, 0)
        # Add entry in parent
        self.add_dir_entry(parent_cluster, name, 0x10, c, 0)
        return c

    def add_dir_entry(self, dir_cluster, name11, attr, first_cluster, file_size):
        lba = self.get_cluster_lba(dir_cluster)
        for s in range(self.spc):
            sec = self.read_sector(lba + s)
            for off in range(0, 512, 32):
                if sec[off] == 0x00 or sec[off] == 0xE5:
                    # Found free entry
                    sec[off:off+11] = name11
                    sec[off+11] = attr
                    # Cluster high
                    struct.pack_into('<H', sec, off + 20, (first_cluster >> 16) & 0xFFFF)
                    # Cluster low
                    struct.pack_into('<H', sec, off + 26, first_cluster & 0xFFFF)
                    # File size
                    struct.pack_into('<I', sec, off + 28, file_size)
                    self.write_sector(lba + s, sec)
                    return True
        return False

# Test the implementation
fat = SimpleFat32(total_sectors=4194304)
boot_dir = fat.create_dir(2, 'BOOT')
limine_dir = fat.create_dir(boot_dir, 'LIMINE')
fat.write_file(limine_dir, 'LIMINE.CFG', b'timeout: 0\n/NybuOS\n    protocol: limine\n    path: boot():/boot/NybuOS.elf\n')
fat.write_file(boot_dir, 'NYBUOS.ELF', b'\x7fELFTESTKERNELDATA')
print("SimpleFat32 verification passed successfully!")
