using System;
using System.Collections.Generic;
using System.Text;
using Decal.Adapter.Wrappers;
using DoThingsBot.FSM.States;

namespace DoThingsBot.FSM {
    public class Machine {
        public IBotState CurrentState { get; set; }
        private IBotState NextState { get; set; }
        private IBotState PreviousState { get; set; }
        private string ParentState { get; set; }

        private Dictionary<string, string> stringValuesStorage = new Dictionary<string, string>();
        private Dictionary<string, int> intValuesStorage = new Dictionary<string, int>();
        private Dictionary<string, DateTime> dateTimeValuesStorage = new Dictionary<string, DateTime>();

        public bool IsRunning;

        public void Start() {
            IsRunning = true;
            //ChangeState(new BotIdleState());
        }

        public void Stop() {
            IsRunning = false;
        }

        private bool disposed;

        public void Dispose() {
            Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            // If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource.
            if (!disposed) {
                if (disposing) {
                }

                // Indicate that the instance has been disposed.
                disposed = true;
            }
        }

        public void SetValue(string key, string value) {
            if (stringValuesStorage.ContainsKey(key)) {
                stringValuesStorage.Remove(key);
            }

            stringValuesStorage.Add(key, value);
        }

        public void SetValue(string key, int value) {
            if (intValuesStorage.ContainsKey(key)) {
                intValuesStorage.Remove(key);
            }

            intValuesStorage.Add(key, value);
        }

        public void SetValue(string key, DateTime value) {
            if (dateTimeValuesStorage.ContainsKey(key)) {
                dateTimeValuesStorage.Remove(key);
            }

            dateTimeValuesStorage.Add(key, value);
        }

        public string GetStringValue(string key) {
            if (stringValuesStorage.ContainsKey(key)) {
                return stringValuesStorage[key];
            }

            return null;
        }

        public int GetIntValue(string key) {
            if (intValuesStorage.ContainsKey(key)) {
                return intValuesStorage[key];
            }

            return 0;
        }

        public DateTime GetDateTimeValue(string key) {
            if (dateTimeValuesStorage.ContainsKey(key)) {
                return dateTimeValuesStorage[key];
            }

            return DateTime.MinValue;
        }

        public void SetParentState(string parentState) {
            ParentState = parentState;
        }

        public void ChangeState(IBotState NewState) {
            Util.WriteToDebugLog(String.Format("{0}{1}: ChangeState -> {2}", ParentState == null ? "" : ParentState + ".", CurrentState == null ? "None" : CurrentState.Name, NewState.Name));
            
            NextState = NewState;
        }

        public bool IsInState(string state) {
            return (IsRunning && CurrentState != null && CurrentState.Name == state);
        }

        public bool IsOrWillBeInState(string state) {
            if (IsRunning) {
                if (CurrentState != null && CurrentState.Name == state) {
                    return true;
                }

                if (NextState != null && NextState.Name == state) {
                    return true;
                }
            }

            return false;
        }

        public void Think() {
            if (IsRunning) {
                /*
                Util.WriteToChat(String.Format("PS: {0} CS: {1} NS: {2}",
                    (PreviousState == null) ? "null" : PreviousState.Name,
                    (CurrentState == null) ? "null" : CurrentState.Name,
                    (NextState == null) ? "null" : NextState.Name
                ));
                */

                
                if (NextState != null) {
                    if (PreviousState == null && CurrentState != null) {
                        PreviousState = CurrentState;

                        Util.WriteToDebugLog(String.Format("{0}{1}: ExitState", ParentState == null ? "" : ParentState + ".", PreviousState.Name));

                        PreviousState.Exit(this);

                        return;
                    }
                    else {
                        PreviousState = null;
                        CurrentState = NextState;
                        NextState = null;

                        Util.WriteToDebugLog(String.Format("{0}{1}: EnterState", ParentState == null ? "" : ParentState + ".", CurrentState.Name));

                        CurrentState.Enter(this);

                        return;
                    }
                }

                if (CurrentState != null) {
                    CurrentState.Think(this);
                }
            }
        }
    }
}
