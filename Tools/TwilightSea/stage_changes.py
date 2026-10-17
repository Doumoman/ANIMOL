import pathlib,json,subprocess
root=pathlib.Path.cwd();out=root/'Docs/Validation/TwilightSea'
previous=set((out/'changed_files.txt').read_text().splitlines()) if (out/'changed_files.txt').exists() else set()
cached=set(filter(None,subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0')))
assert not cached or cached==previous,'Unrelated staged changes must be preserved'
gone=[p for p in previous if not (root/p).exists()]
baseline=json.loads((out/'baseline.json').read_text())['files']
assert not set(gone)&set(baseline)
for start in range(0,len(gone),50):subprocess.run(['git','rm','--cached','-q','--',*gone[start:start+50]],check=True)
dirs=['Assets/ANIMOL/Sprites/Obstacles/common/twilight_sea','Assets/ANIMOL/Sprites/Obstacles/twilight_sea','Assets/ANIMOL/Sprites/Tiles/Floor/twilight_sea','Assets/ANIMOL/Sprites/Tiles/Wall/twilight_sea','Assets/ANIMOL/Sprites/Background/twilight_sea','Assets/ANIMOL/Sprites/Props/twilight_sea','Assets/ANIMOL/Prefabs/Themes/twilight_sea','Assets/ANIMOL/Data/AnimationSequences/twilight_sea','Docs/Inbox/TwilightSea','Docs/Validation/TwilightSea','Screenshots/TwilightSea','Tools/TwilightSea']
paths=[]
for name in dirs:
    paths.extend(p.relative_to(root).as_posix() for p in (root/name).rglob('*') if p.is_file())
    if (root/(name+'.meta')).exists():paths.append(name+'.meta')
for name in ['Assets/ANIMOL/Scripts/Runtime/FerryVisualView.cs','Assets/ANIMOL/Scripts/Runtime/ResolvedCellPreview.cs','Assets/ANIMOL/Scripts/Editor/TwilightSeaArtBuilder.cs','Assets/ANIMOL/Scripts/Tests/Editor/TwilightSeaArtTests.cs','Assets/ANIMOL/Scripts/Tests/PlayMode/TwilightSeaArtPlayTests.cs','Assets/ANIMOL/Data/twilight_sea_import_manifest.json','Assets/ANIMOL/Data/twilight_sea_device_rules.json','Assets/ANIMOL/Data/twilight_sea_motion_bindings.json','Assets/ANIMOL/Data/Themes/twilight_sea.asset','Assets/ANIMOL/Scenes/QA/ANIMOL_TwilightSea_Art_QA.unity','Assets/ANIMOL/Prefabs/Presentation/twilight_sea_ferry_occupied.prefab','Assets/ANIMOL/Prefabs/Presentation/twilight_sea_resolved_cell_preview.prefab']:
    paths.extend([name,name+'.meta'])
paths+=['Assets/ANIMOL/Prefabs/Presentation.meta','Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs','Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs','Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs','ANIMOL_TWILIGHT_SEA_V01_APPLY_RESULT.md','Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md','Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md','README.md','Docs/Validation/TwilightSea/changed_files.txt']
tracked=set(filter(None,subprocess.check_output(['git','ls-files','-z']).decode().split('\0')))
different=set(filter(None,subprocess.run(['git','diff','--name-only','-z'],stdout=subprocess.PIPE,stderr=subprocess.PIPE,check=True).stdout.decode().split('\0')))
paths=sorted(p for p in set(paths) if p not in tracked or p in different or p in previous)
assert all((root/p).exists() or p.endswith('changed_files.txt') for p in paths)
(out/'changed_files.txt').write_text('\n'.join(paths)+'\n',encoding='utf8')
for start in range(0,len(paths),50):subprocess.run(['git','add','--',*paths[start:start+50]],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
staged=set(filter(None,subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0')));assert staged==set(paths)
assert not any(p.startswith(('MCP_INBOX/','ProjectSettings/','Legacy/','Assets/ANIMOL/Fonts/','Packages/')) for p in staged)
print('Staged exactly',len(paths),'owned paths, preserving existing themes and user work')
