import pathlib, json, hashlib, subprocess
root=pathlib.Path.cwd(); out=root/'Docs/Validation/TwilightSea';out.mkdir(parents=True,exist_ok=True)
files={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for folder in ['Assets','ProjectSettings','Packages'] for p in (root/folder).rglob('*') if p.is_file()}
assert not (out/'baseline.json').exists()
(out/'baseline.json').write_text(json.dumps({'head':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'files':files},indent=2))
(out/'git_status_before.txt').write_bytes(subprocess.check_output(['git','status','--short']))
print('Baseline saved before active edits:',len(files))
