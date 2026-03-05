using System.Collections.Generic;
using System.Linq;
using AxGrid;
using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine
{
    public class SlotWinChecker : MonoBehaviourExtBind
    {
        [Header("References")]
        [SerializeField] private List<RectTransform> itemContainers;
        [SerializeField] private RectTransform maskContainer;

        [Header("Win Line")]
        [SerializeField] private Color lineColor = new Color(1f, 0.84f, 0f, 0.8f);
        [SerializeField] private float lineWidth = 6f;

        [Header("Win Effects")]
        [SerializeField] private ParticleSystem winParticles;
        [SerializeField] private ParticleSystem loseParticles;

        private readonly List<List<Image>> _reelImages = new List<List<Image>>();

        private readonly List<GameObject> _segmentPool = new List<GameObject>();

        [OnStart]
        private void OnStart()
        {
            _reelImages.Clear();
            if (itemContainers != null)
            {
                foreach (var container in itemContainers)
                {
                    var images = new List<Image>();
                    for (int i = 0; i < container.childCount; i++)
                    {
                        var img = container.GetChild(i).GetComponent<Image>();
                        if (img != null)
                            images.Add(img);
                    }
                    _reelImages.Add(images);
                }
            }

            Debug.Log($"[SlotWinChecker] {_reelImages.Count} reels cached.");
        }

        [Bind("OnSlotIdle")]
        private void OnSlotIdle()
        {
            CheckWin();
        }

        [Bind("OnSlotStart")]
        private void OnSlotStart()
        {
            HideWin();
        }
        private void CheckWin()
        {
            if (maskContainer == null || _reelImages.Count < 3)
                return;

            var visiblePerReel = GetVisibleItemsPerReel();

            Debug.Log($"[SlotWinChecker] Visible per reel: [{string.Join(", ", visiblePerReel.Select(v => v.Count))}]");

            for (int r = 0; r < visiblePerReel.Count; r++)
            {
                foreach (var img in visiblePerReel[r])
                {
                    Debug.Log($"[SlotWinChecker] Reel {r}: sprite={img.sprite?.name ?? "null"} " +
                              $"instanceID={img.sprite?.GetInstanceID()}");
                }
            }

            var spriteMap = new Dictionary<Sprite, List<(int reel, Image img)>>();

            for (int reelIdx = 0; reelIdx < visiblePerReel.Count; reelIdx++)
            {
                foreach (var img in visiblePerReel[reelIdx])
                {
                    if (img.sprite == null) continue;

                    if (!spriteMap.ContainsKey(img.sprite))
                        spriteMap[img.sprite] = new List<(int, Image)>();

                    spriteMap[img.sprite].Add((reelIdx, img));
                }
            }

            foreach (var kvp in spriteMap)
            {
                var perReel = DeduplicateByReel(kvp.Value);

                if (perReel.Count >= 3)
                {
                    var matches = perReel.Select(x => x.img).ToList();
                    ShowWinLine(matches);

                    if (winParticles != null)
                    {
                        //winParticles.transform.position = GetGroupCenter(matches);
                        // Can go with small particles for the winning group, but decided not to.
                        winParticles.Play();
                    }

                    Debug.Log($"[SlotWinChecker] WIN! {matches.Count}x {kvp.Key.name} across {perReel.Count} reels");
                    Settings.Model.EventManager.Invoke("OnSlotWin");
                    return; // first win found is enough.
                }
            }

            Debug.Log("[SlotWinChecker] No match — lose.");
            if (loseParticles != null)
                loseParticles.Play();
            Settings.Model.EventManager.Invoke("OnSlotLose");
        }

        private List<List<Image>> GetVisibleItemsPerReel()
        {
            var result = new List<List<Image>>();
            var maskCorners = new Vector3[4];
            var itemCorners = new Vector3[4];

            maskContainer.GetWorldCorners(maskCorners);
            float maskMinY = maskCorners[0].y;
            float maskMaxY = maskCorners[2].y;

            for (int reelIdx = 0; reelIdx < _reelImages.Count; reelIdx++)
            {
                var visible = new List<Image>();
                foreach (var img in _reelImages[reelIdx])
                {
                    img.rectTransform.GetWorldCorners(itemCorners);
                    float itemCentreY = (itemCorners[0].y + itemCorners[2].y) * 0.5f;

                    if (itemCentreY >= maskMinY && itemCentreY <= maskMaxY)
                        visible.Add(img);
                }
                result.Add(visible);
            }

            Debug.Log($"[SlotWinChecker] mask Y: [{maskMinY:F1} .. {maskMaxY:F1}], " +
                      $"visible per reel: [{string.Join(", ", result.Select(v => v.Count))}]");

            return result;
        }

        private List<(int reel, Image img)> DeduplicateByReel(
            List<(int reel, Image img)> hits)
        {
            var maskCorners = new Vector3[4];
            maskContainer.GetWorldCorners(maskCorners);
            float maskCentreY = (maskCorners[0].y + maskCorners[2].y) * 0.5f;

            var best = new Dictionary<int, (Image img, float dist)>();
            var itemCorners = new Vector3[4];

            foreach (var (reel, img) in hits)
            {
                img.rectTransform.GetWorldCorners(itemCorners);
                float itemCentreY = (itemCorners[0].y + itemCorners[2].y) * 0.5f;
                float dist = Mathf.Abs(itemCentreY - maskCentreY);

                if (!best.ContainsKey(reel) || dist < best[reel].dist)
                    best[reel] = (img, dist);
            }

            return best
                .OrderBy(kv => kv.Key)
                .Select(kv => (kv.Key, kv.Value.img))
                .ToList();
        }

        private void ShowWinLine(List<Image> matches)
        {
            HideWin();

            if (matches.Count < 2) return;

            var myRt = (RectTransform)transform;

            var points = new List<Vector2>();
            var corners = new Vector3[4];
            foreach (var img in matches)
            {
                img.rectTransform.GetWorldCorners(corners);
                Vector3 worldCentre = (corners[0] + corners[2]) * 0.5f;
                Vector2 localPt;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    myRt,
                    RectTransformUtility.WorldToScreenPoint(null, worldCentre),
                    null, out localPt);
                points.Add(localPt);
            }

            points.Sort((a, b) => a.x.CompareTo(b.x));

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 from = points[i];
                Vector2 to = points[i + 1];

                var seg = GetOrCreateSegment(i);
                var rt = seg.GetComponent<RectTransform>();

                Vector2 mid   = (from + to) * 0.5f;
                float dist  = Vector2.Distance(from, to);
                float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = mid;
                rt.sizeDelta = new Vector2(dist, lineWidth);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);

                seg.SetActive(true);
            }
        }

        private GameObject GetOrCreateSegment(int index)
        {
            if (index < _segmentPool.Count)
                return _segmentPool[index];

            var go = new GameObject($"WinSegment_{index}", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var img = go.AddComponent<Image>();
            img.color = lineColor;
            img.raycastTarget = false;
            go.SetActive(false);

            _segmentPool.Add(go);
            return go;
        }

        private void HideWin()
        {
            foreach (var seg in _segmentPool)
                seg.SetActive(false);

            if (winParticles != null)
                winParticles.Stop();

            if (loseParticles != null)
                loseParticles.Stop();
        }

        private Vector3 GetGroupCenter(List<Image> matches)
        {
            Vector3 sum = Vector3.zero;
            foreach (var img in matches)
                sum += img.rectTransform.position;
            return sum / matches.Count;
        }
    }
}
