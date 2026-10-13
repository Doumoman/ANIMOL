import pathlib,json,shutil,hashlib,subprocess,csv
root=pathlib.Path.cwd();src=pathlib.Path('C:/Users/user/AppData/Local/Temp/animol-memory-theater/ANIMOL_MEMORY_THEATER_V01')
assert not (root/'Assets/ANIMOL/Data/memory_theater_import_manifest.json').exists(),'Already applied; use the Unity builder instead of overwriting import evidence'
out=root/'Docs/Validation/MemoryTheater';out.mkdir(parents=True,exist_ok=True)
baseline={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for base in ['Assets','ProjectSettings','Packages'] for p in (root/base).rglob('*') if p.is_file()}
for name in ['Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs','Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs','Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs']:
    baseline[name]=hashlib.sha256(subprocess.check_output(['git','show','HEAD:'+name])).hexdigest()
for name in ['Assets/ANIMOL/Scripts/Runtime/DeviceThemeCatalog.cs','Assets/ANIMOL/Scripts/Runtime/DiceOrientationTable.cs']:
    for suffix in ['', '.meta']:baseline.pop(name+suffix,None)
(out/'baseline.json').write_text(json.dumps({'head':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'files':baseline},indent=2))
(out/'git_status_before_asset_copy.txt').write_bytes(subprocess.check_output(['git','status','--short']))
m=json.loads((src/'unity_import_manifest.json').read_text(encoding='utf8'))
registry={'04':'M04_AlternatingShutters','06':'M06_ActionStage','07':'M07_Conveyor','08':'M08_RotatingBookshelf','09':'M09_OneWayGate','13':'M13_DominoWall','17':'M17_ChargeStatue','18':'M18_BounceCorner','19':'M19_BrakePlate','22':'M22_DiceBlock','23':'M23_CowardDoll'}
mapping=[]
def path(row,frame=None,clip=False):
    d=row['deviceId'];prefix,state=row['id'].split('/');kind='anim' if clip else 'spr'
    if d!='BG':
        group='common/memory_theater' if d in ['07','09','18','19'] else 'memory_theater'
        parent=f'Sprites/Obstacles/{group}/{registry[d]}';scope='obstacle';obj='m'+d+'_'+prefix[4:]
    else:
        obj=state.replace('lamp_idle','wall_lamp_idle');state='';scope='bg'
        if obj.startswith('floor_'):parent='Sprites/Tiles/Floor/memory_theater';scope='tile_floor'
        elif obj.startswith(('wall','cracked_wall')):parent='Sprites/Tiles/Wall/memory_theater';scope='tile_wall'
        elif obj=='wood_crate':parent='Sprites/Props/memory_theater';scope='prop'
        else:parent='Sprites/Background/memory_theater/'+('Far' if obj=='theater_backdrop' else 'Mid' if obj.startswith(('curtain','theater_post')) else 'Near')
    directory=('Animations' if clip else 'Frames') if d!='BG' or clip or frame is not None else ''
    stem=f'{kind}_{scope}_memory_theater_{obj}'+('_'+state if state else '')+(f'_f{frame:02}' if frame is not None else '')
    return parent+('/'+directory if directory else '')+'/'+stem+('.anim' if clip else '.png')
def copy(old,new):
    dest=root/'Assets/ANIMOL'/new;assert not dest.exists(),dest
    dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src/old,dest)
    mapping.append({'source':old,'target':dest.relative_to(root).as_posix(),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
for row in m['assets']+m['animations']:
    row['pivotX']/=row['width'];row['pivotY']=1-row['pivotY']/row['height']
    if 'frames' in row:
        row['clipPath']=path(row,clip=True)
        (root/'Assets/ANIMOL'/row['clipPath']).parent.mkdir(parents=True,exist_ok=True)
        for i,f in enumerate(row['frames']):old=f['path'];f['path']=path(row,i);copy(old,f['path'])
    else:old=row['path'];row['path']=path(row);copy(old,row['path'])
(root/'Assets/ANIMOL/Data/memory_theater_import_manifest.json').write_text(json.dumps(m,indent=2),encoding='utf8')
shutil.copyfile(src/'data/DICE_ORIENTATIONS.json',root/'Assets/ANIMOL/Data/memory_theater_dice_orientations.json')
(out/'source_path_map.json').write_text(json.dumps(mapping,indent=2))
docs=root/'Docs/Inbox/MemoryTheater';docs.mkdir(parents=True,exist_ok=True)
for name in ['README_KO.txt','manifest.json','unity_import_manifest.json','DEVICE_EVENT_BINDINGS.csv','data/DICE_ORIENTATIONS.json','QA_REPORT.json','source/ADDED_DECISIONS_KO.txt','UNITY_APPLY_MEMORY_THEATER_V01.txt']:
    shutil.copyfile(src/name,docs/pathlib.Path(name).name)
csvrows=list(csv.DictReader((src/'DEVICE_EVENT_BINDINGS.csv').read_text(encoding='utf8').splitlines()))
assert len(csvrows)==len(m['animations'])==211
assert sum(len(a['frames']) for a in m['animations'])==1242
print(f'Copied {len(mapping)} source-identical PNG; theme memory_theater; 211 animations')
