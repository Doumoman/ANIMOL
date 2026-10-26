"""Independent lexical/source-contract review; never claims to compile or run C#."""
from pathlib import Path
import hashlib,json,re,shutil

W=Path(__file__).resolve().parents[1];ROOT=W/'runtime_exports';OUT=Path(__file__).resolve().parent
files=sorted(ROOT.rglob('*.cs'));source={p.relative_to(ROOT).as_posix():p.read_text()for p in files}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def strip_literals(text):
 # Drop comments, regular/verbatim strings and character literals for delimiter checks.
 token=re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')
 return token.sub(lambda m:'\n'*m.group().count('\n'),text)
def balanced(text):
 stack=[];pairs={'}':'{',')':'(',']':'['}
 for c in strip_literals(text):
  if c in '{([':stack.append(c)
  elif c in '})]':
   if not stack or stack.pop()!=pairs[c]:return False
 return not stack
def evidence(file,token):
 text=source[file];at=text.find(token);return {'file':file,'line':text[:at].count('\n')+1,'token':token}
C='Runtime/RanchConnectedDecorV01Controller.cs';P='Runtime/RanchConnectedDecorV01Planner.cs';K='Runtime/RanchConnectedDecorV01Catalog.cs'
B='Editor/RanchConnectedDecorV01CatalogBuilder.cs';I='Editor/RanchConnectedDecorV01SpriteImporter.cs';J='Editor/RanchConnectedDecorV01JsonSchema.cs'
checks=[]
def check(id,description,condition,e=[]):checks.append({'id':id,'description':description,'result':'PASS_SOURCE_REVIEW_ONLY'if condition else'FAIL_SOURCE_REVIEW','evidence':e})
check('S01','All delivered C# delimiters and preprocessor guards are lexically balanced. This is not compilation.',
      all(balanced(t)and len(re.findall(r'^#if\b',t,re.M))==len(re.findall(r'^#endif\b',t,re.M))for t in source.values()))
check('S02','Runtime and Editor files use the new versioned namespace; optional Tests~ references it.',
      all('namespace ANIMOL.Art.RanchConnectedDecorV01'in t for f,t in source.items()if not f.startswith('Tests~'))
      and all('using ANIMOL.Art.RanchConnectedDecorV01;'in t for f,t in source.items()if f.startswith('Tests~')))
check('S03','Catalog has current package/schema/root GUID/Sprite GUID/fileID/SHA/metadata fingerprint.',
      all(x in source[K]for x in ['ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01','sourceRootGuid','assetGuid','localFileId','ComputeFingerprint()','sourceManifestSha256']),
      [evidence(K,'ValidateIntegrity('),evidence(K,'ComputeFingerprint()')])
check('S04','Builder checks exact current manifest bytes against manifest_sha256.txt before source preflight or imports.',
      all(x in source[B]for x in ['RanchConnectedDecorV01JsonSchema.Validate(json)','Current manifest bytes differ from manifest_sha256.txt.','ValidateSourcePng(root, row)','ValidateImporter(path, row)']),
      [evidence(B,'LoadManifest('),evidence(B,'Current manifest bytes differ from manifest_sha256.txt.')])
check('S05','Builder validates actual Sprite/GUID/fileID/source-root identity and saves only the new catalog asset.',
      all(x in source[B]for x in ['TryGetGUIDAndLocalFileIdentifier','AssetDatabase.AssetPathToGUID(root) == catalog.SourceRootGuid','SaveAssetIfDirty(output)'])
      and not re.search(r'AssetDatabase\.SaveAssets\s*\(',strip_literals(source[B])))
check('S06','Importer is restricted to exact declared addon PNG paths; missing/unlisted files stay Default and errors have no legacy Sprite fallback.',
      all(x in source[I]for x in ['RanchConnectedDecorV01Catalog.PackageId','ExactRows(root).TryGetValue(relative, out row)','TextureImporterType.Default','ValidateSourcePng(root, row)'])
      and not re.search(r'AssetDatabase\.(?:Refresh|ImportAsset|SaveAssets)\s*\(',strip_literals(source[I])))
check('S07','Native Single/FullRect/PPU256/custom pivot and no mip/compression/NPOT scaling/automatic physics are explicit.',
      all(x in source[I]for x in ['SpriteImportMode.Single','row.ppu','settings.spritePivot','SpriteMeshType.FullRect','spriteGenerateFallbackPhysicsShape = false','TextureImporterNPOTScale.None','ClearPlatformTextureSettings']))
check('S08','JSON schema rejects duplicate fields and missing required dimensions/ground/footprint/family/kind entries.',
      all(x in source[J]for x in ['Duplicate JSON property:','logicalRect','footprintCells','family','kind','root.fields["groups"]'] ))
check('S09','Cardinal4 and raw8-neighbor selectors use separate exact bits/paths and full-board same family/group/layer/height matching.',
      all(x in source[P]for x in ['N4 = 1, E4 = 2, S4 = 4, W4 = 8','N8 = 1, NE8 = 2, E8 = 4','D3','D2',
       'new LayerCell(x, y, row.logicalLayer, row.heightKey)','neighbor.family == row.family && neighbor.connectionGroup == row.connectionGroup']))
check('S10','Every multi-cell footprint member is validated into a local plan, duplicate IDs/claims or rejected cells fail the complete snapshot.',
      all(x in source[P]for x in ['for (int y = 0; y < size.height; y++) for (int x = 0; x < size.width; x++)',
       '!placementIds.Add(row.placementId)','!occupied.Add(key)','hostAllowDecorCell(claim)','no partial group may be placed.']))
