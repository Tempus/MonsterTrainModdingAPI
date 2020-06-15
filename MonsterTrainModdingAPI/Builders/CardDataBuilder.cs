﻿using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Harmony;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ShinyShoe;
using MonsterTrainModdingAPI.Managers;
using MonsterTrainModdingAPI.Enums.MTCardPools;
using MonsterTrainModdingAPI.Enums.MTClans;
using MonsterTrainModdingAPI.Utilities;

namespace MonsterTrainModdingAPI.Builders
{
    public class CardDataBuilder
    {
        /// <summary>
        /// Unique string used to store and retrieve the card data.
        /// </summary>
        public string CardID { get; set; }
        /// <summary>
        /// The IDs of all card pools the card should be inserted into.
        /// </summary>
        public List<string> CardPoolIDs { get; set; }

        /// <summary>
        /// Ember cost of the card.
        /// </summary>
        public int Cost { get; set; }
        /// <summary>
        /// Determines whether the card has a normal ember cost or an X ember cost.
        /// </summary>
        public CardData.CostType CostType { get; set; }
        /// <summary>
        /// Name displayed on the card.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// ID of the clan the card is a part of. Leave null for clanless.
        /// Base game clan IDs should be retrieved via helper class "MTClanIDs".
        /// </summary>
        public string ClanID { get; set; }
        /// <summary>
        /// Custom description text appended to the end of the card.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Use an existing base game card's description key to copy the format of its description.
        /// Otherwise leave this blank.
        /// </summary>
        public string OverrideDescriptionKey { get; set; }

        /// <summary>
        /// Custom asset path to load card art from.
        /// </summary>
        public string AssetPath { get; set; }
        public AssetBundleLoadingInfo BundleLoadingInfo { get; set; }
        /// <summary>
        /// Use an existing base game card's art by filling this in with the appropriate card's asset reference information.
        /// </summary>
        public AssetReferenceGameObject CardArtPrefabVariantRef { get; set; }

        /// <summary>
        /// Append to this list to add new card effects. The Build() method recursively builds all nested builders.
        /// </summary>
        public List<CardEffectDataBuilder> EffectBuilders { get; set; }
        /// <summary>
        /// Append to this list to add new card traits. The Build() method recursively builds all nested builders.
        /// </summary>
        public List<CardTraitDataBuilder> TraitBuilders { get; set; }
        /// <summary>
        /// Append to this list to add new character triggers. The Build() method recursively builds all nested builders.
        /// </summary>
        public List<CharacterTriggerDataBuilder> EffectTriggerBuilders { get; set; }
        /// <summary>
        /// Append to this list to add new card triggers. The Build() method recursively builds all nested builders.
        /// </summary>
        public List<CardTriggerEffectDataBuilder> TriggerBuilders { get; set; }


        /// <summary>
        /// List of pre-built card effects.
        /// </summary>
        public List<CardEffectData> Effects { get; set; }
        /// <summary>
        /// List of pre-built card traits.
        /// </summary>
        public List<CardTraitData> Traits { get; set; }
        /// <summary>
        /// List of pre-built character triggers.
        /// </summary>
        public List<CharacterTriggerData> EffectTriggers { get; set; }
        /// <summary>
        /// List of pre-built card triggers.
        /// </summary>
        public List<CardTriggerEffectData> Triggers { get; set; }

        /// <summary>
        /// These upgrades are applied to all new instances of this card by default.
        /// </summary>
        public List<CardUpgradeData> StartingUpgrades { get; set; }

        /// <summary>
        /// Use an existing base game card's lore tooltip by adding its key to this list.
        /// </summary>
        public List<string> CardLoreTooltipKeys { get; set; }

        /// <summary>
        /// Whether or not the card has a target.
        /// </summary>
        public bool Targetless { get; set; }
        /// <summary>
        /// Whether or not the card targets a room.
        /// </summary>
        public bool TargetsRoom { get; set; }

        /// <summary>
        /// The class associated with the card.
        /// </summary>
        public ClassData LinkedClass { get; set; }
        /// <summary>
        /// The type of card: Spell, Monster, Blight, Scourge, or Invalid.
        /// </summary>
        public CardType CardType { get; set; }
        /// <summary>
        /// The card's rarity: Common, Uncommon, Rare, Champion, Starter
        /// </summary>
        public CollectableRarity Rarity { get; set; }

