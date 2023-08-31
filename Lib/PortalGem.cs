using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.Lib {
    public class PortalGem {
        public string Name { get; private set; }
        public int Heading { get; private set; }
        public int Icon { get; private set; }

        public PortalGem(string name, int heading, int icon) {
            Name = name;
            Heading = heading;
            Icon = icon;
        }
    }
}
