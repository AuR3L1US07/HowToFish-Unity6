from pathlib import Path
import json,re,collections
root=Path(r'E:\分析howtofish\recovered-unity-project\ExportedProject\Assets');out=root.parents[2]/'analysis'
a=json.loads((out/'all-character-resource-audit.json').read_text(encoding='utf-8'))
index={}
for p in root.rglob('*.meta'):
    m=re.search(r'^guid: ([0-9a-f]{32})',p.read_text(encoding='utf-8',errors='replace'),re.M)
    if m:index[m[1]]=str(Path(str(p)[:-5]).relative_to(root))
clothes=[];managers=[]
for i in a['skin_instances']:
    if i['script'].endswith('Clothes.cs'):
        entry={'path':i['path'],'meshes':{}}
        for field in ['_hatMesh','_accessoryMesh','outfitMesh']:
            m=re.search(r'^  '+field+r': \{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: \d+)?\}',i['serialized_fields'],re.M)
            if m:entry['meshes'][field]={'fileID':m[1],'guid':m[2],'path':index.get(m[2])}
        clothes.append(entry)
    if i['script'].endswith('SkinManager.cs'):
        s=i['serialized_fields'];fields={}
        for m in re.finditer(r'^  (_[A-Za-z]+): \{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: \d+)?\}',s,re.M):fields[m[1]]=index.get(m[3])
        ar=re.search(r'_allClothes:\n((?:  - .*\n)*)',s)
        managers.append({'path':i['path'],'fileID':i['fileID'],'all_clothes':[index.get(m[1]) for m in re.finditer(r'guid: ([0-9a-f]{32})',ar[1])] if ar else [],'npc_clothes':fields})
skinned=[x for x in a['objects'] if x['class_id']==137]
materials={m['material']['path']:m for x in skinned for m in x['materials']}
meshes=sorted({r['path'] for x in skinned for k,r in x['references'].items() if k=='m_Mesh' and r and r.get('path')}|{v['path'] for c in clothes for v in c['meshes'].values() if v.get('path')})
textures=sorted({t['path'] for m in materials.values() for t in m['textures'] if t and t.get('path')})
r={'scope':'All skinned renderers (includes creatures) and all serialized Clothes referenced by scenes/prefabs','clothes':clothes,'skin_managers':managers,'mesh_paths':meshes,'material_paths':sorted(materials),'texture_paths':textures,'dummy_shader_paths':sorted({m['shader']['path'] for m in materials.values() if m['dummy_shader'] and m.get('shader') and m['shader'].get('path')}),'skinned_renderers':skinned}
(out/'character-wardrobe-inventory.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
print('clothes',len(clothes),'meshes',len(meshes),'materials',len(materials),'textures',len(textures),'dummy shaders',len(r['dummy_shader_paths']))
for c in clothes:print(c['path'],{k:v['path'] for k,v in c['meshes'].items()})
