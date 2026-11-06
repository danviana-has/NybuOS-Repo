import pefile, struct

pe = pefile.PE(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll')
cli_dir = pe.OPTIONAL_HEADER.DATA_DIRECTORY[14]
cli_data = pe.get_data(cli_dir.VirtualAddress, cli_dir.Size)
meta_rva = struct.unpack_from('<I', cli_data, 8)[0]
meta_size = struct.unpack_from('<I', cli_data, 12)[0]
meta_data = pe.get_data(meta_rva, meta_size)

magic = struct.unpack_from('<I', meta_data, 0)[0]
vlen = struct.unpack_from('<I', meta_data, 12)[0]
streams_offset = 16 + vlen
streams_offset = (streams_offset + 3) & ~3
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

print("Streams:", stream_ptrs)
str_off, _ = stream_ptrs['#Strings']
def get_str(idx):
    end = meta_data.find(b'\x00', str_off + idx)
    return meta_data[str_off + idx : end].decode('utf-8', errors='ignore')

tilde_off, _ = stream_ptrs['#~']
valid = struct.unpack_from('<Q', meta_data, tilde_off + 8)[0]
cur_t = tilde_off + 24
row_counts = {}
for i in range(64):
    if (valid & (1 << i)) != 0:
        row_counts[i] = struct.unpack_from('<I', meta_data, cur_t)[0]
        cur_t += 4

print("TypeDef count:", row_counts[2], "MethodDef count:", row_counts[6])
