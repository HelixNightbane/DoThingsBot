using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.Lib.Recipes {
    public class RecipeStep {
        public string use = "";
        public string on = "";
        public string result = "";
        public string skill = "";
        public int difficulty = 0;
        public string successMessage = "";
        public string failMessage = "";
        public int successWCID = 0;
        public int failWCID = 0;
        public int successAmount = 0;
        public int failAmount = 0;
        public int successDestroySourceAmount = 0;
        public int successDestroyTargetAmount = 0;
        public int failDestroySourceAmount = 0;
        public int failDestroyTargetAmount = 0;


        public RecipeStep(string use, string on, string result, string skill, int difficulty, string successMessage, string failMessage, int successWCID, int failWCID, int successAmount, int failAmount, int successDestroySourceAmount, int successDestroyTargetAmount, int failDestroySourceAmount, int failDestroyTargetAmount) {
            this.use = use;
            this.on = on;
            this.result = result;
            this.skill = skill;
            this.difficulty = difficulty;
            this.successMessage = successMessage;
            this.failMessage = failMessage;
            this.successWCID = successWCID;
            this.failWCID = failWCID;
            this.successAmount = successAmount;
            this.failAmount = failAmount;
            this.successDestroySourceAmount = successDestroySourceAmount;
            this.successDestroyTargetAmount = successDestroyTargetAmount;
            this.failDestroySourceAmount = failDestroySourceAmount;
            this.failDestroyTargetAmount = failDestroyTargetAmount;

        }

        internal WorldObject GetUseItem(ItemBundle bundle) {
            foreach (var id in bundle.playerData.itemIds) {
                var wo = CoreManager.Current.WorldFilter[id];

                if (wo != null && wo.Name == use) {
                    return wo;
                }
            }

            return null;
        }

        internal WorldObject GetTargetItem(ItemBundle bundle, WorldObject useItem) {
            if (useItem == null) return null;

            if (on == "DYEABLE_ITEM") {
                foreach (var id in bundle.playerData.itemIds) {
                    var wo = CoreManager.Current.WorldFilter[id];
                    if (wo != null && wo.Values(BoolValueKey.Dyeable, false) == true) {
                        return wo;
                    }
                }
            }

            foreach (var id in bundle.playerData.itemIds) {
                var wo = CoreManager.Current.WorldFilter[id];

                if (wo != null && wo.Name == on && wo.Id != useItem.Id) {
                    return wo;
                }
            }

            return null;
        }
    }
}
