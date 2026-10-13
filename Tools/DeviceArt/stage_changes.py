import pathlib,subprocess,json
root=pathlib.Path.cwd()
assert not subprocess.check_output(['git','diff','--cached','--name-only'],text=True).strip(),'Existing staged changes must be preserved'
baseline=json.loads((root/'Docs/Validation/DeviceArt/baseline.json').read_text())['files']
dirs=['Assets/ANIMOL/Sprites/Obstacles','Assets/ANIMOL/Prefabs/Devices','Assets/ANIMOL/Data/AnimationSequences/Devices','Docs/Inbox/DeviceArt','Tools/DeviceArt','Screenshots/DeviceArt']
paths=[]
for name in dirs:
    paths.extend(p.relative_to(root).as_posix() for p in (root/name).rglob('*') if p.is_file())
    meta=root/(name+'.meta')
    if meta.exists():paths.append(meta.relative_to(root).as_posix())
for name in ['Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs','Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs','Assets/ANIMOL/Scripts/Editor/DeviceArtBuilder.cs','Assets/ANIMOL/Scripts/Tests/Editor/DeviceArtTests.cs','Assets/ANIMOL/Scripts/Tests/PlayMode/DeviceArtPlayTests.cs','Assets/ANIMOL/Data/device_art_import_manifest.json','Assets/ANIMOL/Scenes/ANIMOL_Device8_Art_QA.unity']:
    paths.extend([name,name+'.meta'])
for name in ['baseline.json','source_path_map.json','audit.json','device_edit.json','device_play.json','base_edit.json','base_play.json','scene_validation.json','console_final.json','changed_files.txt']:
    paths.append('Docs/Validation/DeviceArt/'+name)
for name in paths:
    assert name not in baseline,'Unexpected pre-existing asset selected: '+name
paths+=['Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs','Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md','README.md','ANIMOL_DEVICE8_V01_APPLY_RESULT.md']
paths=sorted(set(paths))
(root/'Docs/Validation/DeviceArt/changed_files.txt').write_text('\n'.join(paths)+'\n')
for start in range(0,len(paths),50):subprocess.run(['git','add','--',*paths[start:start+50]],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
staged=subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0');staged=set(filter(None,staged))
assert staged==set(paths),(staged-set(paths),set(paths)-staged)
print(f'Staged exactly {len(paths)} task-owned files; no input ZIP, fonts, settings or Legacy files')
