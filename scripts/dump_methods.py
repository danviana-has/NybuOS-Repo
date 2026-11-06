import pefile, struct

pe = pefile.PE(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll')
cli_dir = pe.OPTIONAL_HEADER.DATA_DIRECTORY[14]
cli_data = pe.get_data(cli_dir.VirtualAddress, cli_dir.Size)
meta_rva = struct.unpack_from('<I', cli_data, 8)[0]
meta_size = struct.unpack_from('<I', cli_data, 12)[0]
meta_data = pe.get_data(meta_rva, meta_size)

vlen = struct.unpack_from('<I', meta_data, 12)[0]
streams_offset = (16 + vlen + 3) & ~3
num_streams = struct.unpack_from('<H', meta_data, streams_offset + 2)[0]

stream_ptrs = {}
cur = streams_offset + 4
for _ in range(num_streams):
    s_off, s_sz = struct.unpack_from('<II', meta_data, cur)
    cur += 8
    name = []
    while meta_data[cur] != 0:
        name.append(chr(meta_data[cur]))
        cur += 1
    cur += 1
    cur = (cur + 3) & ~3
    name = ''.join(name)
    stream_ptrs[name] = (s_off, s_sz)

str_off, _ = stream_ptrs['#Strings']
def get_str(idx):
    end = meta_data.find(b'\x00', str_off + idx)
    return meta_data[str_off + idx : end].decode('utf-8', errors='ignore')

tilde_off, _ = stream_ptrs['#~']
heap_sizes = meta_data[tilde_off + 6]
str_4byte = (heap_sizes & 1) != 0
blob_4byte = (heap_sizes & 4) != 0

valid = struct.unpack_from('<Q', meta_data, tilde_off + 8)[0]
cur_t = tilde_off + 24
row_counts = {}
for i in range(64):
    if (valid & (1 << i)) != 0:
        row_counts[i] = struct.unpack_from('<I', meta_data, cur_t)[0]
        cur_t += 4

str_size = 4 if str_4byte else 2
field_idx_size = 4 if row_counts.get(4, 0) > 65535 else 2
method_idx_size = 4 if row_counts.get(6, 0) > 65535 else 2
max_tdor = max(row_counts.get(2,0), row_counts.get(1,0), row_counts.get(0x1B,0))
tdor_size = 4 if max_tdor > (65535 >> 2) else 2

def get_coded_index_size(tags, tables):
    max_r = max([row_counts.get(t, 0) for t in tables] + [0])
    bits = (tags - 1).bit_length()
    return 4 if max_r > (65535 >> bits) else 2

row_sizes = {
    0: 2 + str_size + (4 if (heap_sizes & 2) else 2)*3,
    1: get_coded_index_size(4, [0, 0x1A, 0x23, 1]) + str_size*2,
    2: 4 + str_size*2 + tdor_size + field_idx_size + method_idx_size,
    4: 2 + str_size + (4 if blob_4byte else 2),
    6: 4 + 2 + 2 + str_size + (4 if blob_4byte else 2) + (4 if row_counts.get(8,0) > 65535 else 2) # MethodDef
}

cur_pos = cur_t
table_pos = {}
for t in range(64):
    if t in row_counts:
        table_pos[t] = cur_pos
        cur_pos += row_counts[t] * row_sizes.get(t, 0)

# Read all MethodDef names
method_names = []
for i in range(row_counts[6]):
    m_row = table_pos[6] + i * row_sizes[6]
    off = m_row + 8 # skip RVA (4), ImplFlags (2), Flags (2)
    name_idx = struct.unpack_from('<I' if str_4byte else '<H', meta_data, off)[0]
    method_names.append(get_str(name_idx))

# Read TypeDefs with their method ranges
td_count = row_counts[2]
td_pos = table_pos[2]
td_row_size = row_sizes[2]

for i in range(td_count):
    row = td_pos + i * td_row_size
    off = row + 4
    if str_4byte:
        name_idx, ns_idx = struct.unpack_from('<II', meta_data, off)
        off += 8 + tdor_size + field_idx_size
        m_idx = struct.unpack_from('<I' if method_idx_size == 4 else '<H', meta_data, off)[0]
    else:
        name_idx, ns_idx = struct.unpack_from('<HH', meta_data, off)
        off += 4 + tdor_size + field_idx_size
        m_idx = struct.unpack_from('<I' if method_idx_size == 4 else '<H', meta_data, off)[0]
    
    # Next type's method index
    if i + 1 < td_count:
        next_off = td_pos + (i+1)*td_row_size + 4 + (8 if str_4byte else 4) + tdor_size + field_idx_size
        next_m_idx = struct.unpack_from('<I' if method_idx_size == 4 else '<H', meta_data, next_off)[0]
    else:
        next_m_idx = row_counts[6] + 1
    
    name = get_str(name_idx)
    ns = get_str(ns_idx)
    
    if any(k in name for k in ['StorageManager', 'PartitionManager', 'VfsManager', 'FatFormatter', 'BlockDevice']):
        print(f"\n=== {ns}.{name} ===")
        # 1-based indexing in ECMA-335
        for m in range(m_idx - 1, next_m_idx - 1):
            if m < len(method_names):
                print(f"   method: {method_names[m]}")
