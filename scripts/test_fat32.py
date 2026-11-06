import struct

def test_mbr_and_fat32():
    # 2GB disk = 2 * 1024 * 1024 * 1024 = 2,147,483,648 bytes = 4,194,304 sectors of 512 bytes
    total_sectors = 4194304
    part_start_lba = 2048 # 1MB offset
    part_sectors = total_sectors - part_start_lba

    # MBR Sector (512 bytes)
    mbr = bytearray(512)
    # Stage 1 code placeholder (first 440 bytes)
    mbr[0] = 0xEB
    mbr[1] = 0x3C
    mbr[2] = 0x90
    mbr[3:9] = b'LIMINE'

    # Offset 0x1A4 (420): stage2_loc = 512 (64-bit int)
    struct.pack_into('<Q', mbr, 0x1A4, 512)

    # Offset 440: Disk signature (4 bytes)
    struct.pack_into('<I', mbr, 440, 0x12345678)

    # Offset 446: Partition 1 entry (16 bytes)
    mbr[446] = 0x80 # Bootable
    mbr[447] = 0x00 # Start CHS head
    mbr[448] = 0x02 # Start CHS sector (2)
    mbr[449] = 0x00 # Start CHS cylinder (0)
    mbr[450] = 0x0C # Partition Type: FAT32 LBA
    mbr[451] = 0xFE # End CHS head
    mbr[452] = 0xFF # End CHS sector
    mbr[453] = 0xFF # End CHS cylinder
    struct.pack_into('<I', mbr, 454, part_start_lba) # Start LBA: 2048
    struct.pack_into('<I', mbr, 458, part_sectors)    # Total sectors

    # Boot signature at 510..511
    mbr[510] = 0x55
    mbr[511] = 0xAA

    print(f"MBR constructed: Bootable={hex(mbr[446])}, Type={hex(mbr[450])}, StartLBA={part_start_lba}, Sectors={part_sectors}")

    # FAT32 Boot Sector (Sector 2048)
    spc = 8 # 8 sectors per cluster = 4096 bytes per cluster
    reserved_sectors = 32
    num_fats = 2
    # Number of clusters
    data_sectors = part_sectors - reserved_sectors
    # Each FAT entry is 4 bytes. In one sector (512 bytes), there are 128 FAT entries.
    # We solve: fat_sectors * 128 >= (part_sectors - reserved_sectors - 2 * fat_sectors) / spc
    # spc * 128 * fat_sectors + 2 * fat_sectors >= part_sectors - reserved_sectors
    # (spc * 128 + 2) * fat_sectors >= data_sectors
    fat_sectors = (data_sectors + (spc * 128 + 2) - 1) // (spc * 128 + 2)
    # round up fat_sectors to a clean multiple
    fat_sectors = ((fat_sectors + 31) // 32) * 32

    bs = bytearray(512)
    bs[0] = 0xEB
    bs[1] = 0x58
    bs[2] = 0x90
    bs[3:11] = b'MSWIN4.1'
    struct.pack_into('<H', bs, 11, 512) # bytes per sector
    bs[13] = spc # sectors per cluster
    struct.pack_into('<H', bs, 14, reserved_sectors) # reserved sectors
    bs[16] = num_fats # number of FATs
    struct.pack_into('<H', bs, 17, 0) # root entries (0 for FAT32)
    struct.pack_into('<H', bs, 19, 0) # total sectors 16 (0 for FAT32)
    bs[21] = 0xF8 # media descriptor
    struct.pack_into('<H', bs, 22, 0) # sectors per FAT 16 (0 for FAT32)
    struct.pack_into('<H', bs, 24, 63) # sectors per track
    struct.pack_into('<H', bs, 26, 255) # heads
    struct.pack_into('<I', bs, 28, part_start_lba) # hidden sectors
    struct.pack_into('<I', bs, 32, part_sectors) # total sectors 32
    struct.pack_into('<I', bs, 36, fat_sectors) # sectors per FAT 32
    struct.pack_into('<H', bs, 40, 0) # extended flags
    struct.pack_into('<H', bs, 42, 0) # FAT32 version
    struct.pack_into('<I', bs, 44, 2) # root cluster = 2
    struct.pack_into('<H', bs, 48, 1) # FSInfo sector = 1
    struct.pack_into('<H', bs, 50, 6) # backup boot sector = 6
    bs[64] = 0x80 # drive number
    bs[66] = 0x29 # boot signature
    struct.pack_into('<I', bs, 67, 0x4842594E) # volume ID ("NYBU")
    bs[71:82] = b'NYBUOS     ' # volume label (11 chars)
    bs[82:90] = b'FAT32   ' # sys id (8 chars)
    bs[510] = 0x55
    bs[511] = 0xAA

    # FSInfo Sector (Sector 2049)
    fsi = bytearray(512)
    struct.pack_into('<I', fsi, 0, 0x41615252) # lead sig "RRaA"
    struct.pack_into('<I', fsi, 484, 0x61417272) # struct sig "rrAa"
    total_clusters = (part_sectors - reserved_sectors - num_fats * fat_sectors) // spc
    struct.pack_into('<I', fsi, 488, total_clusters - 1) # free cluster count
    struct.pack_into('<I', fsi, 492, 3) # next free cluster
    struct.pack_into('<I', fsi, 508, 0xAA550000) # trail sig

    print(f"FAT32 parameters: TotalClusters={total_clusters}, FatSectors={fat_sectors}, ClusterSize={spc*512}B")

test_mbr_and_fat32()
