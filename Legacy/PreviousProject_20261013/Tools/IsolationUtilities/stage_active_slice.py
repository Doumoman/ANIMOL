"""Stage only this isolation. Use original HEAD blobs for moved pre-existing files."""
from pathlib import Path
import subprocess
import json

def git(*args, data=None):
    return subprocess.check_output(['git',*args],input=data)

JOURNAL=Path('C:/Users/user/AppData/Local/Temp/animol-active-slice')
OUT=Path('Docs/Validation/ActiveSlice')
roots=(OUT/'moved_roots.txt').read_text().splitlines()
fonts=dict(line.split('\t') for line in (OUT/'font_moves.tsv').read_text().splitlines())
font_paths=dict(fonts)
font_paths.update({source+'.meta':target+'.meta' for source,target in fonts.items()})
tree={}
for line in (JOURNAL/'head-tree.z').read_bytes().split(b'\0'):
    if not line:continue
    info,path=line.split(b'\t',1)
    mode,kind,blob=info.decode().split()
    tree[path.decode()]=(mode,blob)
records=[]
mapped=[]
for source,(mode,blob) in tree.items():
    target=font_paths.get(source)
    if target is None and any(source==root or source.startswith(root+'/') for root in roots):
        target='Legacy/PreviousProject_20261013/'+source
    if target:
        records.extend([f'0 {"0"*40}\t{source}\n',f'{mode} {blob}\t{target}\n'])
        mapped.append(dict(source=source,destination=target,baseline_blob=blob))
    if source in font_paths:
        snapshot='Legacy/PreviousProject_20261013/SharedDependenciesSnapshot/'+source
        records.append(f'{mode} {blob}\t{snapshot}\n')
for name in ('EditorBuildSettings.asset','GraphicsSettings.asset','QualitySettings.asset','EditorSettings.asset','ProjectSettings.asset'):
    source='ProjectSettings/'+name
    mode,blob=tree[source]
    records.append(f'{mode} {blob}\tLegacy/PreviousProject_20261013/{source}\n')
git('update-index','--index-info',data=''.join(records).encode())
(OUT/'tracked_moves.json').write_text(json.dumps(mapped,ensure_ascii=False,indent=2),encoding='utf-8')
ours=[
 'Assets/ANIMOL/StarwayV04/Editor/StarwayGameView.cs',
 'Assets/ANIMOL/StarwayV04/Editor/StarwayGameView.cs.meta',
 'Assets/ANIMOL/StarwayV04/Editor/StarwayV04Builder.cs',
 'Assets/ANIMOL/StarwayV04/Tests/PlayMode/StarwayV04PlayTests.cs',
 'Assets/ANIMOL/Art/StarwayPasture/V04/Fonts.meta',
 'ProjectSettings/EditorBuildSettings.asset','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset',
 'Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md','Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md',
 'Legacy/PreviousProject_20261013/README.md','AGENTS.md',
 'README.md','Screenshots/ActiveSlice_20261013/qa_clean_project.png',
]
for root in ('Tools','Docs/Validation/ActiveSlice'):
    ours.extend(str(p).replace('\\','/') for p in Path(root).rglob('*') if p.is_file())
ours.extend(str(p).replace('\\','/') for p in Path('Assets/ANIMOL/Art/StarwayPasture/V04/Fonts').glob('*OFL.txt*'))
for i in range(0,len(ours),60):git('add','--',*ours[i:i+60])
print('Tracked files relocated with original blobs:',len(mapped))
print('Explicit own paths:',len(ours))
