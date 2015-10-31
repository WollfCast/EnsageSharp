﻿using System;
using System.Linq;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;

namespace SupportSharp
{
    internal class Program
    {
        private static Hero me;
        private static Entity fountain;
        private static bool loaded;
        private const Key SupportKey = Key.Space;
        private static Item Urn, Meka, Guardian, Arcane, LotusOrb, Medallion, SolarCrest;
        private static Hero needMana;
        private static Hero needMeka;
        private static Hero target;

        private static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.Load();
            Drawing.OnDraw += Drawing_OnDraw;

            //Items
            Urn = null;
            Meka = null;
            Guardian = null;
            Arcane = null;
            LotusOrb = null;

            loaded = false;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (loaded)
            {
                var mode = Game.IsKeyDown(SupportKey) ? "ON" : "OFF";
                Drawing.DrawText("Auto Support is: " + mode + ". Hotkey: " + SupportKey + "",
                    new Vector2(Drawing.Width*5/100, Drawing.Height*8/100), Color.LightGreen, FontFlags.DropShadow);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectMgr.LocalHero;


                if (!Game.IsInGame || Game.IsWatchingGame || me == null || !Support(me.ClassID))
                {
                    return;
                }
                loaded = true;
            }

            if (me == null || !me.IsValid)
            {
                loaded = false;
                me = ObjectMgr.LocalHero;
                return;
            }

            if (Game.IsPaused)
            {
                return;
            }

            Urn = me.FindItem("item_urn_of_shadows");
            Meka = me.FindItem("item_mekansm");
            Guardian = me.FindItem("item_guardian_greaves");
            Arcane = me.FindItem("item_arcane_boots");
            LotusOrb = me.FindItem("item_lotus_orb");
            Medallion = me.FindItem("item_medallion_of_courage");
            SolarCrest = me.FindItem("item_solar_crest");

            needMana = null;
            needMeka = null;

