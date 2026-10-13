"""Read-only exact palette/alpha/native-shell audit. Visual candidates are not automatic art approval."""
import argparse,hashlib,json
from pathlib import Path
from PIL import Image

def audit(root):
 root=Path(root); catalog=json.loads((root/'Data/mechanism_catalog.json').read_text())
 palette={tuple(bytes.fromhex(s.strip())) for s in (root/'Data/palette.hex').read_text().splitlines() if s.strip()}
 errors=[];isolated=[];contour_blocks=[];seen=[]
 registered={e['file'] for e in catalog['entries']}
 actual_files={p.relative_to(root).as_posix() for p in (root/'Art/Mechanisms').glob('*.png')}
 if actual_files!=registered:errors.append(['mechanism file inventory','unregistered',sorted(actual_files-registered),'missing',sorted(registered-actual_files)])
 atlas=Image.open(root/catalog['atlas']).convert('RGBA')
 if atlas.size!=(catalog['atlasWidth'],catalog['atlasHeight']):errors.append(['mechanism atlas','size',atlas.size])
 for c in set(atlas.get_flattened_data()):
  if c[3] not in (0,255) or c[3] and c[:3] not in palette:errors.append(['mechanism atlas','palette/alpha',c])
 for e in catalog['entries']:
  p=root/e['file'];im=Image.open(p).convert('RGBA');px=im.load()
  r=e['atlasRect'];part=atlas.crop((r['x'],r['y'],r['x']+32,r['y']+32))
  if part.tobytes()!=im.tobytes():errors.append([e['file'],'atlas rect pixels differ'])
  if im.size!=(32,32):errors.append([e['file'],'size',im.size])
  for y in range(im.height):
   for x in range(im.width):
    c=px[x,y]
    if c[3] not in (0,255):errors.append([e['file'],'alpha',x,y,c[3]])
    if c[3] and c[:3] not in palette:errors.append([e['file'],'palette',x,y,c])
    if c[3] and not any(px[xx,yy][3] for yy in range(max(0,y-1),min(32,y+2)) for xx in range(max(0,x-1),min(32,x+2)) if (xx,yy)!=(x,y)):
     isolated.append([e['file'],x,y])
  # A 2x2 dark contour is a review candidate, not proof of a doubled line.
  ink=(26,28,44,255)
  for y in range(31):
   for x in range(31):
    if all(px[x+dx,y+dy]==ink for dx,dy in [(0,0),(1,0),(0,1),(1,1)]):contour_blocks.append([e['file'],x,y])
  seen.append(e['file'])
 for e in json.loads((root/'Validation/v4_source_preservation.json').read_text())['entries']:
  p=root/e['output'];actual=hashlib.sha256(p.read_bytes()).hexdigest()
  if actual!=e['sha256']:errors.append([e['output'],'v4 bytes changed'])
  im=Image.open(p).convert('RGBA')
  for c in set(im.get_flattened_data()):
   if c[3] not in (0,255) or c[3] and c[:3] not in palette:errors.append([e['output'],'v4 palette/alpha',c])
 report={'mechanismFrames':len(seen),'commonThemeSkins':catalog['commonThemeSkins'],'shellAtlases':80,'errors':errors,
  'isolatedOpaquePixelCandidates':len(isolated),'dark2x2ContourCandidates':len(contour_blocks),
  'isolatedCandidatePositions':isolated,'dark2x2Positions':contour_blocks,
  'scope':'Exact PNG palette, binary alpha, 32px mechanism extent, original v4 atlas hashes. Candidate counts do not certify every curve or material.',
  'unityCompilation':'not run in this environment'}
 (root/'Validation/pixel_audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
 print(json.dumps({k:v for k,v in report.items() if not k.endswith('Positions') and k!='errors'},ensure_ascii=False))
 if errors:raise SystemExit('pixel audit failed: '+str(errors[:5]))
 return report
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('package_root',nargs='?',default=str(Path(__file__).resolve().parents[1]));audit(parser.parse_args().package_root)
