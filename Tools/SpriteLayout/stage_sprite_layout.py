from pathlib import Path
import subprocess
import json

JOURNAL=Path('C:/Users/user/AppData/Local/Temp/animol-sprite-layout')
OUT=Path('Docs/Validation/SpriteLayout')
operations=[line.split('\t') for line in (OUT/'move_operations.tsv').read_text().splitlines()]
obsolete_tools=['isolate_previous_project.cs','inspect_active_slice.cs','verify_active_slice.py','stage_active_slice.py','check_active_scene.cs','prepare_active_slice_playtests.cs','finish_active_slice.cs']
def target(path):
    if path=='ANIMOL_V04_ART_APPLY_RESULT.md':return 'Legacy/PreviousProject_20261013/Reports/'+path
    if path.startswith('Tools/') and path[6:] in obsolete_tools:return 'Legacy/PreviousProject_20261013/Tools/IsolationUtilities/'+path[6:]
    meta=path.endswith('.meta')
    if meta:path=path[:-5]
    for source,destination in operations:
        if path==source or path.startswith(source+'/'):path=destination+path[len(source):]
    if path=='Assets/ANIMOL/Art' or path.startswith('Assets/ANIMOL/Art/'):path='Legacy/PreviousProject_20261013/RetiredEmptyFolders/'+path
    return path+'.meta' if meta else path
tree={}
for entry in (JOURNAL/'head-tree.z').read_bytes().split(b'\0'):
    if not entry:continue
    info,path=entry.split(b'\t',1);mode,kind,blob=info.decode().split();tree[path.decode()]=(mode,blob)
records=[]
font_targets=[]
mapped=[]
for source,(mode,blob) in tree.items():
    destination=target(source)
    if source==destination:continue
    records.extend([f'0 {"0"*40}\t{source}\n',f'{mode} {blob}\t{destination}\n'])
    mapped.append(dict(source=source,destination=destination,baseline_blob=blob))
    if '/Fonts/' in source:font_targets.append(destination)
subprocess.run(['git','update-index','--index-info'],input=''.join(records).encode(),check=True)
(OUT/'tracked_moves.json').write_text(json.dumps(mapped,ensure_ascii=False,indent=2),encoding='utf-8')
ours=['README.md','AGENTS.md','Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md','Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md','Docs/ANIMOL_SPRITE_LAYOUT_RESULT.md','ProjectSettings/EditorBuildSettings.asset','Assets/ANIMOL/Data.meta']
for root in ('Assets/ANIMOL','Tools/SpriteLayout','Docs/Validation/SpriteLayout','Screenshots/ArtQA'):
    ours.extend(str(p).replace('\\','/') for p in Path(root).rglob('*') if p.is_file() and str(p).replace('\\','/') not in font_targets)
ours=sorted(set(ours))
for i in range(0,len(ours),60):subprocess.run(['git','add','--']+ours[i:i+60],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('Relocated tracked paths',len(mapped),'explicit own paths',len(ours),'preserved user font paths',len(font_targets))
