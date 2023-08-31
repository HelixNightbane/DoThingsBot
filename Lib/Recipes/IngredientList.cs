using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.Lib.Recipes {
    public class IngredientList {
        public Dictionary<string, int> ingredients = new Dictionary<string, int>();

        public void Add(string ingredient, int count = 1) {
            if (ingredients.ContainsKey(ingredient)) {
                ingredients[ingredient] += count;
            }
            else {
                ingredients.Add(ingredient, count);
            }

            if (ingredients[ingredient] == 0) ingredients.Remove(ingredient);
        }

        public void Remove(string ingredient, int count = 1) {
            if (ingredients.ContainsKey(ingredient)) {
                ingredients[ingredient] -= count;
            }
            else {
                ingredients.Add(ingredient, -count);
            }

            if (ingredients[ingredient] == 0) ingredients.Remove(ingredient);
        }

        public bool Contains(string ingredient) {
            return ingredients.ContainsKey(ingredient) && ingredients[ingredient] > 0;
        }

        internal int Count() {
            var count = 0;
            foreach (var ingredient in ingredients) {
                count += ingredient.Value;
            }

            return count;
        }

        internal int Count(string ingredient) {
            if (ingredients.ContainsKey(ingredient)) return ingredients[ingredient];

            return 0;
        }

        internal bool ContainsAllOf(IngredientList ingredientList) {
            foreach (var i in ingredientList.ingredients) {
                if (Count(i.Key) < i.Value) return false;
            }

            return true;
        }

        internal string Summary() {
            var summary = new List<string>();
            foreach (var ingredient in ingredients) {
                if (ingredient.Value == 0) continue;

                if (ingredient.Value == 1) {
                    summary.Add($"{ingredient.Key}");
                }
                else {
                    summary.Add($"{ingredient.Key} x{ingredient.Value}");
                }
            }

            return String.Join(", ", summary.ToArray());
        }

        internal IngredientList Clone() {
            var clone = new IngredientList();

            foreach (var ingredient in ingredients) {
                clone.Add(ingredient.Key, ingredient.Value);
            }

            return clone;
        }

        internal void RemoveAll(string key) {
            Remove(key, Count(key));
        }
    }
}