        /// <summary>
        /// Level at which the card is unlocked.
        /// </summary>
        public int UnlockLevel { get; set; }
        public List<CardData> SharedMasteryCards { get; set; }
        public CardData LinkedMasteryCard { get; set; }
        /// <summary>
        /// Whether or not this card is displayed in the logbook when counting all of the player's mastered cards.
        /// </summary>
        public bool IgnoreWhenCountingMastery { get; set; }

        /// <summary>
        /// A cache for the card's sprite so it doesn't have to be reloaded repeatedly.
        /// </summary>
        public Sprite SpriteCache { get; set; }
        /// <summary>
        /// In the event that art assets cannot be found, the game will search this for backup assets.
        /// </summary>
        public FallbackData FallbackData { get; set; }

        public CardDataBuilder()
        {
            this.Name = "";
            this.Description = "";
            this.OverrideDescriptionKey = "EmptyString-0000000000000000-00000000000000000000000000000000-v2";

            this.CardPoolIDs = new List<string>();
            this.EffectBuilders = new List<CardEffectDataBuilder>();
            this.TraitBuilders = new List<CardTraitDataBuilder>();
            this.EffectTriggerBuilders = new List<CharacterTriggerDataBuilder>();
            this.TriggerBuilders = new List<CardTriggerEffectDataBuilder>();
            this.Effects = new List<CardEffectData>();
            this.Traits = new List<CardTraitData>();
            this.EffectTriggers = new List<CharacterTriggerData>();
            this.Triggers = new List<CardTriggerEffectData>();
            this.SharedMasteryCards = new List<CardData>();
            this.StartingUpgrades = new List<CardUpgradeData>();
            this.CardLoreTooltipKeys = new List<string>();
        }

        /// <summary>
        /// Builds the CardData represented by this builder's parameters recursively
        /// and registers it and its components with the appropriate managers.
        /// </summary>
        /// <returns>The newly registered CardData</returns>
        public CardData BuildAndRegister()
        {
            var cardData = this.Build();
            API.Log(LogLevel.Debug, "Adding custom card: " + cardData.GetName());
            CustomCardManager.RegisterCustomCard(cardData, this.CardPoolIDs, BundleLoadingInfo);
            return cardData;
        }

