import pathlib,json,re,shutil
project=pathlib.Path(__file__).resolve().parents[1]
shaderdir=project/'Assets'/'Shader'
backup=project/'ReferenceOnly'/'BeforeRemainingShaderRepair'
backup.mkdir(exist_ok=True)
noise=r'''
float2 RecoveryNoiseDir(float2 p) { float x; Hash_Tchou_2_1_float(p,x); return normalize(float2(x-floor(x+.5),abs(x)-.5)); }
float RecoveryGradient(float2 p) { float2 ip=floor(p),fp=frac(p),f=fp*fp*fp*(fp*(fp*6-15)+10);
float a=dot(RecoveryNoiseDir(ip),fp),b=dot(RecoveryNoiseDir(ip+float2(0,1)),fp-float2(0,1));
float c=dot(RecoveryNoiseDir(ip+float2(1,0)),fp-float2(1,0)),d=dot(RecoveryNoiseDir(ip+1),fp-1);
return lerp(lerp(a,b,f.y),lerp(c,d,f.y),f.x)+.5; }
float RecoveryValue(float2 p) { float2 ip=floor(p),f=frac(p); f=f*f*(3-2*f); float a,b,c,d;
Hash_Tchou_2_1_float(ip,a); Hash_Tchou_2_1_float(ip+float2(1,0),b); Hash_Tchou_2_1_float(ip+float2(0,1),c); Hash_Tchou_2_1_float(ip+1,d);
return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y); }
float RecoverySimpleNoise(float2 uv) { return RecoveryValue(uv)*.125+RecoveryValue(uv*.5)*.25+RecoveryValue(uv*.25)*.5; }
'''
common=r'''
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1; float4 color:COLOR; };
struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; float3 objectPos:TEXCOORD3; float4 color:TEXCOORD4; float fog:TEXCOORD5; };
half3 RecoveryLight(float3 normal) { normal=normalize(normal); Light light=GetMainLight(); return max(SampleSH(normal)+light.color*(.2+.8*saturate(dot(normal,light.direction))),.25); }
'''
blend={0:'Zero',1:'One',2:'DstColor',3:'SrcColor',4:'OneMinusDstColor',5:'SrcAlpha',6:'OneMinusSrcColor',7:'DstAlpha',8:'OneMinusDstAlpha',9:'SrcAlphaSaturate',10:'OneMinusSrcAlpha'}
def state(name):
    tree=json.loads((project/'CompilationChecks'/f'{name.lower()}-evidence'/'parsed.json').read_text())
    sub=tree['m_SubShaders'][0]
    s=sub['m_Passes'][0]['m_State']
    tags=dict(sub['m_Tags']['tags'])
    def val(v,mapping): return '['+v['name']+']' if not v['name'].startswith('<') else mapping[int(v['val'])]
    return ('Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="'+tags.get('RenderType','Opaque')+'" "Queue"="'+tags.get('QUEUE','Geometry')+'" }\n'
        +'Blend '+val(s['rtBlend0']['srcBlend'],blend)+' '+val(s['rtBlend0']['destBlend'],blend)+'\n'
        +'ZWrite '+val(s['zWrite'],{0:'Off',1:'On'})+'\nCull '+val(s['culling'],{0:'Off',1:'Front',2:'Back'})+'\n')
def recover(name,decl,vertex,fragment):
    file=shaderdir/f'Shader Graphs_{name}.shader'
    old=file.read_text(encoding='utf8')
    if 'DummyShaderTextExporter' not in old: return
    shutil.copyfile(file,backup/file.name)
    head=old[:old.index('//DummyShaderTextExporter')]
    code=head+'// Recovery implementation based on original DXBC and render state; lighting remains reconstructed.\nSubShader {\n'+state(name)+'''Pass { Tags { "LightMode"="UniversalForward" }
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
'''+common+noise+decl+'''
V vert(A v) { V o; float3 world=TransformObjectToWorld(v.pos.xyz);
'''+vertex+'''
o.world=TransformObjectToWorld(v.pos.xyz); o.objectPos=v.pos.xyz; o.pos=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.color=v.color; o.fog=ComputeFogFactor(o.pos.z); return o; }
half4 frag(V i):SV_Target {
'''+fragment+'''
}
ENDHLSL
} } }
'''
    file.write_text(code,encoding='utf8')

