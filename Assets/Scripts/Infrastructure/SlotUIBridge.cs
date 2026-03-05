using AxGrid;
using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;

namespace SlotMachine
{
    public class SlotUIBridge : MonoBehaviourExtBind
    {
        private const string KeyStartEnable = "BtnStartEnable";
        private const string KeyStopEnable  = "BtnStopEnable";

        [Bind("OnStartClick")]
        private void OnStartClick()
        {
            Log.Debug("[SlotUIBridge] OnStartClick → FSM OnStartPressed");
            Settings.Fsm?.Invoke("OnStartPressed");
        }

        [Bind("OnStopClick")]
        private void OnStopClick()
        {
            Log.Debug("[SlotUIBridge] OnStopClick → FSM OnStopPressed");
            Settings.Fsm?.Invoke("OnStopPressed");
        }

        [Bind("OnSlotIdle")]
        private void OnSlotIdle()
        {
            SetButtons(startEnabled: true, stopEnabled: false);
        }

        [Bind("OnSlotStart")]
        private void OnSlotStart()
        {
            SetButtons(startEnabled: false, stopEnabled: false);
        }

        [Bind("OnSlotSpinFull")]
        private void OnSlotSpinFull()
        {
            SetButtons(startEnabled: false, stopEnabled: true);
        }

        [Bind("OnSlotStopping")]
        private void OnSlotStopping()
        {
            SetButtons(startEnabled: false, stopEnabled: false);
        }

        private void SetButtons(bool startEnabled, bool stopEnabled)
        {
            Settings.Model.Set(KeyStartEnable, startEnabled);
            Settings.Model.Set(KeyStopEnable,  stopEnabled);
        }
    }
}
