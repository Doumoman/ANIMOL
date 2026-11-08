import json,pathlib,re,sys
root=pathlib.Path.cwd()
backup=pathlib.Path((root/'Temp/maphud-backup-path.txt').read_text(encoding='utf-8-sig').strip())
fields=['m_LocalRotation','m_LocalPosition','m_LocalScale','m_AnchorMin','m_AnchorMax','m_AnchoredPosition','m_SizeDelta','m_Pivot']
def parse(path):
    return {i:(int(t),s) for t,i,s in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=\n--- |\Z)',path.read_text(),re.S)}
def field(s,k):
    return re.search(r'^  '+k+r': (.*)$',s,re.M).group(1)
def numeric_field(s,k):
    return tuple(float(value) for value in re.findall(r'[xyzw]: ([^,}]+)',field(s,k)))
records=[]
serialized=[]
for asset in ['Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab','Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab']:
    old=parse(backup/asset); now=parse(root/asset)
    def path(i):
        s=old[i][1]; go=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',s).group(1)
        parent=re.search(r'm_Father: \{fileID: (-?\d+)\}',s).group(1)
        return (path(parent)+'/' if parent!='0' else '')+field(old[go][1],'m_Name')
    for i,(kind,s) in old.items():
        if kind==114:
            for key,component in [('m_ScaleFactor','CanvasScaler'),('m_TextStyleHashCode','TMP_Text')]:
                found=re.search(r'^  '+key+r': (.*)$',s,re.M)
                if not found or (field(s,key)==field(now[i][1],key) and '--all' not in sys.argv): continue
                go=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',s).group(1)
                transform=re.search(r'm_Component:\n  - component: \{fileID: (-?\d+)\}',old[go][1]).group(1)
                object_path=path(transform)
                serialized.append({'asset':asset,'path':object_path.split('/',1)[1] if '/' in object_path else '',
                                   'component':component,'field':key,'value':float(found.group(1))})
        if kind!=224: continue
        assert i in now, ('Original RectTransform missing',asset,i)
        different=[k for k in fields if numeric_field(s,k)!=numeric_field(now[i][1],k)]
        if different or '--all' in sys.argv:
            vector=lambda k: {key:float(value) for key,value in re.findall(r'([xyzw]): ([^,}]+)',field(s,k))}
            records.append({'asset':asset,'path':path(i).split('/',1)[1] if '/' in path(i) else '',
                            'rotation':vector('m_LocalRotation'),'position':vector('m_LocalPosition'),'scale':vector('m_LocalScale'),
                            'anchorMin':vector('m_AnchorMin'),'anchorMax':vector('m_AnchorMax'),'size':vector('m_SizeDelta'),
                            'pivot':vector('m_Pivot'),'anchored':vector('m_AnchoredPosition'),'fileId':i})
(root/'Temp/maphud-geometry-restore.json').write_text(json.dumps({'entries':records,'serializedFields':serialized},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'preparedOriginalRectangles' if '--all' in sys.argv else 'changedAutoLayoutRectangles':len(records),
                  'preparedOriginalEditorFields' if '--all' in sys.argv else 'normalizedEditorFields':len(serialized)},ensure_ascii=False))