recover('BettingBoxShader','float4 _Base_Color,_Smoothstep,_AlphaMap; float _Alpha_Speed,_WaveDensity,_Alpha_Clip,_WidthPixelation,_HeightPixelation;', '',r'''
float2 quantize=max(float2(_WidthPixelation,_HeightPixelation),.0001);
float2 coordinates=floor(float2((i.objectPos.x+i.objectPos.z)*_WaveDensity,(i.objectPos.y+1)*.5)*quantize)/quantize;
float wave=lerp(_AlphaMap.x,_AlphaMap.y,sin(coordinates.x+_Time.y*_Alpha_Speed));
float alpha=1-smoothstep(_Smoothstep.x,max(_Smoothstep.y,_Smoothstep.x+.0001),coordinates.y*wave);
clip(alpha-_Alpha_Clip); return half4(_Base_Color.rgb,alpha);
''')
recover('LaserSight','float4 _SmoothStep,_Color,_Emission; float _DistanceFromCenter;', '',r'''
float pattern=frac((i.world.x+i.world.z)*_DistanceFromCenter);
float alpha=smoothstep(_SmoothStep.x,max(_SmoothStep.y,_SmoothStep.x+.0001),max(pattern,1-pattern));
return half4(MixFog(_Color.rgb*RecoveryLight(i.normal)+_Emission.rgb,i.fog),alpha);
''')
recover('LineShader','', '',r'''
// Original surface has no material texture/tint parameters: use mesh interpolated RGB.
return half4(MixFog(i.color.rgb*RecoveryLight(i.normal),i.fog),1);
''')
lava_decl='float4 _Vertex_Speed,_Color_Speed,_Color_Speed_2,_DeepColor,Color_93e06cd551a5449091bcde90b46765a0,_Color_Smoothstep; float _Vertex_Noise_Scale,_Vertex_Height,_Pixelate,_Noise_Scale,_Noise_Scale_2,_WorldPositionNoiseMulti;'
lava_frag=r'''
float2 uv=_Pixelate>0?floor(i.uv*_Pixelate)/_Pixelate:i.uv;
float n1=RecoverySimpleNoise((uv+_Time.y*_Color_Speed.xy)*_Noise_Scale);
float n2=RecoverySimpleNoise((uv+_Time.y*_Color_Speed_2.xy)*_Noise_Scale_2);
float n=smoothstep(_Color_Smoothstep.x,max(_Color_Smoothstep.y,_Color_Smoothstep.x+.0001),(n1+n2)*.5);
half3 color=lerp(_DeepColor.rgb,Color_93e06cd551a5449091bcde90b46765a0.rgb,n);
return half4(MixFog(color*RecoveryLight(i.normal),i.fog),1);
'''
recover('LavaShader',lava_decl,'v.pos.xyz=float3(v.pos.x,RecoveryGradient((v.uv+_Time.y*_Vertex_Speed.xy)*_Vertex_Noise_Scale)*_Vertex_Height,v.pos.z);',lava_frag)
recover('LavaballShader',lava_decl,'v.pos.xyz+=v.normal*RecoveryGradient((v.pos.xy*_WorldPositionNoiseMulti+_Time.y*_Vertex_Speed.xy)*_Vertex_Noise_Scale)*_Vertex_Height;',lava_frag)
grass_decl='TEXTURE2D(_Texture); SAMPLER(sampler_Texture); TEXTURE2D(_Noise_Map); SAMPLER(sampler_Noise_Map); float4 _Color,_TopColor,_Noise_Map_Scale,_Noise_Remap; float _WindSpeed,_WindSpeed2,_WindStrength,_WindScale,_WindDensity,_WindColorMulti;'
grass_vertex=r'''
float noise=RecoveryGradient((world.xz+_Time.y*_WindSpeed2)*_WindDensity)+RecoveryGradient((world.xz-_Time.y*_WindSpeed)*_WindDensity)-.5;
world.xz+=noise*WIND_STRENGTH*v.pos.z;
v.pos.xyz=TransformWorldToObject(world);
'''
grass_frag=r'''
half4 texture=SAMPLE_TEXTURE2D(_Texture,sampler_Texture,i.uv);
half3 noise=SAMPLE_TEXTURE2D(_Noise_Map,sampler_Noise_Map,i.uv*_Noise_Map_Scale.xy).rgb;
noise=lerp(_Noise_Remap.x,_Noise_Remap.y,noise);
float wind=RecoveryGradient((i.world.xz+_Time.y*_WindSpeed2)*_WindDensity)+RecoveryGradient((i.world.xz-_Time.y*_WindSpeed)*_WindDensity)-.5;
half3 color=lerp(_Color.rgb,_TopColor.rgb,min(abs(i.objectPos.z*wind*_WindColorMulti),1))*texture.rgb*noise;
return half4(MixFog(color*RecoveryLight(i.normal),i.fog),1);
'''
recover('Grass',grass_decl,grass_vertex.replace('WIND_STRENGTH','_WindStrength'),grass_frag)
recover('WindyDecorations',grass_decl,grass_vertex.replace('WIND_STRENGTH','_WindScale'),'return half4(MixFog(SAMPLE_TEXTURE2D(_Texture,sampler_Texture,i.uv).rgb*RecoveryLight(i.normal),i.fog),1);')
recover('WindyDefault','TEXTURE2D(_Texture); SAMPLER(sampler_Texture); float _WindSpeed,_WindSpeed2,_WindDensity,_WindScale; float4 _WindFromOrigoSmoothstep;',r'''
float noise=RecoveryGradient((v.pos.xy+_Time.y*_WindSpeed2)*_WindDensity)+RecoveryGradient((v.pos.xy-_Time.y*_WindSpeed)*_WindDensity)-.5;
float weight=smoothstep(_WindFromOrigoSmoothstep.x,max(_WindFromOrigoSmoothstep.y,_WindFromOrigoSmoothstep.x+.0001),length(v.pos.xy));
v.pos.xy+=noise*_WindScale*weight;
''','return half4(MixFog(SAMPLE_TEXTURE2D(_Texture,sampler_Texture,i.uv).rgb*RecoveryLight(i.normal),i.fog),1);')

# Official package shader implements the same buffer writes seen in shipped DXBC.
shutil.copyfile(shaderdir/'Hidden_VoxelizeShader.shader',backup/'Hidden_VoxelizeShader.shader')
shutil.copyfile(project/'Packages/com.unity.visualeffectgraph/Shaders/SDFBaker/RayMapVoxelize.shader',shaderdir/'Hidden_VoxelizeShader.shader')
print('Recovered 8 graph shaders and official VFX voxelizer source.')
