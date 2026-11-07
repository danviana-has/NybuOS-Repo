import struct

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    data = f.read()

bsjb = data.find(b'BSJB')
vlen = int.from_bytes(data[bsjb+12:bsjb+16], 'little')
offset = bsjb + 16 + vlen
if offset % 4 != 0: offset += 4 - (offset % 4)
offset += 4

streams = {}
for i in range(5):
    soff = int.from_bytes(data[offset:offset+4], 'little')
    ssize = int.from_bytes(data[offset+4:offset+8], 'little')
    nend = data.find(b'\x00', offset+8)
    sname = data[offset+8:nend].decode('latin1')
    offset = nend + 1
    if offset % 4 != 0: offset += 4 - (offset % 4)
    streams[sname] = (bsjb + soff, ssize)

strings_raw = data[streams['#Strings'][0] : streams['#Strings'][0] + streams['#Strings'][1]]
blob_raw = data[streams['#Blob'][0] : streams['#Blob'][0] + streams['#Blob'][1]]

def get_str(idx):
    end = strings_raw.find(b'\x00', idx)
    return strings_raw[idx:end].decode('latin1')

tilde_off, tilde_size = streams['#~']
tdata = data[tilde_off : tilde_off + tilde_size]
major, minor, heappr, rid, valid = struct.unpack_from('<BBBBQ', tdata, 4)
pos = 24
row_counts = {}
for i in range(64):
    if (valid & (1 << i)) != 0:
        row_counts[i] = struct.unpack_from('<I', tdata, pos)[0]
        pos += 4

str_size = 4 if (heappr & 1) else 2
guid_size = 4 if (heappr & 2) else 2
blob_size = 4 if (heappr & 4) else 2

def get_coded_size(tag_bits, *table_ids):
    max_rows = max([row_counts.get(tid, 0) for tid in table_ids] + [0])
    return 2 if (max_rows < (1 << (16 - tag_bits))) else 4

def get_simple_index_size(table_id):
    return 2 if row_counts.get(table_id, 0) < 65536 else 4

table_sizes = {}
table_sizes[0] = 2 + str_size + guid_size * 3
table_sizes[1] = get_coded_size(2, 0, 1, 26, 35) + str_size * 2
table_sizes[2] = 4 + str_size * 2 + get_coded_size(2, 1, 2, 27) + get_simple_index_size(4) + get_simple_index_size(6)
table_sizes[4] = 2 + str_size + blob_size
table_sizes[6] = 4 + 2 + 2 + str_size + blob_size + get_simple_index_size(8)
table_sizes[8] = 2 + 2 + str_size

table_offsets = {}
curr = pos
for i in range(9):
    if (valid & (1 << i)) != 0:
        table_offsets[i] = curr
        curr += table_sizes[i] * row_counts[i]

def read_str_at(p):
    val = struct.unpack_from('<H', tdata, p)[0] if str_size == 2 else struct.unpack_from('<I', tdata, p)[0]
    return get_str(val)

def read_idx_at(p, t):
    sz = get_simple_index_size(t)
    return struct.unpack_from('<H' if sz == 2 else '<I', tdata, p)[0]

m_off = table_offsets[6]
m_sz = table_sizes[6]
p_off = table_offsets[8]
p_sz = table_sizes[8]

t_off = table_offsets[2]
t_sz = table_sizes[2]
coded_sz = get_coded_size(2, 1, 2, 27)

for i in range(row_counts[2]):
    p = t_off + i * t_sz
    tname = read_str_at(p + 4)
    tns = read_str_at(p + 4 + str_size)
    fullname = f'{tns}.{tname}' if tns else tname
    m_idx = read_idx_at(p + 4 + str_size * 2 + coded_sz + get_simple_index_size(4), 6)
    if i + 1 < row_counts[2]:
        next_p = t_off + (i + 1) * t_sz
        next_m_idx = read_idx_at(next_p + 4 + str_size * 2 + coded_sz + get_simple_index_size(4), 6)
    else:
        next_m_idx = row_counts[6] + 1
    
    for mid in range(m_idx, next_m_idx):
        mp = m_off + (mid - 1) * m_sz
        mname = read_str_at(mp + 8)
        if mname == 'Receive' and 'Dhcp' in fullname:
            sig_idx = struct.unpack_from('<H', tdata, mp + 8 + str_size)[0]
            sig_len = blob_raw[sig_idx]
            sig_bytes = blob_raw[sig_idx+1 : sig_idx+1+sig_len]
            p_idx = struct.unpack_from('<H', tdata, mp + 8 + str_size + blob_size)[0]
            param_row = p_off + (p_idx - 1) * p_sz
            p_flags, p_seq = struct.unpack_from('<HH', tdata, param_row)
            pname = read_str_at(param_row + 4)
            print(f'{fullname}.{mname}: sig={sig_bytes.hex()} param={pname} flags={hex(p_flags)}')
