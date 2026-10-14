#ifndef RECOVERY_ITEM_SURFACE_INCLUDED
#define RECOVERY_ITEM_SURFACE_INCLUDED
// Material behavior reconstructed from the original Default/Boat DXBC.
TEXTURE2D(_Colors); SAMPLER(sampler_Colors);
TEXTURE2D(_Normal_Map); SAMPLER(sampler_Normal_Map);
float _Use_Skin, _Rainbow_Skin, _Metallic_Cutoff, _Skin_Noise_Scale, _UV_Rotation, _Noise_Rotation;
float _MetallicMetallicness, _MetallicSmoothness, _PlasticMetallicness, _PlasticSmoothness;
float _Normal_Strength, _Normal_Scale, _Emission;
float2 _Color_Offset, _Color_Offset_2, _Skin_Smooth_Step, _Skin_UV_Scale;
#ifdef RECOVERY_COOKABLE
float _Cookness, _Cooked_Normal_Strength;
float4 _CookColor, _BurntColor;
#endif
float2 RecoveryGradient(float2 cell)
{
    // Original integer lattice hash constants (DXBC xor/imul/ushr sequence).
    uint2 c = (uint2)(int2)cell;
    uint h = c.y ^ 0x41c64e6du;
    h *= h + c.x; h ^= h >> 5; h *= 0x27d4eb2du; h >>= 8;
    float v = (float)h / 16777215.0;
    return normalize(float2(v-floor(v+.5),abs(v)-.5));
}
float RecoveryGradientNoise(float2 uv)
{
    float2 cell=floor(uv), f=frac(uv), blend=f*f*f*(f*(f*6-15)+10);
    return lerp(lerp(dot(RecoveryGradient(cell),f),dot(RecoveryGradient(cell+float2(0,1)),f-float2(0,1)),blend.y),
                lerp(dot(RecoveryGradient(cell+float2(1,0)),f-float2(1,0)),dot(RecoveryGradient(cell+1),f-1),blend.y),blend.x)+.5;
}
float2 RecoveryVoronoiOffset(float2 cell)
{
    uint2 c=(uint2)(int2)cell; c.y^=0x41c64e6du;c.x+=c.y;c.x*=c.y;c.x^=c.x>>5;c.x*=0x27d4eb2du;c.y^=c.x<<3;
    float2 hashed=(float2)(c>>8)/16777215.0;
    return float2(sin(hashed.y)*.5+.5,cos(hashed.x)*.5+.5);
}
float RecoveryPattern(float2 uv)
{
    float radians=_UV_Rotation*.01745329252, sn,cs; sincos(radians,sn,cs);
    uv-=.5; uv=float2(cs*uv.x-sn*uv.y,sn*uv.x+cs*uv.y)+.5;
    uv*=_Skin_UV_Scale;
    float value;
    #if defined(_SKIN_TYPE_CHECKERBOARD)
        radians=_Noise_Rotation*.01745329252;sincos(radians,sn,cs);
        uv-=.5;uv=float2(cs*uv.x-sn*uv.y,sn*uv.x+cs*uv.y)+1;
        uv*=_Skin_Noise_Scale;
        float2 width=float2(length(float2(ddx(uv.x),ddy(uv.x))),length(float2(ddx(uv.y),ddy(uv.y))));
        float2 wave=clamp((abs(frac(uv+.25)-.5)*4-1)*(.35/max(width,.000001)),-1,1);
        value=1-(wave.x*wave.y*sqrt(saturate(1.1-max(width.x,width.y)))*.5+.5);
    #elif defined(_SKIN_TYPE_VORONOI)
        uv*=_Skin_Noise_Scale;float2 cell=floor(uv),f=frac(uv); value=8;
        [unroll] for(int y=-1;y<=1;y++) [unroll] for(int x=-1;x<=1;x++)
        { float2 offset=float2(x,y);value=min(value,length(offset+RecoveryVoronoiOffset(cell+offset)-f)); }
    #else
        value=RecoveryGradientNoise(uv*_Skin_Noise_Scale);
    #endif
    // The serialized defaults may contain equal edges. Preserve a stable step.
    return abs(_Skin_Smooth_Step.y-_Skin_Smooth_Step.x)<.000001 ? step(_Skin_Smooth_Step.x,value) : smoothstep(_Skin_Smooth_Step.x,_Skin_Smooth_Step.y,value);
}
half3 RecoveryItemColor(float2 paletteUV,float2 materialMask,float2 patternUV)
{
    half3 original=SAMPLE_TEXTURE2D(_Colors,sampler_Colors,paletteUV).rgb;
    float metallic=step(_Metallic_Cutoff,materialMask.x), noise=RecoveryPattern(patternUV);
    float2 firstUV=paletteUV+_Color_Offset;
    float blend=noise*(_Use_Skin!=0);
    #if defined(_SKIN_AFFECTS_ONLY_METALLIC)
        firstUV=paletteUV+_Color_Offset*metallic;
        blend*=metallic;
    #endif
    float2 secondUV=firstUV+_Color_Offset_2;
    if(_Rainbow_Skin!=0) secondUV*=noise;
    half3 color=lerp(SAMPLE_TEXTURE2D(_Colors,sampler_Colors,firstUV).rgb,SAMPLE_TEXTURE2D(_Colors,sampler_Colors,secondUV).rgb,blend);
    if(materialMask.x+materialMask.y>=1.8) color=original;
    #ifdef RECOVERY_COOKABLE
        // Exact original raw -> cooked -> burnt interpolation, cookness range 0..2.
        color=lerp(color,_CookColor.rgb,saturate(_Cookness));
        color=lerp(color,_BurntColor.rgb,saturate(_Cookness-1));
    #endif
    return color;
}
#endif
