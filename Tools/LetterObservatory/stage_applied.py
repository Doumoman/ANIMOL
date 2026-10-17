import pathlib,json,subprocess
root=pathlib.Path.cwd();out=root/'Docs/Validation/LetterObservatory/Applied';baseline=json.loads((out/'baseline.json').read_text())['files']
assert not subprocess.check_output(['git','diff','--cached','--name-only'],text=True).strip()
dirs=['Assets/ANIMOL/Sprites/Obstacles/common/letter_observatory','Assets/ANIMOL/Sprites/Obstacles/letter_observatory','Assets/ANIMOL/Sprites/Tiles/Floor/letter_observatory','Assets/ANIMOL/Sprites/Tiles/Wall/letter_observatory','Assets/ANIMOL/Sprites/Background/letter_observatory','Assets/ANIMOL/Prefabs/Themes/letter_observatory','Assets/ANIMOL/Data/AnimationSequences/letter_observatory','Docs/Inbox/LetterObservatory/Regenerated','Docs/Validation/LetterObservatory/Applied','Screenshots/LetterObservatory','Tools/LetterObservatory']
paths=[]
for name in dirs:
    paths.extend(p.relative_to(root).as_posix() for p in (root/name).rglob('*') if p.is_file())
    if (root/(name+'.meta')).exists():paths.append(name+'.meta')
for name in ['Assets/ANIMOL/Scripts/Runtime/BeamPathView.cs','Assets/ANIMOL/Scripts/Runtime/OpticsPresentationRules.cs','Assets/ANIMOL/Scripts/Editor/LetterObservatoryArtBuilder.cs','Assets/ANIMOL/Scripts/Tests/Editor/LetterObservatoryArtTests.cs','Assets/ANIMOL/Scripts/Tests/PlayMode/LetterObservatoryArtPlayTests.cs','Assets/ANIMOL/Data/letter_observatory_import_manifest.json','Assets/ANIMOL/Data/letter_observatory_optics_rules.json','Assets/ANIMOL/Data/letter_observatory_motion_bindings.json','Assets/ANIMOL/Data/Themes/letter_observatory.asset','Assets/ANIMOL/Data/Themes/letter_observatory_optics.asset','Assets/ANIMOL/Scenes/QA/ANIMOL_LetterObservatory_Art_QA.unity']:
    paths.extend([name,name+'.meta'])
paths+=['Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs','ANIMOL_LETTER_OBSERVATORY_V01_APPLY_RESULT.md','Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md','Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md','README.md','Docs/Validation/LetterObservatory/Applied/changed_files.txt']
tracked=set(filter(None,subprocess.check_output(['git','ls-files','-z']).decode().split('\0')))
different=set(filter(None,subprocess.check_output(['git','diff','--name-only','-z']).decode().split('\0')))
paths=sorted(p for p in set(paths) if p not in tracked or p in different)
(out/'changed_files.txt').write_text('\n'.join(paths)+'\n')
for start in range(0,len(paths),50):subprocess.run(['git','add','--',*paths[start:start+50]],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
staged=set(filter(None,subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0')));assert staged==set(paths)
assert not any(p.startswith(('MCP_INBOX/','ProjectSettings/','Legacy/','Assets/ANIMOL/Fonts/')) for p in staged)
print('Staged exactly',len(paths),'task-owned files; existing themes/user settings/source bundle excluded')
