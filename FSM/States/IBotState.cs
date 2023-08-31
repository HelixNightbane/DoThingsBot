using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    public interface IBotState {
        string Name { get; }

        ItemBundle GetItemBundle();

        void Enter(Machine machine);
        void Exit(Machine machine);
        void Think(Machine machine);
    }
}
