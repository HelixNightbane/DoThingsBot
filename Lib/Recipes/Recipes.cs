using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace DoThingsBot.Lib.Recipes {
    public static class Recipes {
        public static List<string> allTools = new List<string>();
        public static List<string> tools = new List<string>();
        public static List<Recipe> recipes = new List<Recipe>();

        public static void Init() {
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(Util.GetResourcesDirectory(), "tools.xml"));

            allTools.Clear();
            tools.Clear();
            recipes.Clear();

            foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                try {
                    if (node.Attributes["name"] != null) {
                        allTools.Add(node.Attributes["name"].InnerText);
                    }
                }
                catch (Exception ex) { Util.LogException(ex); }
            }

            LoadRecipes();

            Util.WriteToChat($"Loaded {recipes.Count} recipes.");

            // TODO: make sure we are only pulling tools that exist in our inventory
            foreach (var recipe in recipes) {
                foreach (var step in recipe.steps) {
                    if (allTools.Contains(step.use) && !tools.Contains(step.use)) {
                        tools.Add(step.use);
                    }

                    if (allTools.Contains(step.on) && !tools.Contains(step.on)) {
                        tools.Add(step.on);
                    }
                }
            }
        }

        public static string GetToolLocation(string tool) {
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(Util.GetResourcesDirectory(), "tools.xml"));

            foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                try {
                    if (node.Attributes["name"] != null && node.Attributes["name"].InnerText.ToLower() == tool.ToLower()) {
                        return node.Attributes["whereToAcquire"] != null ? node.Attributes["whereToAcquire"].InnerText : "";
                    }
                }
                catch (Exception ex) { Util.LogException(ex); }
            }

            return "";
        }

        public static void LoadRecipes() {
            try {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(Util.GetResourcesDirectory(), "recipes.xml"));

                foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                    try {
                        if (node.Attributes["name"] != null) {
                            var recipe = new Recipe(node.Attributes["name"].InnerText);

                            foreach (XmlNode step in node.ChildNodes) {
                                int difficulty;
                                int successWCID;
                                int failWCID;
                                int successAmount;
                                int failAmount;
                                int successDestroySourceAmount;
                                int successDestroyTargetAmount;
                                int failDestroySourceAmount;
                                int failDestroyTargetAmount;

                                var use = step.Attributes["use"] != null ? step.Attributes["use"].InnerText : "Unknown";
                                var on = step.Attributes["on"] != null ? step.Attributes["on"].InnerText : "Unknown";
                                var result = step.Attributes["result"] != null ? step.Attributes["result"].InnerText : "Unknown";
                                var skill = step.Attributes["skill"] != null ? step.Attributes["skill"].InnerText : "Unknown";
                                var difficulty_str = step.Attributes["difficulty"] != null ? step.Attributes["difficulty"].InnerText : "Unknown";
                                var successMessage = step.Attributes["successMessage"] != null ? step.Attributes["successMessage"].InnerText : "Unknown";
                                var failMessage = step.Attributes["failMessage"] != null ? step.Attributes["failMessage"].InnerText : "Unknown";
                                var successWCID_str = step.Attributes["successWCID"] != null ? step.Attributes["successWCID"].InnerText : "Unknown";
                                var failWCID_str = step.Attributes["failWCID"] != null ? step.Attributes["failWCID"].InnerText : "Unknown";
                                var successAmount_str = step.Attributes["successAmount"] != null ? step.Attributes["successAmount"].InnerText : "Unknown";
                                var failAmount_str = step.Attributes["failAmount"] != null ? step.Attributes["failAmount"].InnerText : "Unknown";
                                var successDestroySourceAmount_str = step.Attributes["successDestroySourceAmount"] != null ? step.Attributes["successDestroySourceAmount"].InnerText : "Unknown";
                                var successDestroyTargetAmount_str = step.Attributes["successDestroyTargetAmount"] != null ? step.Attributes["successDestroyTargetAmount"].InnerText : "Unknown";
                                var failDestroySourceAmount_str = step.Attributes["failDestroySourceAmount"] != null ? step.Attributes["failDestroySourceAmount"].InnerText : "Unknown";
                                var failDestroyTargetAmount_str = step.Attributes["failDestroyTargetAmount"] != null ? step.Attributes["failDestroyTargetAmount"].InnerText : "Unknown";

                                if (!Int32.TryParse(difficulty_str, out difficulty)) difficulty = 1;
                                if (!Int32.TryParse(successWCID_str, out successWCID)) successWCID = 0;
                                if (!Int32.TryParse(failWCID_str, out failWCID)) failWCID = 0;
                                if (!Int32.TryParse(successAmount_str, out successAmount)) successAmount = 1;
                                if (!Int32.TryParse(failAmount_str, out failAmount)) failAmount = 1;
                                if (!Int32.TryParse(successDestroySourceAmount_str, out successDestroySourceAmount)) successDestroySourceAmount = 0;
                                if (!Int32.TryParse(successDestroyTargetAmount_str, out successDestroyTargetAmount)) successDestroyTargetAmount = 0;
                                if (!Int32.TryParse(failDestroySourceAmount_str, out failDestroySourceAmount)) failDestroySourceAmount = 0;
                                if (!Int32.TryParse(failDestroyTargetAmount_str, out failDestroyTargetAmount)) failDestroyTargetAmount = 0;

                                recipe.AddStep(use, on, result, skill, difficulty, successMessage, failMessage, successWCID, failWCID,
                                    successAmount, failAmount, successDestroySourceAmount, successDestroyTargetAmount,
                                    failDestroySourceAmount, failDestroyTargetAmount);
                            }

                            recipes.Add(recipe);
                        }
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public static Recipe FindByName(string name) {
            foreach (var recipe in recipes) {
                if (recipe.name.ToLower() == name.ToLower()) return recipe;
            }

            return null;
        }

        public static List<Recipe> FindByPartialIngredients(IngredientList ingredients) {
            var results = new List<Recipe>();

            foreach (var recipe in recipes) {
                var containsAllIngredients = true;
                foreach (var ingredient in ingredients.ingredients) {
                    if (!recipe.GetFullIngredients().Contains(ingredient.Key)) {
                        containsAllIngredients = false;
                        break;
                    }
                }

                if (containsAllIngredients) {
                    results.Add(recipe);
                }
            }

            return results;
        }

        public static List<Recipe> FindByIngredients(IngredientList ingredients) {
            var results = new List<Recipe>();

            var i = string.Join(", ", ingredients.ingredients.Select(s => s.Key).ToArray());
            Util.WriteToChat($"Finding recipes for ingredients: {i}");

            foreach (var recipe in recipes) {
                foreach (var ingredientSet in recipe.GetIngredientSets()) {
                    if (recipe.MatchesIngredients(ingredients)) {
                        results.Add(recipe);
                        break;
                    }
                }
            }

            if (results.Count == 0) {
                return FindByPartialIngredients(ingredients);
            }

            return results;
        }

        public static List<Recipe> FindByItemBundle(ItemBundle bundle) {
            return FindByIngredients(bundle.GetIngredientList());
        }
    }
}
