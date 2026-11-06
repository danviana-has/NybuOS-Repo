import glob

for f in glob.glob(r'C:\Users\dvian\.nuget\packages\cosmos.*\**\*.targets', recursive=True):
    with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
        lines = fp.readlines()
    for i, line in enumerate(lines):
        if '<Target' in line and any(k in line.lower() for k in ['iso', 'image', 'build', 'limine']):
            print(f"{f}:{i+1}: {line.strip()}")
