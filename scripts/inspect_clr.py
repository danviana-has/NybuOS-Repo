import pefile

pe = pefile.PE(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll')

# Let's find CLI Header
cli_hdr_offset = pe.get_offset_from_rva(pe.OPTIONAL_HEADER.DATA_DIRECTORY[14].VirtualAddress)
print(f"CLI Header offset: {hex(cli_hdr_offset)}")

with open(r'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll', 'rb') as f:
    dll = f.read()

# Let's search for 'StorageManager' in the TypeDef strings
# In #Strings stream:
strings_offset = dll.find(b'#Strings\x00')
print(f"#Strings header offset: {hex(strings_offset)}")
