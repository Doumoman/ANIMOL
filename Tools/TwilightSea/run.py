import subprocess,json,pathlib,sys
name=sys.argv[1];args=sys.argv[2:];r=subprocess.run(['unity',*args,'--format','json'],capture_output=True,text=True,encoding='utf8',errors='replace');print(r.stdout);p=pathlib.Path('Docs/Validation/TwilightSea')/(name+'.json');p.write_text(r.stdout,encoding='utf8');j=json.loads(r.stdout);assert r.returncode==0 and j['success'],r.stderr
if 'result' in j.get('data',{}) and isinstance(j['data']['result'],dict):assert j['data']['result'].get('success',True),j
