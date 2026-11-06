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
guid_4byte = (heap_sizes & 2) != 0
blob_4byte = (heap_sizes & 4) != 0

valid = struct.unpack_from('<Q', meta_data, tilde_off + 8)[0]
cur_t = tilde_off + 24
row_counts = {}
for i in range(64):
    if (valid & (1 << i)) != 0:
        row_counts[i] = struct.unpack_from('<I', meta_data, cur_t)[0]
        cur_t += 4

# Table sizes:
# TypeDef: Flags(4), Name(str), Namespace(str), Extends(TypeDefOrRef), FieldList(Field), MethodList(MethodDef)
str_size = 4 if str_4byte else 2
field_idx_size = 4 if row_counts.get(4, 0) > 65535 else 2
method_idx_size = 4 if row_counts.get(6, 0) > 65535 else 2
# TypeDefOrRef encoded token (2 bits tag): 0=TypeDef, 1=TypeRef, 2=TypeSpec
max_tdor = max(row_counts.get(2,0), row_counts.get(1,0), row_counts.get(0x1B,0))
tdor_size = 4 if max_tdor > (65535 >> 2) else 2

# Calculate offset of each table:
table_offsets = {}
t_data = cur_t
table_row_sizes = {
    0: 2 + str_size + guid_4byte*2 + 2, # Module
    1: 2 + str_size + str_size, # TypeRef
    2: 4 + str_size + str_size + tdor_size + field_idx_size + method_idx_size, # TypeDef
}

# Let's read TypeDef table:
td_count = row_counts[2]
# Need table 0 and 1 row sizes to find start of table 2
# Let's compute table row sizes dynamically:
def get_coded_index_size(tags, tables):
    max_r = max([row_counts.get(t, 0) for t in tables] + [0])
    bits = (tags - 1).bit_length()
    return 4 if max_r > (65535 >> bits) else 2

# Table row sizes:
row_sizes = {}
for t in range(64):
    if t not in row_counts: continue
    # just basic for tables before 2
    if t == 0: row_sizes[0] = 2 + str_size + (4 if guid_4byte else 2)*3
    elif t == 1:
        # ResolutionScope (2 bits): Module(0), ModuleRef(1), AssemblyRef(2), TypeRef(3)
        rs_size = get_coded_index_size(4, [0, 0x1A, 0x23, 1])
        row_sizes[1] = rs_size + str_size*2

cur_pos = cur_t
table_pos = {}
for t in range(64):
    if t in row_counts:
        table_pos[t] = cur_pos
        if t == 2: break
        cur_pos += row_counts[t] * row_sizes[t]

print("TypeDef table pos:", hex(table_pos[2]))
td_pos = table_pos[2]
td_row_size = 4 + str_size*2 + tdor_size + field_idx_size + method_idx_size

types = []
for i in range(td_count):
    row = td_pos + i * td_row_size
    flags = struct.unpack_from('<I', meta_data, row)[0]
    off = row + 4
    if str_4byte:
        name_idx, ns_idx = struct.unpack_from('<II', meta_data, off)
        off += 8
    else:
        name_idx, ns_idx = struct.unpack_from('<HH', meta_data, off)
        off += 4
    name = get_str(name_idx)
    ns = get_str(ns_idx)
    types.append((ns, name))

for ns, n in types:
    if any(k in n for k in ['StorageManager', 'PartitionManager', 'VfsManager', 'FatFormatter', 'BlockDevice']):
        print(f"{ns}.{n}")
