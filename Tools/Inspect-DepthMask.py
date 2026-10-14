import sys, pathlib, json
root = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / 'tools' / 'unitypy'))
import UnityPy
env = UnityPy.load(str(root / 'original-artifacts' / 'How to Fish_Data' / 'sharedassets0.assets'))
for obj in env.objects:
    if obj.type.name == 'Shader' and obj.path_id == 525:
        tree = obj.read_typetree()
        path = root / 'HowToFish-Unity6' / 'CompilationChecks' / 'depth-mask-original-state.json'
        path.write_text(json.dumps(tree['m_ParsedForm'], indent=2), encoding='utf-8')
        print(path)