        /// <summary>
        /// Builds the CardData represented by this builder's parameters recursively;
        /// i.e. all CardEffectBuilders in EffectBuilders will also be built.
        /// </summary>
        /// <returns>The newly created CardData</returns>
        public CardData Build()
        {
            if (this.Description != "")
            {
                this.TraitBuilders.Add(new CardTraitDataBuilder
                {
                    TraitStateName = "CardTraitCustomDescription",
                    ParamStr = "<size=50%><br><br></size>" + this.Description
                });
            }
            foreach (var builder in this.EffectBuilders)
            {
                this.Effects.Add(builder.Build());
            }
            foreach (var builder in this.TraitBuilders)
            {
                this.Traits.Add(builder.Build());
            }
            foreach (var builder in this.EffectTriggerBuilders)
            {
                this.EffectTriggers.Add(builder.Build());
            }
            foreach (var builder in this.TriggerBuilders)
            {
                this.Triggers.Add(builder.Build());
            }

            this.LinkedClass = CustomCardManager.SaveManager.GetAllGameData().FindClassData(this.ClanID);
            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            AccessTools.Field(typeof(CardData), "id").SetValue(cardData, this.CardID);
            if (this.CardArtPrefabVariantRef == null)
            {
                this.CreateAndSetCardArtPrefabVariantRef(this.AssetPath, this.AssetPath);
            }
            AccessTools.Field(typeof(CardData), "cardArtPrefabVariantRef").SetValue(cardData, this.CardArtPrefabVariantRef);
            AccessTools.Field(typeof(CardData), "cardLoreTooltipKeys").SetValue(cardData, this.CardLoreTooltipKeys);
            AccessTools.Field(typeof(CardData), "cardType").SetValue(cardData, this.CardType);
            AccessTools.Field(typeof(CardData), "cost").SetValue(cardData, this.Cost);
            AccessTools.Field(typeof(CardData), "costType").SetValue(cardData, this.CostType);
            AccessTools.Field(typeof(CardData), "effects").SetValue(cardData, this.Effects);
            AccessTools.Field(typeof(CardData), "effectTriggers").SetValue(cardData, this.EffectTriggers);
            AccessTools.Field(typeof(CardData), "fallbackData").SetValue(cardData, this.FallbackData);
            AccessTools.Field(typeof(CardData), "ignoreWhenCountingMastery").SetValue(cardData, this.IgnoreWhenCountingMastery);
            AccessTools.Field(typeof(CardData), "linkedClass").SetValue(cardData, this.LinkedClass);
            AccessTools.Field(typeof(CardData), "linkedMasteryCard").SetValue(cardData, this.LinkedMasteryCard);
            AccessTools.Field(typeof(CardData), "nameKey").SetValue(cardData, this.Name);
            AccessTools.Field(typeof(CardData), "overrideDescriptionKey").SetValue(cardData, this.OverrideDescriptionKey);
            AccessTools.Field(typeof(CardData), "rarity").SetValue(cardData, this.Rarity);
            AccessTools.Field(typeof(CardData), "sharedMasteryCards").SetValue(cardData, this.SharedMasteryCards);
            if (this.SpriteCache != null)
            {
                AccessTools.Field(typeof(CardData), "spriteCache").SetValue(cardData, this.SpriteCache);
            }
            AccessTools.Field(typeof(CardData), "startingUpgrades").SetValue(cardData, this.StartingUpgrades);
            AccessTools.Field(typeof(CardData), "targetless").SetValue(cardData, this.Targetless);
            AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(cardData, this.TargetsRoom);
            foreach (CardTraitData cardTraitData in this.Traits)
            {
                AccessTools.Field(typeof(CardTraitData), "paramCardData").SetValue(cardTraitData, cardData);
            }
            AccessTools.Field(typeof(CardData), "traits").SetValue(cardData, this.Traits);
            AccessTools.Field(typeof(CardData), "triggers").SetValue(cardData, this.Triggers);
            AccessTools.Field(typeof(CardData), "unlockLevel").SetValue(cardData, this.UnlockLevel);

            return cardData;
        }

        /// <summary>
        /// Creates an asset reference to an existing game file.
        /// Primarily useful for reusing base game art.
        /// Cards with custom art should not use this method.
        /// </summary>
        /// <param name="m_debugName">The asset's debug name (usually the path to it)</param>
        /// <param name="m_AssetGUID">The asset's GUID</param>
        public void CreateAndSetCardArtPrefabVariantRef(string m_debugName, string m_AssetGUID)
        {
            var assetReferenceGameObject = new AssetReferenceGameObject();
            AccessTools.Field(typeof(AssetReferenceGameObject), "m_debugName")
                    .SetValue(assetReferenceGameObject, m_debugName);
            AccessTools.Field(typeof(AssetReferenceGameObject), "m_AssetGUID")
                .SetValue(assetReferenceGameObject, m_AssetGUID);
            this.CardArtPrefabVariantRef = assetReferenceGameObject;

            this.AssetPath = m_AssetGUID;
        }

        /// <summary>
        /// Sets this card's clan to the clan whose type is passed in
        /// </summary>
        /// <param name="clanType">Must implement IMTClan</param>
        public void SetClan(Type clanType)
        {
            string clanID = MTClanIDs.GetIDForType(clanType);
            this.SetClan(clanID);
        }

        /// <summary>
        /// Sets this card's clan to the clan whose ID is passed in
        /// </summary>
        /// <param name="clanID">ID of the clan, most easily retrieved using the helper class "MTClanIDs"</param>
        public void SetClan(string clanID)
        {
            this.ClanID = clanID;
        }
        
        /// <summary>
        /// Adds this card to the cardpool whose type is passed in
        /// </summary>
        /// <param name="cardPoolType">Must implement IMTCardPool</param>
        public void AddToCardPool(Type cardPoolType)
        {
            this.CardPoolIDs.Add(MTCardPoolIDs.GetIDForType(cardPoolType));
        }

        /// <summary>
        /// Adds this card to the cardpool whose ID is passed in
        /// </summary>
        /// <param name="cardPoolID">ID of the card pool, most easily retrieved using the helper class "MTCardPoolIDs"</param>
        public void AddToCardPool(string cardPoolID)
        {
            this.CardPoolIDs.Add(cardPoolID);
        }
    }
}
