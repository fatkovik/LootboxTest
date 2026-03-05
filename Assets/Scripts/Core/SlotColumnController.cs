using System.Collections.Generic;
using AxGrid;
using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;

namespace SlotMachine
{
    public class SlotColumnController : MonoBehaviourExtBind
    {
        [Tooltip("Number of unique symbol items (pool will be itemCount + 2).")]
        [SerializeField] private int itemCount = 3;

        [Tooltip("Height of each slot item in pixels. Must match the item RectTransform sizeDelta.y.")]
        [SerializeField] private float itemHeight    = 150f;

        [Tooltip("Scroll speed at full spin, in pixels per second.")]
        [SerializeField] private float maxSpeed      = 1200f;

        [Tooltip("Seconds to linearly ramp from 0 to maxSpeed.")]
        [SerializeField] private float accelTime     = 1.5f;

        [Tooltip("Pixel distance from the nearest snap position that triggers the final hard snap.")]
        [SerializeField] private float snapTolerance = 0.5f;

        private enum Phase { Idle, Accelerating, Spinning, Stopping, Snapping }
        private Phase _phase = Phase.Idle;

        private float _currentSpeed;

        private float _decelVelocity;   // braking in Stopping
        private float _snapVelocity;    // fine alignment in Snapping

        private float _snapRemaining;

        private float _maskHeight;
        private float _maskCentreY;     // = -_maskHeight * 0.5f 
        private float _bottomThreshold; // = -(_maskHeight * 0.5f) - itemHeight
        private float _topThreshold;    // =  _maskHeight * 0.5f   (doc)

        private readonly List<RectTransform> _items = new List<RectTransform>();


        private const float DecelSmoothTime = 0.5f;

        private const float SnapSmoothTime  = 0.15f;

        private const float MinSnapSpeed    = 30f;

        [OnStart]
        private void OnStart()
        {
            var maskRt   = transform.parent as RectTransform;
            _maskHeight  = maskRt != null ? maskRt.rect.height : itemHeight;

            _maskCentreY     = -_maskHeight * 0.5f;
            _bottomThreshold = -(_maskHeight * 0.5f) - itemHeight;
            _topThreshold    =   _maskHeight * 0.5f;

            _items.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var rt = transform.GetChild(i) as RectTransform;
                if (rt != null) _items.Add(rt);
            }

            int expectedPool = itemCount + 2;
            if (_items.Count != expectedPool)
                Debug.LogWarning($"[SlotColumnController] Pool size is {_items.Count} " +
                            $"but itemCount + 2 = {expectedPool}. Adjust itemCount or the scene.");

            float startTopY = _maskCentreY + itemHeight * 0.5f;

            for (int i = 0; i < _items.Count; i++)
            {
                float topY = startTopY + i * itemHeight;
                _items[i].anchoredPosition = new Vector2(0f, topY);
            }
        }

        [Bind("OnSlotStart")]
        private void OnSlotStart()
        {
            _currentSpeed = 0f;
            _phase        = Phase.Accelerating;
        }

        [Bind("OnSlotSpinFull")]
        private void OnSlotSpinFull()
        {
            _currentSpeed = maxSpeed;
            _phase        = Phase.Spinning;
        }

        [Bind("OnSlotStopping")]
        private void OnSlotStopping()
        {
            _decelVelocity = 0f;
            _phase         = Phase.Stopping;
        }

        [OnUpdate]
        private void OnUpdate()
        {
            if (_phase == Phase.Idle) return;

            switch (_phase)
            {
                case Phase.Accelerating:
                    _currentSpeed = Mathf.MoveTowards(
                        _currentSpeed,
                        maxSpeed,
                        (maxSpeed / accelTime) * Time.deltaTime);
                    MoveAndRecycle(_currentSpeed * Time.deltaTime);
                    break;

                case Phase.Spinning:
                    MoveAndRecycle(maxSpeed * Time.deltaTime);
                    break;

                case Phase.Stopping:
                    _currentSpeed = Mathf.SmoothDamp(
                        _currentSpeed, 0f, ref _decelVelocity, DecelSmoothTime);
                    MoveAndRecycle(_currentSpeed * Time.deltaTime);

                    if (_currentSpeed < MinSnapSpeed)
                    {
                        _snapRemaining = GetNearestSnapDelta();
                        _snapVelocity  = 0f;
                        _phase         = Phase.Snapping;
                    }
                    break;

                case Phase.Snapping:
                    UpdateSnapping();
                    break;
            }
        }

        private void UpdateSnapping()
        {
            _snapRemaining = GetNearestSnapDelta();

            if (Mathf.Abs(_snapRemaining) <= snapTolerance)
            {
                ApplyOffset(_snapRemaining);
                _phase = Phase.Idle;

                Settings.Fsm?.Invoke("OnStopped");
            }
            else
            {
                float prev = _snapRemaining;
                float next = Mathf.SmoothDamp(
                    _snapRemaining, 0f, ref _snapVelocity, SnapSmoothTime);

                ApplyOffset(prev - next);
            }
        }

        private void MoveAndRecycle(float downDelta)
        {
            int   poolSize    = _items.Count;
            float recycleJump = itemHeight * poolSize;

            foreach (var item in _items)
            {
                item.anchoredPosition += Vector2.down * downDelta;

                if (item.anchoredPosition.y < _bottomThreshold)
                {
                    item.anchoredPosition = new Vector2(
                        item.anchoredPosition.x,
                        item.anchoredPosition.y + recycleJump);
                }
            }
        }

        private void ApplyOffset(float yOffset)
        {
            foreach (var item in _items)
                item.anchoredPosition += new Vector2(0f, yOffset);
        }

        private float GetNearestSnapDelta()
        {
            float closestDist  = float.MaxValue;
            float closestDelta = 0f;

            foreach (var item in _items)
            {
                float itemCentreY = item.anchoredPosition.y - itemHeight * 0.5f;
                float dist        = Mathf.Abs(itemCentreY - _maskCentreY);

                if (dist < closestDist)
                {
                    closestDist  = dist;
                    closestDelta = _maskCentreY - itemCentreY; // signed correction
                }
            }

            return closestDelta;
        }
    }
}
