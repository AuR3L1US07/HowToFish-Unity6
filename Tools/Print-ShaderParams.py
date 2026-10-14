import json,sys,pathlib
data=json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf8'))
for s in data['m_SubShaders'][:1]:
    for p in s['m_Passes'][:1]:
        names={v:k for k,v in p['m_NameIndices']}
        for stage in ('progVertex','progFragment'):
            print(stage)
            programs=p[stage]['m_SubPrograms']
            if not programs: programs=[program for group in p[stage].get('m_PlayerSubPrograms',[]) for program in group]
            if not programs: continue
            stage_data=p[stage]
            buffers=stage_data.get('m_CommonParameters',{}).get('m_ConstantBuffers',[]) or programs[0]['m_ConstantBuffers']
            for cb in buffers:
                if names.get(cb['m_NameIndex'])!='UnityPerMaterial':continue
                for v in cb['m_VectorParams']:
                    print(v['m_Index']//16,'xyzw'[(v['m_Index']%16)//4],names.get(v['m_NameIndex']))
