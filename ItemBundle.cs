using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Lib.Recipes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DoThingsBot {
    public enum CraftMode {
        None = 0,
        Unknown = 1,
        WeaponTinkering = 2,
        PrimaryPortal = 3,
        SecondaryPortal = 4,
        CheckSkills = 5,
        GiveBackItems = 6,
        Buff = 7,
        PortalGem = 8,
        Recipe = 9,
        Crafting = 10,
        InfiniteRations = 11,
        InfiniteLeather = 12,
        InfiniteDye = 13
    }

    public enum EquipMode {
        None = 0,
        Idle = 1,
        Buff = 2,
        Tinker = 3,
        SummonPortal = 4,
        Craft = 5
    }

    public enum CraftTargetType {
        None = 0,
        Unkown = 1,
        Weapon = 2,
        Armor = 3,
        Jewelry = 4
    }

    public enum WeaponType {
        None = 0,
        Wand = 1,
        Melee = 2,
        Missile = 3,
    }

    public class ItemBundle {
        public CraftMode craftMode = CraftMode.None;
        public CraftTargetType targetType = CraftTargetType.None;
        public WeaponType weaponType = WeaponType.None;

        public double successChance = 0;
        public string successChanceFullString = "";

        string invalidReason = "";
        bool isValid = false;
        string owner;

        private string buffProfile = "";

        private int UseItemId;
        private int UseItemOnId;
        public  int tinkerCount = 0;

        public PlayerData playerData;
        private EquipMode equipMode;
        public Recipe recipe;
        public List<RecipeStep> recipeSteps = new List<RecipeStep>();
        public IngredientList recipeIngredients = new IngredientList();
        public IngredientList originalRecipeIngredients = new IngredientList();
        private bool forceBuff = false;
        public bool IsImbue = false;
        public bool WasPaused = false;
        public bool IsPaused = false;
        public bool DidLoad = false;

        private string portalCommand = "";

        public ItemBundle() {
        }

        public ItemBundle(string playerName) {
            try {
                owner = playerName;

                if (File.Exists(Path.Combine(Util.GetResumablePlayersDataDirectory(), GetOwner() + ".json"))) {
                    WasPaused = true;
                }

                LoadPlayerData();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public string GetOwner() {
            try {
                return owner;
            }
            catch (Exception e) { Util.LogException(e); return "UnknownOwner"; }
        }

        internal IngredientList GetOriginalIngredientList() {
            return originalRecipeIngredients.Clone();
        }

        internal IngredientList GetValidRecipeIngredients() {
            var ingredients = GetIngredientList();
            var fullIngredients = recipe.GetFullIngredients().ingredients;

            foreach (var ingredient in ingredients.Clone().ingredients) {
                if (!fullIngredients.ContainsKey(ingredient.Key)) {
                    ingredients.RemoveAll(ingredient.Key);
                }
            }

            return ingredients;
        }

        internal void SetInfiniteDye(string v) {
            infiniteDye = v;
        }

        internal IngredientList GetIngredientList() {
            var ingredients = new IngredientList();

            foreach (var item in GetItems()) {
                var wo = CoreManager.Current.WorldFilter[item];
                if (wo != null) {
                    if (wo.ObjectClass == ObjectClass.Armor || wo.Values(BoolValueKey.Dyeable, false)) {
                        ingredients.Add("DYEABLE_ITEM", 1);
                    }
                    else {
                        ingredients.Add(wo.Name, wo.Values(LongValueKey.StackCount, 1));
                    }
                }
            }

            return ingredients;
        }

        internal void SetRecipeIngredientList(IngredientList recipeIngredients) {
            this.recipeIngredients = recipeIngredients.Clone();
            this.originalRecipeIngredients = recipeIngredients.Clone();
        }

        internal void SetOwner(string name) {
            owner = name;
        }

        public bool HasOwner() {
            try {
                return owner != null && owner.Length > 0;
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        public void SetCraftMode(CraftMode m) {
            try {
                craftMode = m;
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public bool GetForceBuffMode() {
            try {
                return forceBuff;
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        public void SetForceBuffMode(bool m) {
            try {
                forceBuff = m;
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public CraftMode GetCraftMode() {
            try {
                return craftMode;
            }
            catch (Exception e) { Util.LogException(e); return CraftMode.None; }
        }

        public bool IsPortalCraftMode() {
            return (craftMode == CraftMode.PrimaryPortal || craftMode == CraftMode.SecondaryPortal || craftMode == CraftMode.PortalGem);
        }

        public void SetPortalCommand(string command) {
            portalCommand = command;
        }

        public string GetPortalCommand() {
            return portalCommand;
        }

        public List<int> GetItems() {
            try {
                return playerData.itemIds;
            }
            catch (Exception e) { Util.LogException(e); return new List<int>(); }
        }

        public void SetBuffProfiles(string profiles) {
            buffProfile = profiles;
        }

        public string GetBuffProfiles() {
            return buffProfile;
        }

        public bool IsValid() {
            return isValid;
        }

        public void SetEquipMode(EquipMode mode) {
            equipMode = mode;
        }

        public EquipMode GetEquipMode() {
            return equipMode;
        }

        public void SetRecipe(Recipe recipe) {
            this.recipe = recipe;
            recipeSteps = recipe.GetSteps(GetIngredientList());
        }

        public Recipe GetRecipe() {
            return recipe;
        }

        public RecipeStep GetCurrentRecipeStep() {
            if (recipe == null) return null;
            var ingredients = GetIngredientList();

            if (!HasRecipeStepsLeft()) {
                if (ingredients.Contains("DYEABLE_ITEM") && !hasAssignedSteps) {
                    recipeSteps = new List<RecipeStep>(recipe.GetSteps(ingredients));
                    hasAssignedSteps = true;
                }
                else if (!ingredients.Contains("DYEABLE_ITEM")) {
                    recipeSteps = new List<RecipeStep>(recipe.GetSteps(ingredients));
                }
            }

            //Util.WriteToChat($"Found {recipeSteps.Count} steps to perform on: {string.Join(", ", ingredients.ingredients.Select(i=>i.Key).ToArray())}");

            return recipeSteps.Count > 0 ? recipeSteps[0] : null;
        }

        public bool HasRecipeStepsLeft() {
            return recipeSteps.Count > 0;
        }

        private string GetJsonDataPathForOwner() {
            if (File.Exists(Path.Combine(Util.GetResumablePlayersDataDirectory(), GetOwner() + ".json"))) {
                return Path.Combine(Util.GetResumablePlayersDataDirectory(), GetOwner() + ".json");
            }

            return Path.Combine(Util.GetPlayerDataDirectory(), GetOwner() + ".json");
        }

        private void LoadPlayerData() {
            try {
                if (File.Exists(GetJsonDataPathForOwner())) {
                    try {
                        string json = File.ReadAllText(GetJsonDataPathForOwner());

                        playerData = JsonConvert.DeserializeObject<PlayerData>(json);
                        DidLoad = true;
                    }
                    catch (Exception ex) {
                        //Util.LogException(ex);
                        playerData = new PlayerData(GetOwner());
                        File.Delete(GetJsonDataPathForOwner());
                        DidLoad = false;
                        return;
                    }
                }
                else {
                    playerData = new PlayerData(GetOwner());
                }

                if (playerData.itemIds.Count > 0) {
                    foreach (int id in playerData.itemIds) {
                        if (!playerData.stolenItemIds.Contains(id)) {
                            playerData.stolenItemIds.Add(id);
                        }
                    }
                }

                if (!WasPaused) {
                    playerData.itemIds.Clear();
                }
            }
            catch (Exception ex) {
                Util.LogException(ex);
                File.Delete(GetJsonDataPathForOwner());
            }
        }

        public void SavePlayerData() {
            try {
                if (recipe != null) {
                    playerData.recipe = recipe.name;
                }

                foreach (int id in playerData.itemIds) {
                    WorldObject wo = CoreManager.Current.WorldFilter[id];
                    if (wo != null && wo.HasIdData) {
                        if (playerData.itemDescriptions.ContainsKey(id)) {
                            playerData.itemDescriptions[id] = Util.GetFullLootName(wo);
                        }
                        else {
                            playerData.itemDescriptions.Add(id, Util.GetFullLootName(wo));
                        }
                    }
                }

                string json = JsonConvert.SerializeObject(playerData, Formatting.Indented);
                File.WriteAllText(GetJsonDataPathForOwner() + ".new", json);
                File.Copy(GetJsonDataPathForOwner() + ".new", GetJsonDataPathForOwner(), true);
                File.Delete(GetJsonDataPathForOwner() + ".new");
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public bool AddWorldObject(WorldObject wo, bool skipCheck=false) {
            try {
                if (!skipCheck && !CanAddWorldObject(wo)) {
                    isValid = false;
                    return false;
                }

                playerData.itemIds.Add(wo.Id);

                if (!skipCheck) {
                    SetTargetCraftType(wo);
                    isValid = CheckIsValid();
                    if (isValid) SetItemTargets();
                }

                return isValid;
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        private void SetTargetCraftType(WorldObject wo) {
            try {
                if (targetType == CraftTargetType.None || targetType == CraftTargetType.Unkown) {
                    if (IsWandOrWeapon(wo)) {
                        targetType = CraftTargetType.Weapon;

                        if (wo.ObjectClass == ObjectClass.WandStaffOrb) {
                            weaponType = WeaponType.Wand;
                        }
                        else if (wo.ObjectClass == ObjectClass.MeleeWeapon) {
                            weaponType = WeaponType.Melee;
                        }
                        else if (wo.ObjectClass == ObjectClass.MissileWeapon) {
                            weaponType = WeaponType.Missile;
                        }
                    }
                    else if (wo.ObjectClass == ObjectClass.Armor) {
                        targetType = CraftTargetType.Armor;
                    }
                    else if (wo.ObjectClass == ObjectClass.Jewelry) {
                        targetType = CraftTargetType.Jewelry;
                    }

                    if (targetType == CraftTargetType.None) {
                        targetType = CraftTargetType.Unkown;
                    }
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }

        private string CraftModeToString(CraftMode cm) {
            try {
                switch (cm) {
                    case CraftMode.None:
                        return "None";
                    case CraftMode.Unknown:
                        return "Unknown";
                    case CraftMode.WeaponTinkering:
                        return "Weapon Tinkering";
                    case CraftMode.Recipe:
                        return "Recipe";

                    default:
                        return "Unknown";
                }
            }
            catch (Exception e) { Util.LogException(e); return "Unknown"; }
        }

        public bool CheckValidRecipe() {
            var ingredientsProvided = new IngredientList();
            var hasDyeableItem = false;

            foreach (var itemId in GetItems()) {
                var wo = CoreManager.Current.WorldFilter[itemId];

                if (wo != null) {
                    if (wo.Values(BoolValueKey.Dyeable)) {
                        if (hasDyeableItem == true) {
                            invalidReason = "You already added a Dyeable item, please only add one at a time.";
                            return false;
                        }
                        hasDyeableItem = true;
                        ingredientsProvided.Add("DYEABLE_ITEM", 1);
                    }
                    else {
                        ingredientsProvided.Add(wo.Name, wo.Values(LongValueKey.StackCount, 1));
                    }
                }
            }

            var results = Recipes.FindByIngredients(ingredientsProvided);

            if (results.Count > 1) {
                var hasIngredients = false;
                if (results[0].HasAllIngredients(ingredientsProvided)) hasIngredients = true;

                if (hasIngredients) {
                    return true;
                }
                else {
                    var possibleRecipes = new List<string>();
                    var more = 0;
                    foreach (var recipe in results) {
                        if (possibleRecipes.Count < 5) possibleRecipes.Add(recipe.name);
                        else more++;
                    }

                    string moreText = more > 0 ? $" (+{more} more)" : "";

                    invalidReason = ("If you had more ingredients, I could make the following: " + String.Join(", ", possibleRecipes.ToArray()) + moreText);
                    invalidReason += ("\nTell me 'recipe Wedding Cake' for more information about a recipe.");
                    return false;
                }
            }
            else if (results.Count == 1) {
                var neededIngredients = results[0].GetNeededIngredients(ingredientsProvided);

                if (neededIngredients.Count() == 0) {
                    return true;
                }
                else {
                    invalidReason = ("I am missing the following ingredients to make " + results[0].name + ": " + neededIngredients.Summary() + String.Join(", ", results[0].GetAllToolsNeeded().ToArray()));
                    return false;
                }
            }

            invalidReason = ("I don't know how to make anything with those ingredients: " + ingredientsProvided.Summary());
            return false;
        }

        public bool CheckIsValidFinal() {
            try {
                if (GetCraftMode() == CraftMode.InfiniteLeather)
                    return true;
                if (GetCraftMode() == CraftMode.InfiniteDye)
                    return true;

                if (GetSalvages().Count == 0) {
                    return CheckValidRecipe();
                }

                int targetId = GetTargetId();
                WorldObject targetWo = CoreManager.Current.WorldFilter[targetId];

                if (HasOnlyBuffItems()) return true;

                // no target
                if (targetId == 0) {
                    if (GetSalvages().Count > 0) {
                        invalidReason = "You didn't add an item for me to use those salvages on.";
                    }
                    else {
                        invalidReason = "You didn't add any items?";
                    }
                    return false;
                }
                else if (GetSalvages().Count <= 0) {
                    invalidReason = "You need to add salvages with that!";
                    return false;
                }

                // check salvages will work on this thing
                foreach (var salvage in GetSalvages()) {
                    bool canApply = false;

                    switch (targetWo.ObjectClass) {
                        case ObjectClass.MeleeWeapon: canApply = Salvage.IsAbleToApplyToMeleeWeapon(salvage); break;
                        case ObjectClass.MissileWeapon: canApply = Salvage.IsAbleToApplyToMissileWeapon(salvage); break;
                        case ObjectClass.WandStaffOrb: canApply = Salvage.IsAbleToApplyToAnyMagicWeapon(salvage); break;
                        case ObjectClass.Armor: canApply = Salvage.IsAbleToApplyToArmor(salvage); break;
                        case ObjectClass.Jewelry: canApply = Salvage.IsAbleToApplyToJewelry(salvage); break;
                    }

                    if (!CheckSalvageAgainstTarget(salvage, targetWo)) {
                        invalidReason = String.Format("I can't use the {0} on the {1}", Util.GetItemName(salvage), Util.GetItemName(targetWo));
                        return false;
                    }
                }

                return CheckIsValid();
            }

            catch (Exception e) { Util.LogException(e); return false; }
        }

        internal void Unpause() {
            try {
                IsPaused = false;
                SavePlayerData();

                var src = Path.Combine(Util.GetResumablePlayersDataDirectory(), GetOwner() + ".json");
                var dest = Path.Combine(Util.GetPlayerDataDirectory(), GetOwner() + ".json");

                if (!File.Exists(src)) return;
                if (File.Exists(dest)) File.Delete(dest);

                File.Move(src, dest);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        internal void Pause() {
            try {
                IsPaused = true;
                SavePlayerData();

                var src = Path.Combine(Util.GetPlayerDataDirectory(), GetOwner() + ".json");
                var dest = Path.Combine(Util.GetResumablePlayersDataDirectory(), GetOwner() + ".json");

                File.Copy(src, dest, true);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        internal void MarkAsActive() {
            try {
                SavePlayerData();

                var src = Path.Combine(Util.GetPlayerDataDirectory(), GetOwner() + ".json");
                var dest = Path.Combine(Util.GetResumablePlayersDataDirectory(), GetOwner() + ".json");

                File.Copy(src, dest, true);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public bool CheckIsValid() {
            return true;
        }

        public bool HasItemsLeftToWorkOn() {
            try {
                return GetSalvages().Count > 0;
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        public int GetUseItemTarget() {
            IsImbue = GetImbueSalvages().Count > 0;

            return UseItemId;
        }

        public int GetUseItemOnTarget() {
            return UseItemOnId;
        }

        private bool CanAddWorldObject(WorldObject wo) {
            try
            {

                if (Util.IsValidBuffItem(wo) && Util.CanUseBuffItem(wo))
                {
                    if (Util.IsRare(wo) && !Spells.CanUseRare())
                    {
                        invalidReason = $"I need to wait {Util.GetFriendlyTimeDifference((ulong)Spells.GetTimeUntilCanUseRareAgain())} until I can use another rare.";
                        return false;
                    }

                    return true;
                }

                if (!CheckValidItem(wo))
                {
                    return false;
                }

                if (GetCraftMode() == CraftMode.InfiniteLeather)
                {
                    if (wo.Values(LongValueKey.Material, 0) > 0 && wo.Values(LongValueKey.Workmanship, 0) > 0)
                    {
                        return true;
                    }
                    else
                    {
                        invalidReason = "You can only add leather to lootgen items.";
                        return false;
                    }
                }

                if (GetCraftMode() == CraftMode.InfiniteDye)
                {
                    if (wo.Values(BoolValueKey.Dyeable, false))
                    {
                        return true;
                    }
                    else
                    {
                        invalidReason = $"That item doesn't appear to be dyeable: {Util.GetGameItemDisplayName(wo)}";
                        return false;
                    }
                }

                WorldObject targetItem = GetTargetItem();

                // only one imbue allowed
                if ((Salvage.IsImbueSalvage(wo) || Salvage.IsImbueSalvageFP(wo)) && GetImbueSalvages().Count > 0) {
                    invalidReason = String.Format("You can only add one imbue salvage to an item, you already added {0}", Util.GetItemName(GetImbueSalvages()[0]));
                    return false;
                }

                // we have a target set already
                if (targetItem != null) {
                    // you can only add one weapon
                    if (IsWandOrWeapon(wo)) {
                        invalidReason = String.Format("You can only add one target item to be tinkered, you already added your {0}!", Util.GetItemName(wo));
                        return false;
                    }
                    else if (Salvage.IsSalvage(wo)) {
                        // can we add this salvage to our target item?
                        if (!CheckSalvageAgainstTarget(wo, targetItem)) {
                            if (invalidReason == null || invalidReason == "") {
                                invalidReason = String.Format("I can't add {0} to your {1}, that's the wrong type of salvage!", Util.GetItemName(wo), Util.GetItemName(targetItem));
                            }
                            return false;
                        }

                        if (targetItem.Values(LongValueKey.NumberTimesTinkered) + GetSalvages().Count >= 10) {
                            invalidReason = String.Format("You can only tinker an item 10 times.  That's too much salvage.");
                            return false;
                        }
                    }
                }
                // no item target set
                else {

                    if (Salvage.IsSalvage(wo) && GetSalvages().Count >= 10) {
                        invalidReason = String.Format("You can only tinker an item 10 times.  That's too much salvage.");
                        return false;
                    }

                    // looks like a new target item
                    if (IsWorldObjectTinkerable(wo) && !Salvage.IsSalvage(wo)) {

                        if (wo.Values(LongValueKey.NumberTimesTinkered) >= 10) {
                            invalidReason = String.Format("That item has already been tinkered 10 times, that's the max!");
                            return false;
                        }

                        // and we have salvages, so check them
                        if (GetSalvages().Count > 0) {
                            foreach (var salvage in GetSalvages()) {
                                if (!CheckSalvageAgainstTarget(salvage, wo)) {
                                    if (invalidReason == null || invalidReason.Length == 0) {
                                        invalidReason = String.Format("I wouldn't be able to apply the {0} to your {1}", Util.GetItemName(salvage), Util.GetItemName(wo));
                                    }
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        private bool CheckValidItem(WorldObject wo) {
            try {
                return true;
                /*
                if (IsWandOrWeapon(wo) || wo.ObjectClass == ObjectClass.Armor || wo.ObjectClass == ObjectClass.Jewelry) {
                    if (IsWorldObjectTinkerable(wo)) {
                        return true;
                    }

                    invalidReason = "I can only work with loot generated items.";
                    return false;
                }
                else if (wo.ObjectClass == ObjectClass.Salvage) {
                    SalvageData data = Salvage.GetFromWorldObject(wo);

                    if (wo.Values(LongValueKey.UsesRemaining) != 100) {
                        invalidReason = "I can only work with full bags of Salvage.";
                        return false;
                    }

                    return true;
                }

                if (Util.IsValidBuffItem(wo)) {
                    if (Util.CanUseBuffItem(wo)) return true;

                    invalidReason = Spells.itemEnchantmentAlreadyHadReason;
                    return false;
                }

                invalidReason = String.Format("I don't know how to work with that {0}.", Util.GetItemName(wo));

                return false;
                */
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        public bool CheckSalvageAgainstTarget(WorldObject salvage, WorldObject targetItem) {
            try {
                // make sure we can add this salvage to the target item

                /*
                 *  this is wrong....
                if (Salvage.IsImbueSalvage(salvage) && targetItem.Values(22, false) == true) { // multi-strike
                    invalidReason = "I can't imbue Multi-Strike weapons.";
                    return false;
                }
                */

                if ((Salvage.IsImbueSalvage(salvage) || Salvage.IsImbueSalvageFP(salvage)) && targetItem.Values(LongValueKey.Imbued) > 0) {
                    invalidReason = String.Format("I can't add {0} to your {1}, it's already imbued!", Util.GetItemName(salvage), Util.GetItemName(targetItem));
                    return false;
                }

                switch (targetItem.ObjectClass) {
                    case ObjectClass.MeleeWeapon: return Salvage.IsAbleToApplyToMeleeWeapon(salvage);
                    case ObjectClass.MissileWeapon: return Salvage.IsAbleToApplyToMissileWeapon(salvage);
                    case ObjectClass.WandStaffOrb: return Salvage.IsAbleToApplyToAnyMagicWeapon(salvage);
                    case ObjectClass.Armor: return Salvage.IsAbleToApplyToArmor(salvage);
                    case ObjectClass.Jewelry: return Salvage.IsAbleToApplyToJewelry(salvage);
                    default: return false;
                }
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        public bool HasItems() {
            return playerData.itemIds.Count > 0;
        }

        public List<int> GetStolenItems() {
            try {
                List<int> stolenIds = new List<int>();
                List<int> idsIveSeen = new List<int>(playerData.itemDescriptions.Keys);

                foreach (var wo in CoreManager.Current.WorldFilter.GetInventory()) {
                    if ((idsIveSeen.Contains(wo.Id) || playerData.itemIds.Contains(wo.Id)) && !stolenIds.Contains(wo.Id)) {
                        stolenIds.Add(wo.Id);
                    }
                }

                return stolenIds;
            }
            catch (Exception e) { Util.LogException(e); return new List<int>(); }
        }

        List<int> destroyedIds = new List<int>();

        public void SetItemDestroyed(int id) {
            try {
                playerData.itemIds.Remove(id);
                destroyedIds.Add(id);
                Util.WriteToChat("Item " + id + " marked as destroyed");

                SavePlayerData();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public void RemoveItem(int id) {
            try {
                if (playerData.itemIds.Contains(id)) {
                    playerData.itemIds.Remove(id);
                    Util.WriteToChat("Item " + id + " marked as removed");
                }

                SavePlayerData();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public WorldObject GetTargetItem() {
            return CoreManager.Current.WorldFilter[GetTargetId()];
        }

        public int GetTargetId() {
            try {
                if (GetWeapon() != null) {
                    return GetWeapon().Id;
                }
                if (GetArmor() != null) {
                    return GetArmor().Id;
                }
                if (GetJewelry() != null) {
                    return GetJewelry().Id;
                }

                return 0;
            }
            catch (Exception e) { Util.LogException(e); return 0; }
        }

        public string sortedSalvageNames;
        private bool hasAssignedSteps;

        public string infiniteDye { get; private set; }

        public void SetItemTargets() {
            try {
                List<WorldObject> salvages = GetWeaponTinkeringSalvages();

                // imbue first, then lowest wk, then FP
                salvages.Sort(
                    delegate (WorldObject p1, WorldObject p2) {
                        if (Salvage.IsImbueSalvage(p1) && Salvage.IsImbueSalvage(p2)) {
                            return 0;
                        }
                        else if (Salvage.IsImbueSalvage(p1)) {
                            return -1;
                        }
                        else if (Salvage.IsImbueSalvage(p2)) {
                            return 1;
                        }
                        else if (Salvage.IsImbueSalvageFP(p1) && Salvage.IsImbueSalvageFP(p2))
                        {
                            return 11;
                        }
                        else if (Salvage.IsImbueSalvageFP(p1)) {
                            return 12;
                        }
                        else if (Salvage.IsImbueSalvageFP(p2))
                        {
                            return 13;
                        }
                        else {
                            return p1.Values(DoubleValueKey.SalvageWorkmanship).CompareTo(p2.Values(DoubleValueKey.SalvageWorkmanship));
                        }
                    }
                );
                string itemNames = "";

                foreach (WorldObject wo in salvages) {
                    int id = wo.Id;
                    WorldObject item = CoreManager.Current.WorldFilter[id];

                    if (item == null) continue;

                    if (itemNames.Length > 0) itemNames += ", ";

                    if (Salvage.IsSalvage(item)) {
                        itemNames += Util.GetItemShortName(item);
                    }
                    else {
                        itemNames += Util.GetItemShortName(item);
                    }
                }

                sortedSalvageNames = itemNames;

                if (salvages.Count > 0) {
                    UseItemId = salvages[0].Id;
                }

                UseItemOnId = GetTargetId();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public static bool IsWandOrWeapon(WorldObject wo) {
            return (wo.ObjectClass == ObjectClass.MeleeWeapon ||
                wo.ObjectClass == ObjectClass.MissileWeapon ||
                wo.ObjectClass == ObjectClass.WandStaffOrb);
        }

        public string GetInvalidReason() {
            return invalidReason.Length > 0 ? invalidReason : null;
        }

        public void SetItemMissing(int id) {
            try {
                if (playerData.itemIds.Contains(id)) {
                    playerData.itemIds.Remove(id);
                    if (!playerData.missingItemIds.Contains(id)) {
                        playerData.missingItemIds.Add(id);
                    }
                }

                SavePlayerData();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public void AddStolenItemsToMainItems() {
            try {
                foreach (int id in playerData.stolenItemIds) {
                    if (!playerData.itemIds.Contains(id)) {
                        playerData.itemIds.Add(id);
                    }
                }
                playerData.stolenItemIds.Clear();

                SavePlayerData();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public string GetItemNames() {
            try {
                string itemNames = "";

                foreach (int id in playerData.itemIds) {
                    WorldObject item = CoreManager.Current.WorldFilter[id];

                    if (item == null) continue;

                    if (itemNames.Length > 0) itemNames += ", ";

                    if (Salvage.IsSalvage(item)) {
                        itemNames += Salvage.GetName(item);
                    }
                    else {
                        itemNames += Util.GetItemName(item);
                    }
                }

                return itemNames;
            }
            catch (Exception e) { Util.LogException(e); return ""; }
        }

        public string GetItemNames(bool useCache) {
            try {
                string itemNames = "";

                if (useCache == false) return GetItemNames();

                foreach (int id in playerData.itemIds) {
                    string name = "";

                    if (playerData.itemNames.ContainsKey(id)) {
                        name = playerData.itemNames[id];
                    }

                    if (name.Length > 0) {
                        if (itemNames.Length > 0) itemNames += ", ";

                        itemNames += name;
                    }
                }

                return itemNames;
            }
            catch (Exception e) { Util.LogException(e); return ""; }
        }

        public string GetStolenItemsNames() {
            try {
                string itemNames = "";

                foreach (int id in GetStolenItems()) {
                    string name = "";

                    if (playerData.itemNames.ContainsKey(id)) {
                        name = playerData.itemNames[id];
                    }

                    if (name.Length > 0) {
                        if (itemNames.Length > 0) itemNames += ", ";

                        itemNames += name;
                    }
                }

                return itemNames;
            }
            catch (Exception e) { Util.LogException(e); return ""; }
        }

        public void ClearItems() {
            try {
                isValid = true;
                playerData.itemIds.Clear();
                SavePlayerData();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public WorldObject GetItemByObjectClass(ObjectClass objectClass) {
            foreach (int id in playerData.itemIds) {
                WorldObject item = CoreManager.Current.WorldFilter[id];

                if (item == null) continue;

                if (item.ObjectClass == objectClass) {
                    return item;
                }
            }

            return null;
        }

        public WorldObject GetWeapon() {
            foreach (int id in playerData.itemIds) {
                WorldObject item = CoreManager.Current.WorldFilter[id];

                if (item == null) continue;

                if (IsWandOrWeapon(item)) {
                    return item;
                }
            }

            return null;
        }

        public WorldObject GetArmor() {
            return GetItemByObjectClass(ObjectClass.Armor);
        }

        public WorldObject GetJewelry() {
            return GetItemByObjectClass(ObjectClass.Jewelry);
        }

        public List<WorldObject> GetImbueSalvages() {
            try {
                List<WorldObject> imbueSalvages = new List<WorldObject>();

                foreach (int id in playerData.itemIds) {
                    WorldObject item = CoreManager.Current.WorldFilter[id];

                    if (item != null && (Salvage.IsImbueSalvage(item) || Salvage.IsImbueSalvageFP(item))) {
                        imbueSalvages.Add(item);
                    }
                }

                return imbueSalvages;
            }
            catch (Exception e) { Util.LogException(e); return new List<WorldObject>(); }
        }

        public List<WorldObject> GetWeaponTinkeringSalvages() {
            try {
                List<WorldObject> weaponTinkeringSalvages = new List<WorldObject>();

                foreach (int id in playerData.itemIds) {
                    WorldObject item = CoreManager.Current.WorldFilter[id];

                    if (item != null && Salvage.IsSalvage(item)) {
                        weaponTinkeringSalvages.Add(item);
                    }
                }

                return weaponTinkeringSalvages;
            }
            catch (Exception e) { Util.LogException(e); return new List<WorldObject>(); }
        }

        public List<WorldObject> GetWeaponImbueSalvages() {
            try {
                List<WorldObject> weaponImbueSalvages = new List<WorldObject>();

                foreach (int id in playerData.itemIds) {
                    WorldObject item = CoreManager.Current.WorldFilter[id];

                    if (item != null && Salvage.IsWeaponImbueSalvage(item)) {
                        weaponImbueSalvages.Add(item);
                    }
                }

                return weaponImbueSalvages;
            }
            catch (Exception e) { Util.LogException(e); return new List<WorldObject>(); }
        }

        public List<WorldObject> GetSalvages() {
            try {
                List<WorldObject> salvages = new List<WorldObject>();

                foreach (int id in playerData.itemIds) {
                    WorldObject item = CoreManager.Current.WorldFilter[id];

                    if (item != null && Salvage.IsSalvage(item)) {
                        salvages.Add(item);
                    }
                }

                return salvages;
            }
            catch (Exception e) { Util.LogException(e); return new List<WorldObject>(); }
        }

        private bool IsWorldObjectTinkerable(WorldObject wo) {
            try {
                if (wo != null && wo.Values(LongValueKey.Workmanship) >= 1) {
                    return true;
                }
                else {
                    return false;
                }
            }
            catch (Exception e) { Util.LogException(e); return false; }
        }

        private CraftMode GetCraftModeFromItem(WorldObject wo) {
            try {
                if (IsWandOrWeapon(wo)) {
                    return CraftMode.WeaponTinkering;
                }
                else if (Salvage.IsSalvage(wo)) {
                    SalvageData salvageData = Salvage.GetFromWorldObject(wo);

                    switch (salvageData.tinkerType) {
                        case TinkerType.Weapon:
                            return CraftMode.WeaponTinkering;
                        default:
                            return CraftMode.Unknown;
                    }
                }
            }
            catch (Exception e) { Util.LogException(e); }

            return CraftMode.Unknown;
        }

        internal bool HasBuffItems() {
            foreach (var item in playerData.itemIds) {
                var wo = CoreManager.Current.WorldFilter[item];

                if (Util.IsValidBuffItem(wo)) {
                    return true;
                }
            }

            return false;
        }

        internal List<int> GetBuffItems() {
            var buffItems = new List<int>();

            foreach (var item in playerData.itemIds) {
                var wo = CoreManager.Current.WorldFilter[item];

                if (Util.IsValidBuffItem(wo)) {
                    buffItems.Add(wo.Id);
                }
            }

            return buffItems;
        }

        internal int GetBuffItem() {
            var items = GetBuffItems();

            if (items.Count > 0) return items[0];

            return 0;
        }

        internal bool HasOnlyBuffItems() {
            var buffItems = GetBuffItems();

            return buffItems.Count == playerData.itemIds.Count;
        }
    }
}
