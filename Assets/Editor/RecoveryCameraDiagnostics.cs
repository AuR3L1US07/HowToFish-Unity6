using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

// Read-only evidence collection. Call during the same frame as a game capture.
[InitializeOnLoad]
public static class RecoveryCameraDiagnostics
{
    static bool written;
    static RecoveryCameraDiagnostics() { EditorApplication.update += InitTick; }
    static void InitTick()
    {
        if (!EditorApplication.isPlaying) { written = false; return; }
        if (!written && Time.time >= 60f && Player.LocalPlayer != null) { written = true; Write(); }
    }
    [MenuItem("Recovery/Write Camera Diagnostics")]
    public static void Write()
    {
        var text = new StringBuilder();
        var camera = Player.LocalPlayer != null ? Player.LocalPlayer.CurCam : Camera.main;
        if (Player.LocalPlayer != null)
        {
            var player = Player.LocalPlayer;
            text.AppendLine($"PLAYER root={player.Transform.position} cameraHeight={player.Camera.CamHeight} feet={player.Movement.CurFeetWorldPos} spawn={SpawnManager.MainSpawnPos} water={WaterManager.GetWaterHeight(player.Transform.position)} grounded={player.Movement.Grounded}");
            foreach (var col in player.GetComponentsInChildren<Collider>()) text.AppendLine($"PLAYER-COLLIDER {Path(col.transform)} enabled={col.enabled} trigger={col.isTrigger} bounds={col.bounds}");
        }
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            text.AppendLine($"CAM {Path(c.transform)} enabled={c.enabled} pos={c.transform.position} rot={c.transform.eulerAngles} fov={c.fieldOfView} near={c.nearClipPlane} mask={c.cullingMask} target={c.targetTexture}");
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas.isRootCanvas) text.AppendLine($"CANVAS {Path(canvas.transform)} mode={canvas.renderMode} camera={canvas.worldCamera} distance={canvas.planeDistance}");
        if (camera != null)
        {
            foreach (var hit in Physics.RaycastAll(camera.transform.position, camera.transform.forward, 5f, ~0, QueryTriggerInteraction.Ignore))
                text.AppendLine($"FORWARD-HIT {Path(hit.transform)} distance={hit.distance} point={hit.point} layer={hit.collider.gameObject.layer}");
            foreach (var hit in Physics.RaycastAll(camera.transform.position + Vector3.up * 5f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
                text.AppendLine($"GROUND-HIT {Path(hit.transform)} distance={hit.distance} point={hit.point} layer={hit.collider.gameObject.layer}");
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0 || !GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) continue;
                if (Vector3.Distance(camera.transform.position, renderer.bounds.ClosestPoint(camera.transform.position)) > 3f) continue;
                text.AppendLine($"NEAR {Path(renderer.transform)} type={renderer.GetType().Name} layer={renderer.gameObject.layer} pos={renderer.transform.position} scale={renderer.transform.lossyScale} bounds={renderer.bounds} containsCamera={renderer.bounds.Contains(camera.transform.position)}");
                if (renderer is SkinnedMeshRenderer skin)
                    text.AppendLine($" SKIN mesh={skin.sharedMesh} rootBone={skin.rootBone} localBounds={skin.localBounds} boneCount={skin.bones.Length}");
                foreach (var material in renderer.sharedMaterials)
                    if (material != null) text.AppendLine($" MAT {material.name} shader={material.shader.name} queue={material.renderQueue}");
            }
        }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/camera-diagnostics.txt", text.ToString());
    }
    static string Path(Transform value) => value.parent == null ? value.name : Path(value.parent) + "/" + value.name;
}
