import sys,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(root/'tools'/'unitypy'))
import UnityPy
for file in (root/'original-artifacts'/'How to Fish_Data').glob('level*'):
    if file.suffix: continue
    env=UnityPy.load(str(file))
    for obj in env.objects:
        if obj.type.name!='Shader':continue
        tree=obj.read_typetree()
        print(file.name,obj.path_id,tree.get('m_ParsedForm',{}).get('m_Name'),flush=True)