check('S11','Groups preserve one anchor and one renderer; no canvas/alpha/footprint fit to1cell.',
      'WorldPosition(worldOrigin, row.CenterX, row.CenterY' in source[C]
      and 'foreach (PendingSprite row in pending)'in source[C]
      and 'cellSize / parentScale.x, cellSize / parentScale.y' in source[C]
      and 'widthCells * .5'in source[P]and'heightCells * .5'in source[P])
check('S12','Old owned sprites are hidden and cleared before deferred Destroy; empty snapshots replace with an empty owned set.',
      'renderer.enabled = false; renderer.sprite = null;'in source[C]
      and'ClearOwned(); owned = next;'in source[C]
      and'if (Application.isPlaying) UnityEngine.Object.Destroy(node); else UnityEngine.Object.DestroyImmediate(node);'in source[C])
check('S13','Null/new Configure attempts clear prior owned visuals and persist attempted config before validation, so stale art is not restored.',
      source[C].index('ClearOwned(); configured = false;')<source[C].index('catalog = latest;')
      and'else { configured = false; hasExplicitLayout = false; currentPlan = null; }'in source[C])
check('S14','Inactive pre-Awake Configure+SetLayout is intentionally preserved; mere disabled/re-enabled objects cannot resurrect old layout.',
      all(x in source[C]for x in ['private void Awake() { if (!hasExplicitLayout) HideOwned(); }',
       'configured && hasExplicitLayout && catalog != null','configured = false; hasExplicitLayout = false; currentPlan = null; HideOwned();']))
check('S15','Periodic posts require straight nodes and both along-line neighbours straight; phase is stable world-coordinate3/4, not frame Random.',
      all(x in source[P]for x in ['spacing != 3 && spacing != 4','mask4 != (N4 | S4) && mask4 != (E4 | W4)',
       'CardinalMask(same, checked(row.x - dx), checked(row.y - dy)) == mask',
       'CardinalMask(same, checked(row.x + dx), checked(row.y + dy)) == mask'])
      and'Random.'not in strip_literals(source[P]))
meadow=W/'meadow_exports/meadow_manifest.json';accent_rows=[]
if meadow.exists():accent_rows=[a for a in json.loads(meadow.read_text())['assets']if a.get('kind')=='meadow_flower_accent']
check('S16','Producer flower family/kind matches controller; accents sorted by exact path and selected by stable hash.',
      len(accent_rows)>0 and all(a['family']=='raised_flower_meadow'and a['footprintCells']==[1,1]for a in accent_rows)
      and'row.Kind == "meadow_flower_accent"'in source[C]and'accents.Sort('in source[C])
check('S17','Controller/Planner never create colliders, change game transforms, poll input or advance gameplay in Update.',
      not re.search(r'\b(?:GameObject\.Find|Input\.|Random\.|AddComponent\s*<\s*(?:BoxCollider|PolygonCollider|Collider|Rigidbody)|void\s+(?:Start|Update|FixedUpdate)\s*\()',strip_literals(source[C]+source[P])))
check('S18','Row-down cell/group helpers and top-left origin adapter are explicit and arithmetically consistent.',
      all(x in source[P]for x in ['new RanchDecorCell(column, checked(-row))','checked(-topRow - heightCells + 1)'])
      and'boardTopLeft.y - cellSize'in source[C])
check('S19','Explicit catalog builds invalidate importer row cache before any PNG reimport, including copies retaining mtime/length.',
      'public static void InvalidateRoot(string root) { ByRoot.Remove(root); }'in source[I]
      and source[B].index('RanchConnectedDecorV01SpriteImporter.InvalidateRoot(root)')<source[B].index('AssetDatabase.ImportAsset(path,'))

testfile=next((f for f in source if f.startswith('Tests~')and f.endswith('PlannerTests.cs')),None)
tests=re.findall(r'\[Test\]\s+public void (\w+)\(',source.get(testfile,''))
compiler={n:shutil.which(n)for n in ['Unity','dotnet','csc','mcs']}
fail=[c for c in checks if c['result'].startswith('FAIL')]
report={'status':'PASS_SOURCE_REVIEW_ONLY'if not fail else'FAIL_SOURCE_REVIEW','packageId':'ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01',
 'scope':'Independent reading plus lexical/pattern checks of delivered source. No C# compile, Unity import, tests, scene loading, controller execution or host gameplay was run.',
 'files':[{'file':p.relative_to(ROOT).as_posix(),'sha256':sha(p),'lines':len(p.read_text().splitlines())}for p in files],
 'sourceFilesReviewed':len(files),'checks':checks,'checkCount':len(checks),'openFindings':fail,'resolvedFindings':[],
 'manualArithmeticExamples':[{'hostTopLeftY':10,'cellSize':1,'row':4,'worldOriginY':9,'nativeCellCenterY':5.5},
  {'hostTopLeftY':10,'cellSize':1,'groupTopRow':4,'heightCells':2,'convertedBottomLeftY':-5,'nativeGroupCenterY':5}],
 'optionalNUnitSource':{'file':testfile,'testMethods':tests,'count':len(tests),'execution':'NOT_RUN','reason':'No Unity/dotnet/csc/mcs compiler installed and no actual project/test assembly supplied.'},
 'compilerAvailability':compiler,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualNUnit':'NOT_RUN','actualPlayMode':'NOT_RUN','actualSceneBindings':'NOT_RUN'}
(OUT/'source_review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'status':report['status'],'files':len(files),'sourceChecks':len(checks),'openFindings':len(fail),'optionalNUnitMethodsNotRun':len(tests)}))
