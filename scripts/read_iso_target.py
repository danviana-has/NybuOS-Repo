with open(r'C:\Users\dvian\.nuget\packages\cosmos.build.common\3.0.95\build\Cosmos.Build.Common.targets', 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

for i in range(130, 210):
    if i < len(lines):
        print(f"{i+1}: {lines[i].rstrip()}")
