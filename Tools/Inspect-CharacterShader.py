import sys, pathlib, json, ctypes
root = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / 'tools' / 'unitypy'))
import UnityPy
from UnityPy.export import ShaderConverter
from UnityPy.helpers import CompressionHelper
import struct
target_name = sys.argv[1] if len(sys.argv)>1 else 'Shader Graphs/CharacterShader'
folder = target_name.split('/')[-1].lower() + '-evidence' if len(sys.argv)>1 else 'character-shader-evidence'
out = root / 'HowToFish-Unity6' / 'CompilationChecks' / folder
out.mkdir(exist_ok=True)
counter = 0
original = ShaderConverter.ShaderSubProgram.Export
def export(program):
    global counter
    code = bytes(program.m_ProgramCode)
    index = code.find(b'DXBC')
    if index >= 0:
        blob = ctypes.c_void_p()
        dll = ctypes.WinDLL('d3dcompiler_47.dll')
        fn = dll.D3DDisassemble
        fn.argtypes = [ctypes.c_void_p,ctypes.c_size_t,ctypes.c_uint,ctypes.c_char_p,ctypes.POINTER(ctypes.c_void_p)]
        buffer = ctypes.create_string_buffer(code[index:])
        if fn(buffer,len(code)-index,0,None,ctypes.byref(blob)) == 0:
            table = ctypes.cast(blob,ctypes.POINTER(ctypes.POINTER(ctypes.c_void_p))).contents
            get_pointer = ctypes.WINFUNCTYPE(ctypes.c_void_p,ctypes.c_void_p)(table[3])
            get_size = ctypes.WINFUNCTYPE(ctypes.c_size_t,ctypes.c_void_p)(table[4])
            text = ctypes.string_at(get_pointer(blob),get_size(blob)).decode('utf8')
            (out / f'program-{counter}.txt').write_text(text,encoding='utf8')
            release = ctypes.WINFUNCTYPE(ctypes.c_uint,ctypes.c_void_p)(table[2]); release(blob)
        counter += 1
    return original(program)
ShaderConverter.ShaderSubProgram.Export = export
source_file = sys.argv[2] if len(sys.argv)>2 else 'sharedassets0.assets'
env = UnityPy.load(str(root / 'original-artifacts' / 'How to Fish_Data' / source_file))
for obj in env.objects:
    if obj.type.name != 'Shader': continue
    tree = obj.read_typetree()
    if tree.get('m_ParsedForm',{}).get('m_Name') != target_name: continue
    (out / 'parsed.json').write_text(json.dumps(tree['m_ParsedForm'],indent=2),encoding='utf8')
    (out / 'export.txt').write_text(obj.read().export(),encoding='utf8')
    for platform in range(len(tree['platforms'])):
        offsets = tree['offsets'][platform]
        lengths = tree['compressedLengths'][platform]
        sizes = tree['decompressedLengths'][platform]
        if not isinstance(offsets,list): offsets,lengths,sizes=[offsets],[lengths],[sizes]
        for offset,length,size in zip(offsets,lengths,sizes):
            data = CompressionHelper.decompress_lz4(bytes(tree['compressedBlob'][offset:offset+length]),size)
            start = 0
            while True:
                start = data.find(b'DXBC',start)
                if start < 0: break
                length = struct.unpack_from('<I',data,start+24)[0]
                class Program: pass
                program = Program(); program.m_ProgramCode=data[start:start+length]
                # Invoke the same disassembler without ShaderConverter's unsupported conversion.
                saved = original
                original = lambda p: ''
                export(program)
                original = saved
                start += length
    print('Programs:',counter)

