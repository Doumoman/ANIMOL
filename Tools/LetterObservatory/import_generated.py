import pathlib,json,hashlib,shutil
root=pathlib.Path.cwd();src=pathlib.Path('C:/Users/user/AppData/Local/Temp/animol-observatory-build/output/ANIMOL_LETTER_OBSERVATORY_V01');out=root/'Docs/Validation/LetterObservatory/Applied'
assert not (root/'Assets/ANIMOL/Data/letter_observatory_import_manifest.json').exists()
m=json.loads((src/'unity_import_manifest.json').read_text(encoding='utf8'));original=json.load(open(root/'Docs/Inbox/LetterObservatory/manifest.json',encoding='utf8'));rebuilt=json.loads((src/'manifest.json').read_text(encoding='utf8'));assert original==rebuilt
registry={'03':'M03_CrescentJump','07':'M07_Conveyor','09':'M09_OneWayGate','10':'M10_LightSensor','14':'M14_Magnet','16':'M16_PairedPortal','18':'M18_BounceCorner','19':'M19_BrakePlate','21':'M21_GlassWall'};mapping=[]
def target(row,index=None,clip=False):
    d=row['deviceId'];prefix,state=row['id'].split('/');kind='anim' if clip else 'spr'
    if d!='BG':
        group='common/letter_observatory' if d in ['07','09','18','19'] else 'letter_observatory';parent=f'Sprites/Obstacles/{group}/{registry[d]}';scope='obstacle';obj='m'+d+'_'+prefix[4:]
    else:
        obj=state;state='';scope='bg'
        if obj.startswith('floor'):parent='Sprites/Tiles/Floor/letter_observatory';scope='tile_floor'
        elif obj.startswith(('wall','cracked_wall')):parent='Sprites/Tiles/Wall/letter_observatory';scope='tile_wall'
        else:parent='Sprites/Background/letter_observatory/'+('Far' if 'backdrop' in obj else 'Mid' if obj.startswith(('observatory_column','telescope','letter_box')) else 'Near')
    sub='Animations' if clip else 'Frames' if d!='BG' or index is not None else ''
    stem=f'{kind}_{scope}_letter_observatory_{obj}'+('_'+state if state else '')+(f'_f{index:02}' if index is not None else '')
    return parent+('/'+sub if sub else '')+'/'+stem+('.anim' if clip else '.png')
def copy(old,new):
    dest=root/'Assets/ANIMOL'/new;assert not dest.exists();dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src/old,dest);mapping.append({'source':old,'target':dest.relative_to(root).as_posix(),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
for row in m['assets']+m['animations']:
    row['pivotX']/=row['width'];row['pivotY']=1-row['pivotY']/row['height']
    if 'frames' in row:
        row['clipPath']=target(row,clip=True);(root/'Assets/ANIMOL'/row['clipPath']).parent.mkdir(parents=True,exist_ok=True)
        for i,f in enumerate(row['frames']):old=f['path'];f['path']=target(row,i);copy(old,f['path'])
    else:old=row['path'];row['path']=target(row);copy(old,row['path'])
(root/'Assets/ANIMOL/Data/letter_observatory_import_manifest.json').write_text(json.dumps(m,indent=2),encoding='utf8')
for name in ['OPTICS_RULES','MOTION_BINDINGS']:shutil.copyfile(src/f'data/{name}.json',root/f'Assets/ANIMOL/Data/letter_observatory_{name.lower()}.json')
docs=root/'Docs/Inbox/LetterObservatory/Regenerated';docs.mkdir(exist_ok=True)
for name in ['README_KO.txt','manifest.json','unity_import_manifest.json','DEVICE_EVENT_BINDINGS.csv','data/OPTICS_RULES.json','data/MOTION_BINDINGS.json','QA_REPORT.json','POLISH_REVIEW_KO.csv','source/ADDED_DECISIONS_KO.txt','UNITY_APPLY_LETTER_OBSERVATORY_V01.txt']:shutil.copyfile(src/name,docs/pathlib.Path(name).name)
(out/'source_path_map.json').write_text(json.dumps(mapping,indent=2));(out/'regeneration.json').write_text(json.dumps({'sourceBundle':'MCP_INBOX/ANIMOL_LETTER_OBSERVATORY_V01','recoveredManifestExactlyMatches':True,'sourceFileQaPassed':True,'qaPortabilityFix':'Temp QA only: relative_to().as_posix() on Windows; no raster processing changes','runtimePng':len(mapping),'clips':len(m['animations'])},indent=2))
print(f'Imported {len(mapping)} authored runtime PNG, {len(m["animations"])} clips declared; no source/preview/sheets')
