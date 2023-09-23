﻿using Library;
using Library.SystemModels;
using Server.DBModels;
using Server.Envir;
using System.Drawing;
using System.Linq;

namespace Server.Models.Magics.Taoist
{
    [MagicType(MagicType.SummonDemonicCreature)]
    public class SummonDemonicCreature : MagicObject
    {
        protected override Element Element => Element.None;
        public override bool UpdateCombatTime => false;

        public SummonDemonicCreature(PlayerObject player, UserMagic magic) : base(player, magic)
        {

        }

        public override MagicCast MagicCast(MapObject target, Point location, MirDirection direction)
        {
            var response = new MagicCast
            {
                Ob = null
            };
 
            if (!Player.UseAmulet(25, 0))
            {
                response.Cast = false;
                return response;
            }

            var delay = SEnvir.Now.AddMilliseconds(500);

            var info = SEnvir.MonsterInfoList.Binding.First(x => x.Flag == MonsterFlag.InfernalSoldier);

            ActionList.Add(new DelayedAction(delay, ActionType.DelayMagic, Type, CurrentMap, Functions.Move(CurrentLocation, direction, -1), info));

            return response;
        }

        public override void MagicComplete(params object[] data)
        {
            var map = (Map)data[1];
            var location = (Point)data[2];
            var info = (MonsterInfo)data[3];

            if (info == null) return;

            MonsterObject ob = Player.Pets.FirstOrDefault(x => x.MonsterInfo == info);

            if (ob != null)
            {
                ob.PetRecall();
                return;
            }

            if (Player.Pets.Count >= 2) return;

            ob = MonsterObject.GetMonster(info);

            ob.PetOwner = Player;
            Player.Pets.Add(ob);

            ob.Master?.MinionList.Remove(ob);
            ob.Master = null;
            ob.Magics.Add(Magic);
            ob.SummonLevel = Magic.Level * 2;
            ob.TameTime = SEnvir.Now.AddDays(365);

            var strengthOfFaith = GetAugmentedSkill(MagicType.StrengthOfFaith);

            if (Player.Buffs.Any(x => x.Type == BuffType.StrengthOfFaith) && strengthOfFaith != null)
            {
                ob.Magics.Add(strengthOfFaith);
            }

            var demonicRecovery = GetAugmentedSkill(MagicType.DemonicRecovery);

            if (demonicRecovery != null)
            {
                ob.Magics.Add(demonicRecovery);
            }

            Cell cell = map.GetCell(location);

            if (cell == null || cell.Movements != null || !ob.Spawn(map, location))
                ob.Spawn(CurrentMap, CurrentLocation);

            ob.SetHP(ob.Stats[Stat.Health]);

            Player.LevelMagic(Magic);
        }
    }
}
