"""Prepare GUID-preserving moves into the versionless sprite layout."""
from pathlib import Path
import json
import subprocess
import hashlib

JOURNAL=Path('C:/Users/user/AppData/Local/Temp/animol-sprite-layout')
JOURNAL.mkdir(exist_ok=True)
SOURCE='Assets/ANIMOL/Art/StarwayPasture/V04'
ROOT='Assets/ANIMOL'
manifest=json.loads(Path(SOURCE+'/unity_import_manifest.json').read_text())
full_manifest=json.loads(Path(SOURCE+'/manifest.json').read_text())
def location(asset):
    name=asset['id']
    if '/ui/' in asset['path']:
        names={'pause_normal':'pause_idle','pause_press':'pause_pressed','pause_release':'pause_released','pause_disabled':'pause_disabled','cost_panel_blank':'cost_panel_idle','bubble_counter_blank':'bubble_counter_idle','soap_bubble_hud':'bubble_icon_idle'}
        return 'Sprites/UI/InGame','spr_ui_ingame_'+names[name]
    if name=='rabbit_front':return 'Sprites/Player/rabbit/Frames','spr_player_rabbit_idle_s'
    if name.startswith('grass_') and '/tiles/' in asset['path']:return 'Sprites/Tiles/Floor/ranch','spr_tile_floor_ranch_'+name+'_idle'
    if name.startswith('stone_'):return 'Sprites/Tiles/Wall/ranch','spr_tile_wall_ranch_'+name+'_idle'
    if name in ('cloud_small','crescent_moon','star_large'):
        layer='Mid' if name=='cloud_small' else 'Far'
        return 'Sprites/Background/ranch/'+layer,'spr_bg_ranch_'+layer.lower()+'_'+name+'_idle'
    object_name='bubble' if name=='soap_bubble_pink' else name
    return 'Sprites/Tiles/Decoration/ranch','spr_tile_decoration_ranch_'+object_name+'_idle'
leaves=[]
path_map={}
def add(source,destination):
    leaves.append(dict(source=source,destination=destination))
    if source.startswith(SOURCE+'/'):path_map[source[len(SOURCE)+1:]]=destination[len(ROOT)+1:]
for asset in manifest['assets']:
    directory,name=location(asset)
    new_path=directory+'/'+name+'.png'
    add(SOURCE+'/'+asset['path'],ROOT+'/'+new_path)
    asset['sourcePath']=asset['path'];asset['path']=new_path
for animation in manifest['animations']:
    for i,frame in enumerate(animation['frames']):
        new_path='Sprites/Tiles/Decoration/ranch/spr_tile_decoration_ranch_'+animation['id']+'_f'+f'{i:02d}'+'.png'
        add(SOURCE+'/'+frame['path'],ROOT+'/'+new_path)
        frame['sourcePath']=frame['path'];frame['path']=new_path
    clip_path='Sprites/Tiles/Decoration/ranch/anim_tile_decoration_ranch_'+animation['id']+'.anim'
    add(SOURCE+'/Clips/'+animation['id']+'.anim',ROOT+'/'+clip_path)
    animation['clipPath']=clip_path
add(SOURCE+'/'+manifest['background'],ROOT+'/Sprites/Background/ranch/Far/spr_bg_ranch_far_night_sky_idle.png')
manifest['sourceBackground']=manifest['background'];manifest['background']=path_map[manifest['background']]
for asset in full_manifest['assets']:
    asset['source_file']=asset['file'];asset['file']=path_map[asset['file']]
for clip in full_manifest['clips'].values():
    for frame in clip['frames']:
        frame['source_file']=frame['file'];frame['file']=path_map[frame['file']]
    if 'sheet_file' in clip:clip['source_sheet_file']=clip.pop('sheet_file')
full_manifest['background']['source_file']=full_manifest['background']['file']
full_manifest['background']['file']=manifest['background']
folders=[
 dict(source=SOURCE+'/sprites',destination=ROOT+'/Sprites'),
 dict(source=ROOT+'/Sprites/tiles',destination=ROOT+'/Sprites/Tiles'),
 dict(source=ROOT+'/Sprites/ui',destination=ROOT+'/Sprites/UI'),
 dict(source=ROOT+'/Sprites/character',destination=ROOT+'/Sprites/Player'),
 dict(source=ROOT+'/Sprites/props',destination=ROOT+'/Sprites/Tiles/Decoration/ranch'),
 dict(source=SOURCE+'/Fonts',destination=ROOT+'/Fonts'),
 dict(source=SOURCE+'/Prefabs',destination=ROOT+'/Prefabs'),
 dict(source=SOURCE+'/Scenes',destination=ROOT+'/Scenes'),
 dict(source=SOURCE+'/QaMaterials',destination=ROOT+'/Materials'),
 dict(source=SOURCE+'/Sequences',destination=ROOT+'/Data/AnimationSequences'),
 dict(source='Assets/ANIMOL/StarwayV04',destination=ROOT+'/Scripts'),
]
add(SOURCE+'/unity_import_manifest.json',ROOT+'/Data/art_import_manifest.json')
add(SOURCE+'/manifest.json',ROOT+'/Data/art_manifest.json')
renames={'StarwayFrames':'SpriteFrameSequence','StarwayVisual':'SpriteFramePlayer','StarwayHud':'PuzzleHudView','StarwayArtQa':'ArtQaController','StarwayV04Builder':'ArtAssetBuilder','StarwayV04FinalBatch':'ArtQaBatch','StarwayGameView':'PortraitGameView','StarwayV04Tests':'ArtAssetTests','StarwayV04PlayTests':'ArtQaPlayTests'}
for file in Path('Assets/ANIMOL/StarwayV04').rglob('*.cs'):
    name=renames[file.stem]+'.cs'
    add(str(file).replace('\\','/'),ROOT+'/Scripts/'+str(file.parent.relative_to('Assets/ANIMOL/StarwayV04')).replace('\\','/')+'/'+name)
for file in Path('Assets/ANIMOL/StarwayV04').rglob('*.asmdef'):
    definition=json.loads(file.read_text())
    new_name=definition['name'].replace('ANIMOL.StarwayV04.Tests','ANIMOL.Presentation.Tests.Editor') if '/Tests/Editor/' in str(file).replace('\\','/') else definition['name'].replace('ANIMOL.StarwayV04','ANIMOL.Presentation')
    add(str(file).replace('\\','/'),ROOT+'/Scripts/'+str(file.parent.relative_to('Assets/ANIMOL/StarwayV04')).replace('\\','/')+'/'+new_name+'.asmdef')
plan=dict(folders=folders,leaves=leaves,path_map=path_map,renames=renames,manifest=manifest,full_manifest=full_manifest)
(JOURNAL/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
(JOURNAL/'before.json').write_text(json.dumps({str(p).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in Path('Assets').rglob('*') if p.is_file()},indent=2))
(JOURNAL/'head-tree.z').write_bytes(subprocess.check_output(['git','ls-tree','-r','-z','HEAD']))
(JOURNAL/'base.txt').write_bytes(subprocess.check_output(['git','rev-parse','HEAD']))
(JOURNAL/'status.txt').write_bytes(subprocess.check_output(['git','status','--porcelain=v1','-uall']))
assert not subprocess.check_output(['git','diff','--cached','--name-only']).strip()
print('PNG moves',sum(p['source'].endswith('.png') for p in leaves),'clip moves',sum(p['source'].endswith('.anim') for p in leaves))
