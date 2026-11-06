import struct

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    data = f.read()

# PE header
pe_offset = struct.unpack_from('<I', data, 0x3C)[0]
opt_hdr_size = struct.unpack_from('<H', data, pe_offset + 0x14)[0]
cli_rva = struct.unpack_from('<I', data, pe_offset + 0x18 + 0xE0)[0]

# Find section containing cli_rva
num_sections = struct.unpack_from('<H', data, pe_offset + 0x06)[0]
sec_offset = pe_offset + 0x18 + opt_hdr_size

def rva_to_offset(rva):
    for i in range(num_sections):
        s_va = struct.unpack_from('<I', data, sec_offset + i*40 + 12)[0]
        s_raw_ptr = struct.unpack_from('<I', data, sec_offset + i*40 + 20)[0]
        s_raw_size = struct.unpack_from('<I', data, sec_offset + i*40 + 16)[0]
        if s_va <= rva < s_va + s_raw_size:
            return s_raw_ptr + (rva - s_va)
    return None

cli_offset = rva_to_offset(cli_rva)
meta_rva = struct.unpack_from('<I', data, cli_offset + 8)[0]
meta_offset = rva_to_offset(meta_rva)

# In metadata root:
magic = struct.unpack_from('<I', data, meta_offset)[0]
vlen = struct.unpack_from('<I', data, meta_offset + 12)[0]
streams_offset = meta_offset + 16 + vlen
streams_offset = (streams_offset + 3) & ~3
num_streams = struct.unpack_from('<H', data, streams_offset + 2)[0]

stream_ptrs = {}
cur = streams_offset + 4
for _ in range(num_streams):
    s_off, s_sz = struct.unpack_from('<II', data, cur)
    cur += 8
    name = []
    while data[cur] != 0:
        name.append(chr(data[cur]))
        cur += 1
    cur += 1
    cur = (cur + 3) & ~3
    name = ''.join(name)
    stream_ptrs[name] = (meta_offset + s_off, s_sz)

print(f"Streams found: {list(stream_ptrs.keys())}")

# Let's inspect #Strings
str_off, str_sz = stream_ptrs['#Strings']
def get_str(idx):
    end = data.find(b'\x00', str_off + idx)
    return data[str_off + idx : end].decode('utf-8', errors='ignore')

# Let's inspect #~
tilde_off, tilde_sz = stream_ptrs['#~']
valid = struct.unpack_from('<Q', data, tilde_off + 8)[0]
print(f"Valid tables mask: {hex(valid)}")

# Read row counts
cur_t = tilde_off + 24
row_counts = {}
for i in range(64):
    if (valid & (1 << i)) != 0:
        row_counts[i] = struct.unpack_from('<I', data, cur_t)[0]
        cur_t += 4

# Table 0x02 is TypeDef, Table 0x06 is MethodDef
print("Row counts: TypeDef(2)=", row_counts.get(2), "MethodDef(6)=", row_counts.get(6))
