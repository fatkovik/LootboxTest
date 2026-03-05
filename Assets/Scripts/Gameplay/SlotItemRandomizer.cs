using System.Collections.Generic;
using System.Linq;
using AxGrid;
using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine
{
    public class SlotItemRandomizer : MonoBehaviourExtBind
    {
        [Tooltip("Pool of sprites to pick from. Add as many as you like.")]
        [SerializeField] private List<Sprite> sprites = new List<Sprite>();

        private readonly List<Image> _images = new List<Image>();

        [OnStart]
        private void OnStart()
        {
            _images.Clear();

            _images.AddRange(transform.GetComponentsInChildren<Image>().ToList());

            if (_images.Count == 0)
                Debug.LogWarning("[SlotItemRandomizer] No child Image components found.");

            if (sprites.Count == 0)
                Debug.LogWarning("[SlotItemRandomizer] Sprite list is empty — assign sprites in the Inspector.");

            RandomizeSlots();
        }

        [Bind("OnSlotStart")]
        private void OnSlotStart()
        {
            RandomizeSlots();
        }

        private void RandomizeSlots()
        {
            if (sprites.Count == 0 || _images.Count == 0) return;

            foreach (var img in _images)
                img.sprite = sprites[Random.Range(0, sprites.Count)];
        }
    }
}
