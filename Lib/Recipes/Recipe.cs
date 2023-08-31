using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.Lib.Recipes {
    public class Recipe {
        public string name = "";
        public List<RecipeStep> steps = new List<RecipeStep>();

        public Recipe(string name) {
            this.name = name;
        }

        public void AddStep(string use, string on, string result, string skill, int difficulty, string successMessage, string failMessage, int successWCID, int failWCID, int successAmount, int failAmount, int successDestroySourceAmount, int successDestroyTargetAmount, int failDestroySourceAmount, int failDestroyTargetAmount) {
            steps.Add(new RecipeStep(use, on, result, skill, difficulty, successMessage, failMessage, successWCID, failWCID, successAmount, failAmount, successDestroySourceAmount, successDestroyTargetAmount, failDestroySourceAmount, failDestroyTargetAmount));
        }

        public string summary() {
            var tools = new List<string>();
            var skillsNeeded = GetSkillsNeeded();
            List<string> skills = new List<string>();
            var summary = $"{this.name}";

            foreach (var skill in skillsNeeded) {
                skills.Add($"{skill.Key} {skill.Value.ToString()}");
            }

            foreach (var step in steps) {
                if (Recipes.allTools.Contains(step.use) && !tools.Contains(step.use)) {
                    tools.Add(step.use);
                }
                if (Recipes.allTools.Contains(step.on) && !tools.Contains(step.on)) {
                    tools.Add(step.on);
                }
            }

            summary += $" ({String.Join(" ", skills.ToArray())}) Ingredients: ";
            summary += GetFullIngredients().Summary();
            if (GetToolsNeeded(tools).Count > 0) {
                summary += $" (I'm missing tools: {String.Join(", ", GetToolsNeeded(tools).ToArray())})";
            }

            return summary;
        }

        public void print() {
            var skillsNeeded = GetSkillsNeeded();
            var ingredientsets = GetIngredientSets();
            var tools = new List<string>();

            List<string> skills = new List<string>();

            foreach (var skill in skillsNeeded) {
                skills.Add($"{skill.Key} {skill.Value.ToString()}");
            }

            foreach (var step in steps) {
                if (Recipes.allTools.Contains(step.use) && !tools.Contains(step.use)) {
                    tools.Add(step.use);
                }
                if (Recipes.allTools.Contains(step.on) && !tools.Contains(step.on)) {
                    tools.Add(step.on);
                }
            }

            Console.WriteLine(name);
            Console.WriteLine($"\tTools Required: {String.Join(", ", tools.ToArray())}");
            if (!HasRequiredTools(tools)) {
                Console.WriteLine($"\tTools Missing: {String.Join(", ", GetToolsNeeded(tools).ToArray())}");
            }
            Console.WriteLine("\tSkills Required: " + String.Join(", ", skills.ToArray()));

            Console.WriteLine("\tValid Ingredient Sets:");
            foreach (var itemset in ingredientsets) {
                Console.WriteLine("\t\t" + itemset.Summary());
            }

            foreach (var step in steps) {
                Console.WriteLine($"\t* Use {step.use} on {step.on} to get {step.result} ({step.skill} {step.difficulty})");
            }
            Console.WriteLine();
        }

        public void printByIngredients(IngredientList ingredients) {
            var skillsNeeded = new Dictionary<string, int>();
            var ingredientsets = GetIngredientSets();

            List<string> skills = new List<string>();
            var tools = new List<string>();

            var i = 0;
            var ingredientSteps = new List<RecipeStep>();

            //ingredientSteps.Reverse();
            var sets = GetIngredientSets();

            //sets.Reverse();

            foreach (var ingredientSet in sets) {
                i++;
                if (ingredients.ContainsAllOf(ingredientSet)) {
                    var reversedSteps = new List<RecipeStep>(steps);
                    reversedSteps.Reverse();
                    foreach (var step in reversedSteps.Take(i)) {
                        ingredientSteps.Add(step);
                        if (Recipes.allTools.Contains(step.use) && !tools.Contains(step.use)) {
                            tools.Add(step.use);
                        }
                        if (Recipes.allTools.Contains(step.on) && !tools.Contains(step.on)) {
                            tools.Add(step.on);
                        }
                        if (skillsNeeded.ContainsKey(step.skill)) {
                            if (skillsNeeded[step.skill] < step.difficulty) {
                                skillsNeeded[step.skill] = step.difficulty;
                            }
                        }
                        else {
                            skillsNeeded.Add(step.skill, step.difficulty);
                        }
                    }
                    break;
                }
            }

            foreach (var skill in skillsNeeded) {
                skills.Add($"{skill.Key} {skill.Value.ToString()}");
            }


            Console.WriteLine(name);
            Console.WriteLine($"\tTools Required: {String.Join(", ", tools.ToArray())}");
            if (!HasRequiredTools(tools)) {
                Console.WriteLine($"\tTools Missing: {String.Join(", ", GetToolsNeeded(tools).ToArray())}");
            }
            Console.WriteLine("\tSkills Required: " + String.Join(", ", skills.ToArray()));

            ingredientSteps.Reverse();

            foreach (var step in ingredientSteps) {
                Console.WriteLine($"\t* Use {step.use} on {step.on} to get {step.result} ({step.skill} {step.difficulty})");
            }
            Console.WriteLine();
        }

        public List<RecipeStep> GetSteps(IngredientList ingredients) {
            var skillsNeeded = new Dictionary<string, int>();
            var ingredientsets = GetIngredientSets();
            var i = 0;
            var ingredientSteps = new List<RecipeStep>();
            var sets = GetIngredientSets();

            foreach (var ingredientSet in sets) {
                i++;
                if (ingredients.ContainsAllOf(ingredientSet)) {
                    var reversedSteps = new List<RecipeStep>(steps);
                    reversedSteps.Reverse();

                    foreach (var step in reversedSteps.Take(i)) {
                        ingredientSteps.Add(step);
                    }
                    break;
                }
            }

            ingredientSteps.Reverse();

            return ingredientSteps;
        }

        public Dictionary<string, int> GetSkillsNeeded() {
            var skills = new Dictionary<string, int>();

            foreach (var step in steps) {
                if (skills.ContainsKey(step.skill)) {
                    if (skills[step.skill] < step.difficulty) {
                        skills[step.skill] = step.difficulty;
                    }
                }
                else {
                    skills.Add(step.skill, step.difficulty);
                }
            }

            return skills;
        }

        public bool HasRequiredTools(List<string> tools) {
            foreach (var tool in tools) {
                if (!Recipes.tools.Contains(tool)) return false;
            }

            return true;
        }

        public IngredientList GetFullIngredients() {
            var ingredients = new IngredientList();
            var lastStep = false;

            var steps = new List<RecipeStep>(this.steps);
            steps.Reverse();

            foreach (var step in steps) {
                if (!Recipes.allTools.Contains(step.use)) {
                    ingredients.Add(step.use);
                }
                if (!Recipes.allTools.Contains(step.on)) {
                    ingredients.Add(step.on);
                }

                if (lastStep && ingredients.Contains(step.result)) {
                    ingredients.Remove(step.result);
                }

                lastStep = true;
            }

            return ingredients;
        }

        public List<IngredientList> GetIngredientSets() {
            var sets = new List<IngredientList>();
            var steps = new List<RecipeStep>(this.steps);
            steps.Reverse();
            bool lastStep = true;

            var ingredients = new IngredientList();

            foreach (var step in steps) {
                if (!Recipes.allTools.Contains(step.use)) {
                    ingredients.Add(step.use);
                }
                if (!Recipes.allTools.Contains(step.on)) {
                    ingredients.Add(step.on);
                }

                if (!lastStep && ingredients.Contains(step.result)) {
                    ingredients.Remove(step.result);
                }

                lastStep = false;

                sets.Add(ingredients.Clone());
            }

            return sets;
        }

        public bool HasAllIngredients(IngredientList ingredients) {
            foreach (var neededIngredients in GetIngredientSets()) {
                var hasAll = true;
                foreach (var ingredient in neededIngredients.ingredients) {
                    if (ingredients.Count(ingredient.Key) < ingredient.Value) {
                        hasAll = false;
                        break;
                    }
                }

                if (hasAll) return true;
            }

            return false;
        }

        public bool MatchesIngredients(IngredientList ingredients) {
            foreach (var neededIngredients in GetIngredientSets()) {
                var hasAll = true;

                foreach (var ingredient in neededIngredients.ingredients) {
                    if (ingredients.Count(ingredient.Key) < ingredient.Value) {
                        hasAll = false;
                        break;
                    }
                }

                if (hasAll && ingredients.ingredients.Keys.Count == neededIngredients.ingredients.Keys.Count) return true;
            }

            return false;
        }

        public IngredientList GetNeededIngredients(IngredientList ingredients) {
            var results = new IngredientList();
            var ingredientSets = GetIngredientSets();

            foreach (var neededIngredients in ingredientSets) {
                var remainingIngredientsNeeded = neededIngredients.Clone();
                foreach (var ingredient in ingredients.ingredients) {
                    remainingIngredientsNeeded.Remove(ingredient.Key, Math.Min(ingredient.Value, remainingIngredientsNeeded.Count(ingredient.Key)));
                }

                if (remainingIngredientsNeeded.Count() == 0) {
                    return new IngredientList();
                }

                results = remainingIngredientsNeeded;
            }

            return results;
        }

        public List<string> GetAllToolsNeeded() {
            var toolsNeeded = new List<string>();

            foreach (var step in steps) {
                if (Recipes.allTools.Contains(step.use) && !Recipes.tools.Contains(step.use)) {
                    toolsNeeded.Add(step.use);
                }
                if (Recipes.allTools.Contains(step.on) && !Recipes.tools.Contains(step.on)) {
                    toolsNeeded.Add(step.on);
                }
            }

            return toolsNeeded;
        }

        public List<string> GetToolsNeeded(List<string> tools) {
            var toolsNeeded = new List<string>();

            foreach (var tool in tools) {
                if (!Util.HasItem(tool)) {
                    toolsNeeded.Add(tool);
                }
            }

            return toolsNeeded;
        }

        public List<string> GetToolsNeeded(IngredientList ingredients) {
            var steps = GetSteps(ingredients);
            var neededTools = new List<string>();

            foreach (var step in steps) {
                if (Recipes.allTools.Contains(step.use) && !Util.HasItem(step.use)) {
                    neededTools.Add(step.use);
                }
                if (Recipes.allTools.Contains(step.on) && !Util.HasItem(step.on)) {
                    neededTools.Add(step.on);
                }
            }

            return neededTools;
        }
    }
}
