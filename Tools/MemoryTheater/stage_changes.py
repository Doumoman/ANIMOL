import pathlib,subprocess,json
root=pathlib.Path.cwd();out=root/'Docs/Validation/MemoryTheater';baseline=json.loads((out/'baseline.json').read_text())['files']
already=set(filter(None,subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0')))
dirs=['Assets/ANIMOL/Sprites/Obstacles/common/memory_theater','Assets/ANIMOL/Sprites/Obstacles/memory_theater','Assets/ANIMOL/Sprites/Tiles/Floor/memory_theater','Assets/ANIMOL/Sprites/Tiles/Wall/memory_theater','Assets/ANIMOL/Sprites/Props/memory_theater','Assets/ANIMOL/Sprites/Background/memory_theater','Assets/ANIMOL/Prefabs/Themes/memory_theater','Assets/ANIMOL/Data/AnimationSequences/memory_theater','Assets/ANIMOL/Data/Themes','Docs/Inbox/MemoryTheater','Docs/Validation/MemoryTheater','Screenshots/MemoryTheater','Tools/MemoryTheater']
paths=[]
for name in dirs:
    paths.extend(p.relative_to(root).as_posix() for p in (root/name).rglob('*') if p.is_file())
    if (root/(name+'.meta')).exists():paths.append(name+'.meta')
for name in ['Assets/ANIMOL/Sprites/Obstacles/common.meta','Assets/ANIMOL/Sprites/Props.meta','Assets/ANIMOL/Prefabs/Themes.meta']:
    if (root/name).exists():paths.append(name)
for name in ['Assets/ANIMOL/Scripts/Runtime/DeviceThemeCatalog.cs','Assets/ANIMOL/Scripts/Runtime/DiceOrientationTable.cs','Assets/ANIMOL/Scripts/Editor/MemoryTheaterArtBuilder.cs','Assets/ANIMOL/Scripts/Tests/Editor/MemoryTheaterArtTests.cs','Assets/ANIMOL/Scripts/Tests/PlayMode/MemoryTheaterArtPlayTests.cs','Assets/ANIMOL/Data/memory_theater_import_manifest.json','Assets/ANIMOL/Data/memory_theater_dice_orientations.json','Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Art_QA.unity']:
    paths.extend([name,name+'.meta'])
paths.append('Docs/Validation/MemoryTheater/changed_files.txt')
for name in paths:assert name not in baseline,'Unexpected pre-existing asset selected: '+name
paths+=['Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs','Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs','Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs','Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md','Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md','README.md','ANIMOL_MEMORY_THEATER_V01_APPLY_RESULT.md']
paths=sorted(set(paths));(out/'changed_files.txt').write_text('\n'.join(paths)+'\n')
assert already<=set(paths),'Preserve unrelated existing staged changes'
for start in range(0,len(paths),50):subprocess.run(['git','add','--',*paths[start:start+50]],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
staged=set(filter(None,subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0')));assert staged==set(paths),(staged-set(paths),set(paths)-staged)
print(f'Staged exactly {len(paths)} task-owned files; old art/settings/fonts/Legacy/input ZIP excluded')
