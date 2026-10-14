from pathlib import Path
import re,json,hashlib
root=Path(r'E:\分析howtofish\recovered-unity-project\ExportedProject\Assets')
out=Path(r'E:\分析howtofish\analysis')
guid_index={}
for p in root.rglob('*.meta'):
    m=re.search(r'^guid: ([0-9a-f]{32})',p.read_text(encoding='utf-8',errors='replace'),re.M)
    if m:guid_index[m[1]]=Path(str(p)[:-5])
cache={}
def load(p):
    if p in cache:return cache[p]
    b=p.read_bytes(); t=b.decode('utf-8',errors='replace') if b.startswith(b'%YAML') else ''
    blocks={}
    for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\s*\n(.*?)(?=^--- !u!|\Z)',t,re.M|re.S):
        blocks[m[2]]={'class_id':int(m[1]),'text':m[3],'line':t[:m.start()].count('\n')+1}
    cache[p]=(t,blocks);return t,blocks
def refs(t):
    return list(re.finditer(r'\{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: (\d+))?\}',t))
def ref_value(s):
    m=re.search(r'\{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: (\d+))?\}',s)
    if not m:return None
    f,g=m[1],m[2];result={'fileID':f,'guid':g}
    if f=='0':result['status']='null'
    elif g and g.startswith('0000000000000000'):result['status']='Unity built-in'
    elif g:
        p=guid_index.get(g);result['path']=str(p.relative_to(root)) if p else None
        result['status']='missing GUID' if not p else ('missing asset' if not p.exists() else 'asset exists')
        if p and p.exists():
            _,b=load(p)
            if b:result['fileID_exists']=f in b
    else:result['status']='local'
    return result
seeds=[root/'Resources/other/PlayerHolder.prefab',root/'Resources/other/PlayerHolderBackup.prefab',root/'Resources/items/other/DeadPlayer.prefab']
result={'scope':'Static exported Unity YAML audit, no runtime equivalence claim','seeds':[]}
for seed in seeds:
    t,blocks=load(seed);names={}
    for fid,b in blocks.items():
        if b['class_id']==1:
            n=re.search(r'^  m_Name: (.*)$',b['text'],re.M);names[fid]=n[1] if n else ''
    record={'path':str(seed.relative_to(root)),'sha256':hashlib.sha256(seed.read_bytes()).hexdigest(),'skinned_renderers':[],'animators':[],'missing_local_refs':[],'missing_external_refs':[],'invalid_external_fileIDs':[],'reachable_assets':[],'character_materials':[]}
    for fid,b in blocks.items():
        s=b['text'];c=b['class_id']
        if c in [137,95]:
            go=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',s);entry={'fileID':fid,'line':b['line'],'name':names.get(go[1] if go else ''),'references':{}}
            for field in (['m_Mesh','m_RootBone'] if c==137 else ['m_Avatar','m_Controller']):
                m=re.search(r'^  '+field+r': (.*)$',s,re.M)
                if m:entry['references'][field]=ref_value(m[1])
            if c==137:
                for field in ['m_Bones','m_Materials']:
                    m=re.search(r'^  '+field+r':\s*\n((?:  - .*\n)*)',s,re.M)
                    entry[field]=[ref_value(r[0]) for r in refs(m[1])] if m else []
                for mat in entry['m_Materials']:
                    if mat and mat.get('path'):
                        mp=root/mat['path']; mt,_=load(mp)
                        shader=re.search(r'm_Shader: (.*)',mt)
                        sr=ref_value(shader[1]) if shader else None
                        st=(root/sr['path']).read_text(encoding='utf-8',errors='replace') if sr and sr.get('path') else ''
                        record['character_materials'].append({'renderer':entry['name'],'material':mat,'shader':sr,'shader_stub_markers':[x for x in ['Dummy','dummy','Fallback','AssetRipper'] if x in st],'textures':[ref_value(m[0]) for m in re.finditer(r'm_Texture: (\{[^}]*\})',mt)]})
            record['skinned_renderers' if c==137 else 'animators'].append(entry)
        for m in refs(s):
            if m[1]!='0' and not m[2] and m[1] not in blocks:record['missing_local_refs'].append({'sourceID':fid,'reference':m[0]})
    todo=[seed];seen=set()
    while todo:
        p=todo.pop()
        if p in seen:continue
        seen.add(p);s,_=load(p)
        for m in refs(s):
            if not m[2] or m[1]=='0' or m[2].startswith('0000000000000000'):continue
            target=guid_index.get(m[2]);r=ref_value(m[0])
            if not target or not target.exists():record['missing_external_refs'].append({'source':str(p.relative_to(root)),**r})
            else:
                if r.get('fileID_exists') is False:record['invalid_external_fileIDs'].append({'source':str(p.relative_to(root)),**r})
                if target not in seen:todo.append(target)
    for p in sorted(seen):
        record['reachable_assets'].append({'path':str(p.relative_to(root)),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
    result['seeds'].append(record)
(out/'character-resource-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
for r in result['seeds']:
    print(r['path'],len(r['skinned_renderers']),len(r['animators']),len(r['reachable_assets']),'missing local',len(r['missing_local_refs']),'missing external',len(r['missing_external_refs']))
    for s in r['skinned_renderers']:print(s['name'],s['references']['m_Mesh'],len(s['m_Bones']))
