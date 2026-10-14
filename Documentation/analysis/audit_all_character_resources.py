import audit_character_resources as base
from pathlib import Path
import re,json,collections
root,out=base.root,base.out
scopes=sorted([*root.rglob('*.prefab'),*root.rglob('*.unity')])
report={'scope':'All exported prefabs and scenes; static serialized-reference validation only','prefabs':sum(p.suffix=='.prefab' for p in scopes),'scenes':sum(p.suffix=='.unity' for p in scopes),'objects':[],'skin_instances':[],'files':[],'missing_local_refs':[],'missing_external_refs':[],'invalid_external_fileIDs':[],'unverified_imported_fileIDs':[]}
all_reachable=set();todo=scopes.copy()
while todo:
    p=todo.pop()
    if p in all_reachable:continue
    all_reachable.add(p);t,blocks=base.load(p)
    names={}
    for fid,b in blocks.items():
        if b['class_id']==1:
            m=re.search(r'^  m_Name: (.*)$',b['text'],re.M);names[fid]=m[1] if m else ''
    filerec={'path':str(p.relative_to(root)),'class_counts':dict(collections.Counter(str(b['class_id']) for b in blocks.values())),'reference_count':0}
    is_scope=p in scopes
    filters={}
    for fid,b in blocks.items():
        if b['class_id']==33:
            go=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b['text']);m=re.search(r'm_Mesh: (.*)',b['text'])
            if go and m:filters[go[1]]=base.ref_value(m[1])
    for fid,b in blocks.items():
        s=b['text'];c=b['class_id'];go=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',s)
        if is_scope and c in [23,137,95,111]:
            entry={'path':str(p.relative_to(root)),'fileID':fid,'class_id':c,'line':b['line'],'name':names.get(go[1] if go else ''),'references':{}}
            for field in ['m_Mesh','m_RootBone','m_Avatar','m_Controller','m_Animation']:
                m=re.search(r'^  '+field+r': (.*)$',s,re.M)
                if m:entry['references'][field]=base.ref_value(m[1])
            if c==23 and go and go[1] in filters:entry['references']['MeshFilter.m_Mesh']=filters[go[1]]
            for field in ['m_Bones','m_Materials','m_Animations']:
                m=re.search(r'^  '+field+r':\s*\n((?:  - .*\n)*)',s,re.M)
                if m:entry[field]=[base.ref_value(r[0]) for r in base.refs(m[1])]
            entry['materials']=[]
            for mat in entry.get('m_Materials',[]):
                if not mat or not mat.get('path'):continue
                mt,_=base.load(root/mat['path']);sm=re.search(r'm_Shader: (.*)',mt);sr=base.ref_value(sm[1]) if sm else None
                st=(root/sr['path']).read_text(encoding='utf-8',errors='replace') if sr and sr.get('path') else ''
                entry['materials'].append({'material':mat,'shader':sr,'dummy_shader':'DummyShaderTextExporter' in st,'textures':[base.ref_value(m[0]) for m in re.finditer(r'm_Texture: (\{[^}]*\})',mt)]})
            report['objects'].append(entry)
        if c==114:
            sm=re.search(r'm_Script: (.*)',s);sr=base.ref_value(sm[1]) if sm else None
            if sr and sr.get('path') and Path(sr['path']).stem in ['SkinManager','SkinPreset','Clothes','PlayerSkin','DeadPlayer']:
                report['skin_instances'].append({'path':str(p.relative_to(root)),'fileID':fid,'line':b['line'],'name':names.get(go[1] if go else ''),'script':sr['path'],'serialized_fields':s})
        for m in base.refs(s):
            filerec['reference_count']+=1;f,g=m[1],m[2]
            if f=='0':continue
            src={'path':str(p.relative_to(root)),'sourceID':fid,'reference':m[0]}
            if not g:
                if f not in blocks:report['missing_local_refs'].append(src)
                continue
            if g.startswith('0000000000000000'):continue
            target=base.guid_index.get(g)
            if not target or not target.exists():report['missing_external_refs'].append(src);continue
            _,tb=base.load(target)
            if tb and f not in tb:report['invalid_external_fileIDs'].append({**src,'target':str(target.relative_to(root))})
            elif not tb:report['unverified_imported_fileIDs'].append({**src,'target':str(target.relative_to(root))})
            if target not in all_reachable:todo.append(target)
    report['files'].append(filerec)
report['summary']={'reachable_files':len(all_reachable),'renderer_counts':dict(collections.Counter(str(x['class_id']) for x in report['objects'])),'skin_instances':len(report['skin_instances']),'missing_local_refs':len(report['missing_local_refs']),'missing_external_refs':len(report['missing_external_refs']),'invalid_external_fileIDs':len(report['invalid_external_fileIDs']),'unverified_imported_fileID_reference_count':len(report['unverified_imported_fileIDs'])}
(out/'all-character-resource-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report['summary'],ensure_ascii=False))
print('prefabs',report['prefabs'],'scenes',report['scenes'])
