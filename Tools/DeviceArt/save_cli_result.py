import subprocess,json,sys,pathlib
p=json.loads(subprocess.check_output(['unity','command',*sys.argv[2:],'--format','json'],text=True,encoding='utf-8'))
dest=pathlib.Path('Docs/Validation/DeviceArt')/sys.argv[1];dest.write_text(json.dumps(p,indent=2),encoding='utf-8')
r=p.get('data',{}).get('result',{});print(r.get('summary',r.get('groundTruth',r.get('result',r.get('message','saved')))))
