import pathlib,json,hashlib,shutil
root=pathlib.Path.cwd();src=pathlib.Path('C:/Users/user/AppData/Local/Temp/animol-sea-build/ANIMOL_TWILIGHT_SEA_V01_SOURCE/output/ANIMOL_TWILIGHT_SEA_V01');out=root/'Docs/Validation/TwilightSea'
assert not (root/'Assets/ANIMOL/Data/twilight_sea_import_manifest.json').exists()
m=json.loads((src/'unity_import_manifest.json').read_text(encoding='utf8'));mapping=[]
registry={'05':'M05_ExitSpikes','07':'M07_Conveyor','09':'M09_OneWayGate','11':'M11_Ferry','12':'M12_RollingBarrel','15':'M15_TimedBomb','18':'M18_BounceCorner','19':'M19_BrakePlate'}
def target(row,index=None,clip=False):
    d=row['deviceId'];prefix,state=row['id'].split('/');kind='anim' if clip else 'spr'
    parts=state.split('_')
    if len(parts)>2 and parts[-1] in ['rear','front'] and parts[-2] in ['n','e','s','w']:state='_'.join(parts[:-2]+[parts[-1],parts[-2]])
    if d!='BG':
        group='common/twilight_sea' if d in ['07','09','18','19'] else 'twilight_sea';parent=f'Sprites/Obstacles/{group}/{registry[d]}';scope='obstacle';obj='m'+d+'_'+prefix[4:]
    else:
        obj=state;state='';scope='bg'
        if obj.startswith(('floor','water','shore')):parent='Sprites/Tiles/Floor/twilight_sea';scope='tile_floor'
        elif obj.startswith(('wall','cracked_wall')):parent='Sprites/Tiles/Wall/twilight_sea';scope='tile_wall'
        elif obj=='wood_crate':parent='Sprites/Props/twilight_sea';scope='prop'
        else:parent='Sprites/Background/twilight_sea/'+('Far' if 'backdrop' in obj else 'Mid' if obj.startswith(('rail','dock')) else 'Near')
    sub='Animations' if clip else 'Frames' if d!='BG' or index is not None else ''
    stem=f'{kind}_{scope}_twilight_sea_{obj}'+('_'+state if state else '')+(f'_f{index:02}' if index is not None else '')
    return parent+('/'+sub if sub else '')+'/'+stem+('.anim' if clip else '.png')
def copy(old,new):
    dest=root/'Assets/ANIMOL'/new;assert not dest.exists();dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src/old,dest);mapping.append({'source':old,'target':dest.relative_to(root).as_posix(),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
for row in m['assets']+m['animations']:
    row['pivotX']/=row['width'];row['pivotY']=1-row['pivotY']/row['height']
    if 'frames' in row:
        row['passengerOffsets']=[{'x':x,'y':y} for x,y in row.get('passengerOffsetPixelsByFrame',[])]
        row['clipPath']=target(row,clip=True);(root/'Assets/ANIMOL'/row['clipPath']).parent.mkdir(parents=True,exist_ok=True)
        for i,f in enumerate(row['frames']):old=f['path'];f['path']=target(row,i);copy(old,f['path'])
    else:old=row['path'];row['path']=target(row);copy(old,row['path'])
(root/'Assets/ANIMOL/Data/twilight_sea_import_manifest.json').write_text(json.dumps(m,indent=2),encoding='utf8')
for name in ['DEVICE_RULES','MOTION_BINDINGS']:shutil.copyfile(src/f'data/{name}.json',root/f'Assets/ANIMOL/Data/twilight_sea_{name.lower()}.json')
docs=root/'Docs/Inbox/TwilightSea';docs.mkdir(parents=True,exist_ok=True)
for name in ['README_KO.txt','manifest.json','unity_import_manifest.json','DEVICE_EVENT_BINDINGS.csv','data/DEVICE_RULES.json','data/MOTION_BINDINGS.json','QA_REPORT.json','POLISH_REVIEW_KO.csv','source/ADDED_DECISIONS_KO.txt','UNITY_APPLY_TWILIGHT_SEA_V01.txt','source/SOURCE_CELL_CHECKS.json']:shutil.copyfile(src/name,docs/pathlib.Path(name).name)
(out/'source_path_map.json').write_text(json.dumps(mapping,indent=2));(out/'regeneration.json').write_text(json.dumps({'input':'MCP_INBOX/ANIMOL_TWILIGHT_SEA_V01.zip','inputKind':'authored source bundle','sourceQaPassed':True,'runtimePng':len(mapping),'clips':len(m['animations']),'qaPortabilityChange':'Temp only: relative_to().as_posix(); raster production unmodified'},indent=2))
print('Imported',len(mapping),'PNG; declared',len(m['animations']),'clips')
