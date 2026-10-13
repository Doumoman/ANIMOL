from pathlib import Path
import json
import shutil
import subprocess

plan=json.loads(Path('C:/Users/user/AppData/Local/Temp/animol-sprite-layout/plan.json').read_text())
code=Path('Tools/finish_active_slice.cs').read_text()
for source,target in plan['renames'].items():code=code.replace(source,target)
code=code.replace('ANIMOL.StarwayV04','ANIMOL.Presentation').replace('Assets/ANIMOL/Art/StarwayPasture/V04','Assets/ANIMOL').replace('Screenshots/ActiveSlice_20261013','Screenshots/ArtQA')
Path('Tools/finish_sprite_layout.cs').write_text(code)
archive=Path('Legacy/PreviousProject_20261013/Tools/IsolationUtilities')
archive.mkdir(parents=True,exist_ok=True)
for name in ['isolate_previous_project.cs','inspect_active_slice.cs','verify_active_slice.py','stage_active_slice.py','check_active_scene.cs','prepare_active_slice_playtests.cs','finish_active_slice.cs']:
    target=archive/name
    assert not target.exists()
    shutil.move(str(Path('Tools')/name),str(target))
root=Path('Tools/SpriteLayout')
root.mkdir(exist_ok=True)
for path in list(Path('Tools').glob('*')):
    if path.is_file():shutil.move(str(path),str(root/path.name))
output=json.loads(subprocess.check_output(['unity','command','eval_file','--file','Tools/SpriteLayout/finish_sprite_layout.cs','--timeout','60000','--format','json']))
assert output['data']['result']['success']
result=output['data']['result']['result']
Path('Docs/Validation/SpriteLayout/scene_check.json').write_text(json.dumps(result,indent=2))
print('Missing scripts/sprites',result['missingScripts'],result['missingSprites'])
output=json.loads(subprocess.check_output(['unity','command','console_status','--format','json']))
Path('Docs/Validation/SpriteLayout/console.json').write_text(json.dumps(output['data']['result'],indent=2))
print('Compilation failed',output['data']['result']['groundTruth']['compilationFailed'])
