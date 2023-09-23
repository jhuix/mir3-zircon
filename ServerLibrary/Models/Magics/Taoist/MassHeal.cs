﻿using Library;
using Server.DBModels;
using Server.Envir;
using System;
using System.Drawing;
using System.Linq;

namespace Server.Models.Magics
{
    [MagicType(MagicType.MassHeal)]
    public class MassHeal : MagicObject
    {
        protected override Element Element => Element.None;
        public override bool UpdateCombatTime => false;

        public MassHeal(PlayerObject player, UserMagic magic) : base(player, magic)
        {

        }

        public override MagicCast MagicCast(MapObject target, Point location, MirDirection direction)
        {
            var response = new MagicCast
            {
                Ob = null
            };

            if (!Functions.InRange(CurrentLocation, location, Globals.MagicRange))
            {
                response.Cast = false;
                return response;
            }

            response.Locations.Add(location);

            var cells = CurrentMap.GetCells(location, 0, 2);

            var delay = SEnvir.Now.AddMilliseconds(500 + Functions.Distance(CurrentLocation, location) * 48);

            foreach (var cell in cells)
            {
                ActionList.Add(new DelayedAction(delay, ActionType.DelayMagic, Type, cell));
            }

            return response;
        }

        public override void MagicComplete(params object[] data)
        {
            var cell = (Cell)data[1];

            if (cell?.Objects == null) return;

            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                MapObject ob = cell.Objects[i];

                if (ob?.Node == null || !Player.CanHelpTarget(ob) || ob.CurrentHP >= ob.Stats[Stat.Health] || ob.Buffs.Any(x => x.Type == BuffType.Heal))
                {
                    continue;
                }

                int bonus = 0;
                int cap = 30;

                var empoweredHealing = GetAugmentedSkill(MagicType.EmpoweredHealing);

                if (empoweredHealing != null && Player.Level >= empoweredHealing.Info.NeedLevel1)
                {
                    bonus = empoweredHealing.GetPower();
                    cap += (1 + empoweredHealing.Level) * 30;

                    Player.LevelMagic(empoweredHealing);
                }

                Stats buffStats = new Stats
                {
                    [Stat.Healing] = Magic.GetPower() + Player.GetSC() + Player.Stats[Stat.HolyAttack] * 2 + bonus,
                    [Stat.HealingCap] = cap, // empowered healing
                };

                ob.BuffAdd(BuffType.Heal, TimeSpan.FromSeconds(buffStats[Stat.Healing] / buffStats[Stat.HealingCap]), buffStats, false, false, TimeSpan.FromSeconds(1));
                Player.LevelMagic(Magic);
            }
        }
    }
}
