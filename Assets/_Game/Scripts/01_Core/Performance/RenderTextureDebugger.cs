using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TowerBreakers.Core.Performance
{
    public static class RenderTextureDebugger
    {
        [MenuItem("Tools/Performance/List All RenderTextures")]
        public static void ListAllRenderTextures()
        {
            var rts = Resources.FindObjectsOfTypeAll<RenderTexture>();
            Debug.Log($"[RenderTextureDebugger] Found {rts.Length} RenderTextures in memory.");

            var groups = rts.GroupBy(r => r.name)
                            .Select(g => new { Name = g.Key, Count = g.Count(), Size = g.First().width + "x" + g.First().height })
                            .OrderByDescending(g => g.Count);

            foreach (var group in groups)
            {
                Debug.Log($"[RenderTextureDebugger] Name: {group.Name}, Count: {group.Count}, Resolution: {group.Size}");
            }
            
            // 상세 분석: 특정 크기 이상의 텍스처들 조사
            long totalMem = 0;
            foreach (var rt in rts)
            {
                // ARGB32 assumes 4 bytes per pixel
                totalMem += rt.width * rt.height * 4;
            }
            Debug.Log($"[RenderTextureDebugger] Estimated Total GPU Memory for RTs: {totalMem / (1024f * 1024f):F2} MB");
        }

        [MenuItem("Tools/Performance/Clear Leaked RenderTextures")]
        public static void ClearLeakedScreenshots()
        {
            var rts = Resources.FindObjectsOfTypeAll<RenderTexture>();
            int count = 0;
            
            foreach (var rt in rts)
            {
                if (rt != null && rt.name.StartsWith("ProfilerScreenshot"))
                {
                    Object.DestroyImmediate(rt);
                    count++;
                }
            }
            
            Debug.Log($"[RenderTextureDebugger] Successfully cleared {count} leaked ProfilerScreenshot RenderTextures.");
            
            // GC 실행 유도
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }
    }
}
