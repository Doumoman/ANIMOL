"""Independent targeted source review, static only. No parser/compiler equivalence claim."""
from pathlib import Path
import argparse,hashlib,json,re
O=Path(__file__).resolve().parent
APPROVED_DESIGNS={'networks': [{'key': 'ivory_star_balustrade'}, {'key': 'brass_lattice_screen'}, {'key': 'archive_shelf_run'}, {'key': 'star_banner_partition'}, {'key': 'blue_crystal_planter'}, {'key': 'votive_light_channel'}], 'groups': [{'key': 'sanctum_altar_3x2', 'footprintCells': [3, 2]}, {'key': 'archive_worktable_3x1', 'footprintCells': [3, 1]}, {'key': 'ruined_colonnade_3x1', 'footprintCells': [3, 1]}, {'key': 'prayer_pew_3x1', 'footprintCells': [3, 1]}, {'key': 'celestial_chart_table_2x2', 'footprintCells': [2, 2]}, {'key': 'reliquary_case_2x1', 'footprintCells': [2, 1]}, {'key': 'folio_display_2x1', 'footprintCells': [2, 1]}, {'key': 'scroll_archive_2x2', 'footprintCells': [2, 2]}, {'key': 'banner_dais_2x1', 'footprintCells': [2, 1]}, {'key': 'faceted_armillary_2x2', 'footprintCells': [2, 2]}, {'key': 'ruined_mosaic_2x2', 'footprintCells': [2, 2]}, {'key': 'offering_steps_2x1', 'footprintCells': [2, 1]}], 'points': [{'key': 'stone_lantern', 'footprintCells': [1, 1]}, {'key': 'broken_pillar', 'footprintCells': [1, 1]}, {'key': 'book_pedestal', 'footprintCells': [1, 1]}, {'key': 'copper_fern', 'footprintCells': [1, 1]}, {'key': 'scribe_inkstand', 'footprintCells': [1, 1]}, {'key': 'votive_cluster', 'footprintCells': [1, 1]}, {'key': 'scroll_basket', 'footprintCells': [1, 1]}, {'key': 'star_boundary_marker', 'footprintCells': [1, 1]}]}

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--source',required=True);ap.add_argument('--report',help='Optional JSON output path; omitted writes no files.');a=ap.parse_args();r=Path(a.source).resolve()
 code={p.stem:p.read_text()for c in['Runtime','Editor','Tests~']for p in(r/c).glob('*.cs')}
 src=lambda suffix:code.get('SanctumConnectedDecorV01'+suffix,'')
 checks=[]
 def ck(label,yes,evidence):checks.append({'check':label,'passed':bool(yes),'sourceEvidence':evidence})
 p=src('Planner');b=src('Binding');c=src('Controller');s=src('BoardSnapshot');pub=src('PublishedSnapshotSource');e=src('BoardTools');j=src('BoardJson');m=src('JsonSchema');w=src('BoardWindow');ct=src('CatalogBuilder')
 ck('All19 primary sources present',len(code)==19,'10Runtime+6Editor+3Tests~ files')
 ck('All semantic names match approved contract',all(f'"{x["key"]}"'in p for cat in['networks','groups','points']for x in APPROVED_DESIGNS[cat]),'Planner known families+GroupKeys')
 ck('Connection domain uses family/group/layer/height',all(x in p for x in['new LayerCell(x, y, row.logicalLayer, row.heightKey)','neighbor.family == row.family && neighbor.connectionGroup == row.connectionGroup']),'Planner same predicate')
 ck('Whole footprint approved atomically',all(x in p for x in['for (int y = 0; y < size.height; y++) for (int x = 0; x < size.width; x++)','if (!hostAllowDecorCell(claim)) throw','groups.Add(new SanctumDecorGroupSelection(row, size))']),'Planner throws before returned plan on denied footprint')
 ck('Unique group instance identity',all(x in p for x in['placementIds.Add(row.placementId)','groups.Sort((a, b) => string.CompareOrdinal(a.placementId, b.placementId))']),'Planner placementID uniqueness')
 ck('Guarded deterministic optional posts',all(x in p for x in['mask4 != (N4 | S4) && mask4 != (E4 | W4)','StableHash(seed, 0, 0, layer, height, group)','CardinalMask(same, checked(row.x - dx), checked(row.y - dy)) == mask','CardinalMask(same, checked(row.x + dx), checked(row.y + dy)) == mask']),'Longstraight only with straight along-line neighbors')
 ck('Explicit row-down cell and whole-bottom-left conversion','checked(-row)'in p and 'checked(-topRow - heightCells + 1)'in p,'Planner conversion helpers')
 ck('DTO rejects missing arrays',all(x in s for x in['networkCells == null || groupPlacements == null || allowedDecorCells == null','!isWholeBoard','revision < 0']),'BoardSnapshot TryValidate')
 ck('Immutable captured eligibility whitelist','return claim => keys.Contains('in s and 'var keys = new HashSet<AllowedKey>()'in s,'BoardSnapshot query captures new set')
 ck('DTO deepcopies all three arrays',all(x in s for x in['new SanctumDecorNetworkCell[networkCells.Length]','new SanctumDecorGroupPlacement[groupPlacements.Length]','new SanctumDecorAllowedCell[allowedDecorCells.Length]']),'No shared row references')
 ck('Published source clones before storing/capture','var next = value.DeepCopy()'in pub and 'var copy = current.DeepCopy()'in pub,'Published defensive data ownership')
 ck('Published rejects inspection data','if (next.isExampleOnly)'in pub and 'Example-only data cannot be published'in pub,'Production source')
 ck('Published invalidates ondisable','protected override void OnDisable()'in pub and 'host must publish a fresh snapshot'in pub,'Pool reuse fresh authority')
 ck('Binding concrete configure and apply','controller.Configure('in b and 'controller.SetLayout('in b,'Actual call sites')
 ck('Binding event lifecycle',all(x in b for x in['.Changed += OnSourceChanged','.Changed -= OnSourceChanged','private void OnDisable() { Unsubscribe(); ClearVisuals(); }']),'Subscribe/capture/clear')
 ck('Hierarchy validated before idempotence cache',b.find('ValidateHierarchy(snapshot.cellSize')<b.find('if (lastAppliedHash == content')and 'lastAppliedWorldMatrix.Equals(worldMatrix)'in b and 'lastAppliedControllerActive == controller.isActiveAndEnabled'in b,'Parent matrix+visibility stamped')
 ck('Failure clears own required Controller','GetComponent<SanctumConnectedDecorV01Controller>()'in b and 'if (ownedController != null) ownedController.ClearLayout()'in b,'Inspector foreign/null reference safe')
 ck('Example explicitly opt-in binding','snapshot.isExampleOnly && !allowExampleSource'in b,'Example data cannot silently act as production snapshot')
 ck('Reentrant event refresh bounded','for (int attempt = 0; attempt < 3; attempt++)'in b and 'Snapshot source changed repeatedly during capture'in b,'No unbounded refresh loop')
 ck('Strict JSON validates before JsonUtility',j.find('ValidateJson(')<j.find('JsonUtility.FromJson')if 'ValidateJson('in j else j.find('new StrictReader')<j.find('JsonUtility.FromJson'),'BoardJson ordered structure checks (manual source review)')
 ck('Exact unicode digits in both parsers','NumberStyles.AllowHexSpecifier'in j and "digit >= '0' && digit <= '9'"in m,'No HexNumber whitespace acceptance')
 ck('Board builder calls catalog builder and validates current catalog','CatalogBuilder.BuildFromManifest('in e and 'CatalogBuilder.ValidateBuiltCatalog(catalog)'in e,'Actual Editor import path')
 ck('Board builder concrete source/binding setup',all(x in e for x in['root.AddComponent<SanctumConnectedDecorV01Controller>()','root.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>()','root.AddComponent<SanctumConnectedDecorV01Binding>()','binding.Bind(controller, catalog, pin, sortingLayer, source, out error, true)']),'Generated components genuinely connect')
 ck('Explicit refresh before prefab save',e.find('Require(binding.RefreshNow(out error), error)')<e.find('PrefabUtility.SaveAsPrefabAsset('),'Editmode not reliant on OnEnable')
 ck('Renderer visibility asserted before save',all(x in e for x in['renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null','rendered.Length == controller.OwnedVisualCount']),'Enabled current Sprite check')
 ck('Builder blocks open stage before mutation',e.find('PrefabStageUtility.GetCurrentPrefabStage() == null')<e.find('CatalogBuilder.BuildFromManifest('),'No stage mutation')
 ck('Builder checks actual SortingLayer','RequireSortingLayer(sortingLayer)'in e and 'Select an existing actual decoration Sorting Layer explicitly'in b,'No guessed sortinglayer')
 ck('Builder isolates additive scene and restores active',all(x in e for x in['NewSceneMode.Additive','if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous)','EditorSceneManager.CloseScene(inspection, true)']),'No production scene save/close')
 ck('Scoped output ownership labels','RequireOwnedOrAbsent(example.boardPath'in e and 'RequireOwnedOrAbsent(example.prefabPath'in e and 'RequireOwnedOrAbsent(example.scenePath'in e,'Do not overwrite unrelated assets')
 ck('Optional URP reflection avoids harddependency','assembly.GetType("UnityEngine.Rendering.Universal.Light2D", false)'in e and 'using UnityEngine.Rendering.Universal'not in e,'Only isolated inspection light')
 ck('No autonomous polling','void Update('not in'\n'.join(code.values())and 'void Start('not in'\n'.join(code.values()),'Whole snapshot event driven')
 ck('No gameplay collider/input creation',not re.search(r'AddComponent<(?:BoxCollider2D|Rigidbody2D|PlayerInput)>','\n'.join(code.values())),'Visual decoration scope')
 ck('Root importer validates exact manifest file hash','string.Equals(actual, pin, StringComparison.OrdinalIgnoreCase)'in ct and 'ValidateSourcePng(root, row)'in ct,'Exact current files')
 ck('Catalog rejects alternate art revisions even without Binding', 'string.Equals(expectedManifestSha256, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, StringComparison.OrdinalIgnoreCase)'in src('Catalog'),'Catalog runtime compiled art pin')
 ck('Importer rejects alternate art manifest pin','string.Equals(pin, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, StringComparison.OrdinalIgnoreCase)'in ct,'Importer compiled art pin')
 report={'status':'PASS_SOURCE_REVIEW'if all(x['passed']for x in checks)else'FINDINGS_SOURCE_REVIEW','sourceRoot':str(r),'checkCount':len(checks),'checks':checks,'sourceFiles':[{'file':str(p.relative_to(r)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}for folder in['Runtime','Editor','Tests~']for p in sorted((r/folder).glob('*.cs'))],'scope':'Independent static review: source evidence for lifecycle/authority/JSON/builder contracts. It does not execute C# or validate Unity API availability.','actualCSharpCompile':'NOT_RUN','actualUnityImport':'NOT_RUN','actualTestRunner':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
 if a.report:Path(a.report).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({'status':report['status'],'checks':len(checks),'failed':[x['check']for x in checks if not x['passed']]}))
if __name__=='__main__':main()
