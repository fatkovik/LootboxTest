using AxGrid;
using AxGrid.Base;
using AxGrid.FSM;
using AxGrid.Model;
using UnityEngine;

namespace SlotMachine
{
    public class SlotFSM : MonoBehaviourExtBind
    {
        private const string StateIdle = "Slot_Idle";
        private const string StateAccelerating = "Slot_Accelerating";
        private const string StateSpinning = "Slot_Spinning";
        private const string StateStopping = "Slot_Stopping";

        [OnStart]
        private void StartThis()
        {
            Settings.Fsm = new FSM();
            Settings.Fsm.Add(new IdleState());
            Settings.Fsm.Add(new AcceleratingState());
            Settings.Fsm.Add(new SpinningState());
            Settings.Fsm.Add(new StoppingState());
            Settings.Fsm.Start(StateIdle);
        }

        [OnUpdate]
        private void UpdateThis()
        {
            Settings.Fsm.Update(Time.deltaTime);
        }

        [State(StateIdle)]
        private class IdleState : FSMState
        {
            [Enter]
            private void OnEnter()
            {
                Log.Debug("[SlotFSM] → Idle");
                Settings.Model.EventManager.Invoke("OnSlotIdle");
            }

            [Bind("OnStartPressed")]
            private void OnStartPressed()
            {
                Log.Debug("[SlotFSM] Idle → Accelerating");
                Parent.Change(StateAccelerating);
            }

            [Exit]
            private void OnExit()
            {
                Log.Debug("[SlotFSM] ← Idle");
            }
        }

        [State(StateAccelerating)]
        private class AcceleratingState : FSMState
        {
            [Enter]
            private void OnEnter()
            {
                Log.Debug("[SlotFSM] → Accelerating");
                Settings.Model.EventManager.Invoke("OnSlotStart");
            }

            [One(3f)]
            private void OnSpinReady()
            {
                Log.Debug("[SlotFSM] Accelerating → Spinning (3 s elapsed)");
                Parent.Change(StateSpinning);
            }

            [Exit]
            private void OnExit()
            {
                Log.Debug("[SlotFSM] ← Accelerating");
            }
        }

        [State(StateSpinning)]
        private class SpinningState : FSMState
        {
            [Enter]
            private void OnEnter()
            {
                Log.Debug("[SlotFSM] → Spinning");
                Settings.Model.EventManager.Invoke("OnSlotSpinFull");
            }

            [Bind("OnStopPressed")]
            private void OnStopPressed()
            {
                Log.Debug("[SlotFSM] Spinning → Stopping");
                Parent.Change(StateStopping);
            }

            [One(2f)]
            private void AutoStop()
            {
                Log.Debug("[SlotFSM] Spinning → Stopping (auto 2s)");
                Parent.Change(StateStopping);
            }

            [Exit]
            private void OnExit()
            {
                Log.Debug("[SlotFSM] ← Spinning");
            }
        }

        [State(StateStopping)]
        private class StoppingState : FSMState
        {
            [Enter]
            private void OnEnter()
            {
                Log.Debug("[SlotFSM] → Stopping");
                Settings.Model.EventManager.Invoke("OnSlotStopping");
            }

            [Bind("OnStopped")]
            private void OnStopped()
            {
                Log.Debug("[SlotFSM] Stopping → Idle");
                Parent.Change(StateIdle);
            }

            [Exit]
            private void OnExit()
            {
                Log.Debug("[SlotFSM] ← Stopping");
            }
        }
    }
}
