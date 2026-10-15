# Water original-resource audit — 2026-10-15

Read-only audit of main Unity project; no Unity process started and no Assets changed.

## Confirmed rendering path

`globalgamemanagers.assets` object 3125 is the active `Render Water` RenderObjects feature. Its original serialized fields match exported `Assets/MonoBehaviour/Render Water_0.asset`:
- active 1, event 450, opaque queue, layer mask 16
- overrideMaterial points to original Water (globalgamemanagers.assets object 5)
- override material pass 0; overrideMode 1
- depth override enabled, compare 4, write false
- stencil override enabled, reference 0, compare 3, all operations Keep

Main project's active URP renderer includes this feature (GUID 549ca85659303e94fa091bd1ef8837c3). Its overrideMaterial still points at the editable `Assets/Material/Water.mat`, whose shader is the reconstructed source Shader asset. `WaterManager.Awake` replaces tile materials and meshes with bundle originals, while `RecoveryOriginalShaders.Apply` only replaces Renderer and UI materials. Neither operation updates RenderObjects.settings.overrideMaterial in the inspected source. Consequently, a rendering route exists which can draw water with the reconstructed shader despite the tile's original compiled shader. Runtime verification is still necessary to establish which pass contributes to the final frame.

## Texture closure

Original Water object 5 has exactly one nonempty texture reference: `_Normal_Bumps_Map` -> same-file Texture2D object 29. The unity_Lightmaps, unity_LightmapsInd and unity_ShadowMasks refs are originally zero. pack_original_shaders_v2.py resetting these empty entries does not lose water texture content.

## Scene state

All nine original water tiles use sharedassets0.assets Mesh 172 and original Water 5. Their scale is (10,10,10), local y -1, grid x/z -200/0/200, and no shadow casting. Original holder is scale 1 and position zero. Serialized ReflectionProbeUsage=1 but recovered original gameplay Awake explicitly turns it Off, so that runtime setting should not be guessed from the serialized value alone.

Game.unity's fog, ambient sky/equator/ground colors, ambient intensity/mode, custom reflection mode/resolution/intensity and references match original level0 RenderSettings 2887 within float serialization precision. No static evidence supports changing those artistic settings.

Original RenderSettings additionally contains baked runtime m_AmbientProbe 27 SH floats and m_IndirectSpecularColor (0.2627057731, 0.5761764050, 0.9999990463). These are not represented in exported Game.unity YAML. This identifies a comparison target only; it does not establish that editor/runtime recomputation yields different values.

## Evidence files

- tools/water-original-audit.json: original Water full material and original scene camera/light/RenderSettings trees.
- tools/water-original-renderers.json: original water GameObjects, Transform, MeshFilter and MeshRenderer fields.
- tools/water-original-monobehaviours.json: original relevant MonoBehaviour base identities and complete raw object hex; water feature 3104/3125 serialized layout inspected against current URP RenderObjects settings.
- tools/water-original-pipeline.json: original GraphicsSettings and QualitySettings native type trees.

Original MonoBehaviour build data omits custom type trees. Base names were read with check_read=False; complete raw bytes retained. We do not claim a full semantic parse of every URP or Volume field.
