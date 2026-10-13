import subprocess,json,sys,pathlib
p=json.loads(subprocess.check_output(['unity','command',*sys.argv[2:],'--format','json'],text=True,encoding='utf8'));out=pathlib.Path('Docs/Validation/LetterObservatory/Applied');(out/sys.argv[1]).write_text(json.dumps(p,indent=2))
r=(p.get('data') or {}).get('result',{});assert p['success'] and r.get('success',True),p;print(r.get('summary',r.get('groundTruth',r.get('result','saved'))))
