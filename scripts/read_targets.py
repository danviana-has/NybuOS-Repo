with open(r'C:\Users\dvian\.nuget\packages\cosmos.sdk\3.0.95\Sdk\Sdk.targets', 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()
for i, line in enumerate(lines):
    l = line.lower()
    if 'iso' in l or 'limine' in l or 'xorriso' in l:
        print(f'{i+1}: {line.strip()[:100]}')