            if (Game.IsKeyDown(SupportKey))
            {
                var allies = ObjectMgr.GetEntities<Hero>().Where(ally => ally.Team == me.Team).ToList();
                fountain =
                    ObjectMgr.GetEntities<Entity>()
                        .First(entity => entity.ClassID == ClassID.CDOTA_Unit_Fountain && entity.Team == me.Team);


                if (allies.Any())
                {
                    foreach (var ally in allies)
                    {
                        if (!ally.IsIllusion() && ally.IsAlive && ally.Health > 0 && me.IsAlive && !me.IsChanneling() &&
                            me.Distance2D(fountain) > 2000 &&
                            !me.IsInvisible())
                        {
                            if ((ally.MaximumHealth - ally.Health) > (450 + ally.HealthRegeneration*10) &&
                                me.Distance2D(ally) <= 2000 &&
                                (me.Mana >= 225 || Guardian != null))
                            {
                                if (needMeka == null || (needMeka != null && me.Distance2D(needMeka) <= 750))
                                {
                                    needMeka = ally;
                                }
                            }

                            if (Urn != null && Urn.CanBeCasted(ally) && Urn.CurrentCharges > 0 &&
                                !ally.Modifiers.Any(x => x.Name == "modifier_item_urn_heal"))
                            {
                                if (me.Distance2D(ally) <= 950 && !IsInDanger(ally) && Utils.SleepCheck("Urn"))
                                {
//                                    Console.WriteLine("Using Urn");
                                    Urn.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "Urn");
                                }
                                if (ally.Modifiers.Any(x => x.Name == "modifier_wisp_tether") &&
                                    (ally.MaximumHealth - ally.Health) >= 600 && Utils.SleepCheck("Urn"))
                                {
                                    Urn.UseAbility(me);
                                    Utils.Sleep(100 + Game.Ping, "Urn");
                                }
                            }

                            if (Arcane != null && Arcane.Cooldown == 0)
                            {
                                if ((ally.MaximumMana - ally.Mana) >= 135 && me.Distance2D(ally) < 2000 && me.Mana >= 35)
                                {
                                    if (needMana == null || (needMana != null && me.Distance2D(needMana) <= 600))
                                    {
                                        needMana = ally;
                                    }
                                }
                            }

                            if (IsInDanger(ally))
                            {
                                if (LotusOrb != null && LotusOrb.Cooldown == 0 && Utils.SleepCheck("LotusOrb") &&
                                    me.Distance2D(ally) <= LotusOrb.CastRange + 50)
                                {
                                    LotusOrb.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "LotusOrb");
                                }

                                if (Medallion != null && Medallion.Cooldown == 0 &&
                                    me.Distance2D(ally) <= Medallion.CastRange + 50 && Utils.SleepCheck("Medallion"))
                                {
                                    Medallion.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "Medallion");
                                }

                                if (SolarCrest != null && SolarCrest.Cooldown == 0 &&
                                    me.Distance2D(ally) <= SolarCrest.CastRange + 50 && Utils.SleepCheck("SolarCrest"))
                                {
                                    SolarCrest.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "SolarCrest");
                                }
                            }
                        }
                    }
                }

                if (needMeka != null &&
                    ((Guardian != null && Guardian.CanBeCasted()) || (Meka != null && Meka.CanBeCasted())) &&
                    me.Distance2D(needMeka) <= 750)
                {
                    if (Meka != null)
                    {
//                        Console.WriteLine("Using Meka");
                        Meka.UseAbility();
                    }
                    else
                    {
//                        Console.WriteLine("Using Guardian");
                        Guardian.UseAbility();
                    }
                }
                if (needMana != null && Arcane != null && Arcane.CanBeCasted() && me.Distance2D(needMana) <= 600)
                {
                    Console.Write("Using Arcane");
                    Arcane.UseAbility();
                }


                if (Support(me.ClassID))
                {
                    if (Utils.SleepCheck("Logging hero support"))
                    {
//                        Console.WriteLine("Hero is support!");
//                        Console.WriteLine("Checking classID");
                        Utils.Sleep(100 + Game.Ping, "Logging hero support");
                    }

                    switch (me.ClassID)
                    {
                        case ClassID.CDOTA_Unit_Hero_Abaddon:
                            Save(me, me.Spellbook.SpellW, new float[] {15, 15, 15, 15}, me.Spellbook.SpellW.CastRange);
                            Heal(me, me.Spellbook.SpellQ, new float[] {100, 150, 200, 250},
                                800,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Chen:
                            Save(me, me.Spellbook.SpellE, new float[] {6, 5, 4, 3}, me.Spellbook.SpellE.CastRange);
                            Heal(me, me.Spellbook.SpellR, new float[] {200, 300, 400},
                                (int) me.Spellbook.SpellR.CastRange, 2);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Dazzle:
//                            Console.WriteLine("Hero is dazzle!");
                            Save(me, me.Spellbook.SpellW, new float[] {5, 5, 5, 5}, me.Spellbook.SpellW.CastRange);
                            Heal(me, me.Spellbook.SpellE, new float[] {80, 100, 120, 140},
                                750,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Enchantress:
                            Heal(me, me.Spellbook.SpellE, new float[] {400, 600, 800, 1000},
                                275, 2);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Legion_Commander:
                            Heal(me, me.Spellbook.SpellW, new float[] {150, 200, 250, 300},
                                800,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Necrolyte:
                            Heal(me, me.Spellbook.SpellQ, new float[] {70, 90, 110, 130},
                                475,
                                2);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Omniknight:
                            Heal(me, me.Spellbook.SpellQ, new float[] {90, 180, 270, 360},
                                950,
                                1);
                            Save(me, me.Spellbook.SpellW, new float[] {6, 8, 10, 12}, me.Spellbook.SpellW.CastRange);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Oracle:
                            Save(me, me.Spellbook.SpellR, new float[] {6, 7, 8}, me.Spellbook.SpellR.CastRange);
                            Heal(me, me.Spellbook.SpellE, new float[] {99, 198, 297, 396},
                                750,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Treant:
                            Heal(me, me.Spellbook.SpellE, new float[] {60, 105, 150, 195},
                                (int) me.Spellbook.SpellE.CastRange, 1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Undying:
                            var unitsAround =
                                ObjectMgr.GetEntities<Entity>()
                                    .Where(entity => entity.IsAlive && me.Distance2D(entity) <= 1300).ToList();

                            if (unitsAround.Any())
                            {
                                var unitCount = unitsAround.Count;
                                var healperUnit = new[] {18, 22, 36, 30};

                                Heal(me, me.Spellbook.SpellW,
                                    new float[]
                                    {
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1],
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1],
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1],
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1]
                                    },
                                    750, 1);
                            }
                            break;
                        case ClassID.CDOTA_Unit_Hero_Winter_Wyvern:
                            Heal(me, me.Spellbook.SpellE, new[] {0.03f, 0.04f, 0.05f, 0.06f},
                                1000, 1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_WitchDoctor:
                            Heal(me, me.Spellbook.SpellW, new float[] {16, 24, 32, 40}, 500,
                                3);
                            break;
                    }
                }

                if (target != null && (!target.IsValid || !target.IsVisible || !target.IsAlive || target.Health <= 0))
                {
                    target = null;
                }
                var canCancel = Orbwalking.CanCancelAnimation();
                if (canCancel)
                {
                    if (target != null && !target.IsVisible && !Orbwalking.AttackOnCooldown(target))
                    {
                        target = me.ClosestToMouseTarget();
                    }
                    else if (target == null || !Orbwalking.AttackOnCooldown(target))
                    {
                        var bestAa = me.BestAATarget();
                        if (bestAa != null)
                        {
                            target = me.BestAATarget();
                        }
                    }
                }

                Orbwalking.Orbwalk(target, Game.Ping, attackmodifiers: true);
            }
        }

        private static void Save(Hero self, Ability saveSpell, float[] duration, uint castRange = 0,
            int targettingType = 1)
        {
            if (saveSpell != null && saveSpell.CanBeCasted())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible() || !me.Modifiers.Any(x => x.Name == "modifier_treant_natures_guise")))
                {
                    var allies =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(
                                x => x.Team == me.Team && self.Distance2D(x) <= castRange && IsInDanger(x) && x.IsAlive)
                            .ToList();

                    if (allies.Any())
                    {
                        foreach (var ally in allies)
                        {
                            if (ally.Health <= (ally.MaximumHealth*0.3))
                            {
                                if (targettingType == 1)
                                {
                                    if (Utils.SleepCheck("saveduration"))
                                    {
                                        saveSpell.UseAbility(ally);
                                        Utils.Sleep(duration[saveSpell.Level - 1] + Game.Ping, "saveduration");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Heal(Hero self, Ability healSpell, float[] amount, int range, int targettingType)
        {
            if (healSpell != null && healSpell.CanBeCasted())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible() || !me.Modifiers.Any(x => x.Name == "modifier_treant_natures_guise")) &&
                    self.Distance2D(fountain) > 2000)
                {
//                    Console.WriteLine("Getting heroes entities");
                    var heroes =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(
                                entity =>
                                    entity.Team == self.Team && self.Distance2D(entity) <= range)
                            .ToList();

                    if (heroes.Any())
                    {
                        foreach (var ally in heroes)
                        {
                            if (ally.Health <= (ally.MaximumHealth*0.7) && healSpell.CanBeCasted() &&
                                self.Distance2D(fountain) > 2000 && IsInDanger(ally) &&
                                ally.Health + amount[healSpell.Level - 1] <= ally.MaximumHealth)
                            {
                                if (targettingType == 1)
                                    CastHeal(healSpell, ally);
                                else if (targettingType == 2)
                                    CastHeal(healSpell);
                                else if (targettingType == 3)
                                {
                                    /*if (healSpell.CanBeCasted() && !healSpell.IsChanneling)
                                    {
                                        Console.Write("Casting Heal");
                                        CastHeal(healSpell);
                                    }
                                    else
                                    {
                                        if (ally.Health > (ally.MaximumHealth*0.7) && healSpell.IsChanneling)
                                        {
                                            Console.Write("Casting Heal");
                                            CastHeal(healSpell);
                                        }
                                    }*/

                                    if (healSpell.CanBeCasted() && Utils.SleepCheck("ToggleHeal"))
                                    {
                                        if (!healSpell.IsToggled)
                                        {
                                            CastHeal(healSpell);
                                            Utils.Sleep(100 + Game.Ping, "ToggleHeal");
                                        }
                                    }
                                }
                            }
                            else if (targettingType == 3 && ally.Health > (ally.MaximumHealth*0.7) && healSpell.IsToggled &&
                                     Utils.SleepCheck("ToggleHeal"))
                            {
                                CastHeal(healSpell);
                                Utils.Sleep(100 + Game.Ping, "ToggleHeal");
                            }
                        }
                    }
                }
            }
        }

        private static void CastHeal(Ability healSpell, Hero destination = null)
        {
            if (destination != null)
            {
                if (healSpell.CanBeCasted())
                {
                    healSpell.UseAbility(destination);
                }
            }
            else
            {
                if (Utils.SleepCheck("Casting Heal"))
                {
                    healSpell.UseAbility();
                    Utils.Sleep(healSpell.ChannelTime + Game.Ping, "Casting Heal");
                }
            }
        }

        private static bool IsInDanger(Hero ally)
        {
            if (ally != null && ally.IsAlive && ally.Health > 0)
            {
                var enemies =
                    ObjectMgr.GetEntities<Hero>()
                        .Where(entity => entity.Team != ally.Team && entity.IsAlive && entity.IsVisible)
                        .ToList();
                foreach (var enemy in enemies)
                {
                    if (ally.Distance2D(enemy) < enemy.AttackRange + 50)
                    {
                        return true;
                    }
                    if (enemy.Spellbook.Spells.Any(abilities => ally.Distance2D(enemy) < abilities.CastRange + 50))
                    {
                        return true;
                    }
                }

                var buffs = new[]
                {
                    "modifier_item_urn_damage", "modifier_doom_bringer_doom", "modifier_axe_battle_hunger",
                    "modifier_queenofpain_shadow_strike", "modifier_phoenix_fire_spirit_burn",
                    "modifier_venomancer_poison_nova", "modifier_venomancer_venomous_gale",
                    "modifier_silencer_curse_of_the_silent", "modifier_silencer_last_word"
                };

                foreach (var buff in buffs)
                {
                    return ally.Modifiers.Any(x => x.Name == buff);
                }

                if ((ally.IsStunned() ||
                     (ally.IsSilenced() &&
                      ((ally.FindItem("item_manta_style") == null || ally.FindItem("item_manta_style").Cooldown > 0) ||
                       (ally.FindItem("item_black_king_bar") == null ||
                        ally.FindItem("item_black_king_bar").Cooldown > 0))) ||
                     ally.IsHexed() ||
                     ally.IsRooted()) && !ally.IsInvul()
                    )
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Support(ClassID hero)
        {
            if (hero == ClassID.CDOTA_Unit_Hero_Oracle || hero == ClassID.CDOTA_Unit_Hero_Winter_Wyvern ||
                hero == ClassID.CDOTA_Unit_Hero_KeeperOfTheLight || hero == ClassID.CDOTA_Unit_Hero_Dazzle ||
                hero == ClassID.CDOTA_Unit_Hero_Chen || hero == ClassID.CDOTA_Unit_Hero_Enchantress ||
                hero == ClassID.CDOTA_Unit_Hero_Legion_Commander || hero == ClassID.CDOTA_Unit_Hero_Abaddon ||
                hero == ClassID.CDOTA_Unit_Hero_Omniknight || hero == ClassID.CDOTA_Unit_Hero_Treant ||
                hero == ClassID.CDOTA_Unit_Hero_Wisp || hero == ClassID.CDOTA_Unit_Hero_Centaur ||
                hero == ClassID.CDOTA_Unit_Hero_Undying || hero == ClassID.CDOTA_Unit_Hero_WitchDoctor ||
                hero == ClassID.CDOTA_Unit_Hero_Necrolyte || hero == ClassID.CDOTA_Unit_Hero_Warlock ||
                hero == ClassID.CDOTA_Unit_Hero_Rubick || hero == ClassID.CDOTA_Unit_Hero_Huskar)
            {
                return true;
            }
            return false;
        }
    }
}