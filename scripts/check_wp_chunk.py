import base64, struct

with open(r'GUI\EmbeddedWallpaperData.cs', 'rb') as f:
    text = f.read(5000).decode('utf-8-sig', errors='ignore')

parts = text.split('@"')
chunk0 = parts[1].split('"')[0][:200]
data = base64.b64decode(chunk0)
print('Signature:', data[:2])
file_size = struct.unpack_from('<I', data, 2)[0]
offset = struct.unpack_from('<I', data, 10)[0]
header_size, width, height, planes, bpp = struct.unpack_from('<IiiHH', data, 14)
print(f'File Size: {file_size}, Width: {width}, Height: {height}, BPP: {bpp}, Data Offset: {offset}')
