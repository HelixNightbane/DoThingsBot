using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.Lib {
    public class StockItems {
        public string Name { get; private set; }
        public int StackSize { get; private set; }
        public int Icon { get; private set; }

        public StockItems(string name, int size, int icon) {
            Name = name;
            StackSize = size;
            Icon = icon;
        }
    }
}
