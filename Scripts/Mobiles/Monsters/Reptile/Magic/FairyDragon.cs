﻿#region Header

// **********
// ServUO - FairyDragon.cs
// **********

#endregion

#region References

using System;
using Server.Items;
using Server.Services;

#endregion

namespace Server.Mobiles
{
    [CorpseName("a Fairy dragon corpse")]
    public class FairyDragon : BaseCreature
    {
        public static Type[] VArtifacts =
        {
            typeof (GargishSignOfChaos)
        };

        [Constructable]
        public FairyDragon()
            : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Fairy Dragon";
            Body = 718;
            BaseSoundID = 362;

            SetStr(512, 558);
            SetDex(95, 105);
            SetInt(455, 501);

            SetHits(398, 403);

            SetDamage(15, 18);

            //SetDamageType( ResistanceType.Physical, 100 );
            SetDamageType(ResistanceType.Fire, 20, 25);
            SetDamageType(ResistanceType.Cold, 20, 25);
            SetDamageType(ResistanceType.Poison, 20, 25);
            SetDamageType(ResistanceType.Energy, 20, 25);

            SetResistance(ResistanceType.Physical, 16, 30);
            SetResistance(ResistanceType.Fire, 41, 44);
            SetResistance(ResistanceType.Cold, 40, 49);
            SetResistance(ResistanceType.Poison, 40, 49);
            SetResistance(ResistanceType.Energy, 45, 47);

            SetSkill(SkillName.EvalInt, 30.1, 40.0);
            SetSkill(SkillName.Magery, 30.1, 40.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 60.6, 68.2);
            SetSkill(SkillName.Wrestling, 90.1, 92.5);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 39;
        }

        public FairyDragon(Serial serial)
            : base(serial)
        {
        }

        //public override bool ReacquireOnMovement { get { return !Controlled; } }
        //public override bool HasBreath{ get{ return true; } } // fire breath enabled
        public override bool AutoDispel
        {
            get { return !Controlled; }
        }

        public override int TreasureMapLevel
        {
            get { return 4; }
        }

        public override int Meat
        {
            get { return 9; }
        }

        public override Poison HitPoison
        {
            get { return Poison.Greater; }
        }

        public override double HitPoisonChance
        {
            get { return 0.75; }
        }

        public override FoodType FavoriteFood
        {
            get { return FoodType.Meat; }
        }

        public override bool CanAngerOnTame
        {
            get { return true; }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.MedScrolls, 2);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);
            SARegionDrops.GetSADrop(c);

            if (Utility.RandomDouble() <= 0.25)
            {
                switch (Utility.Random(3))
                {
                    case 0:
                        c.DropItem(new FeyWings());
                        break;
                    case 1:
                        c.DropItem(new FairyDragonWing());
                        break;
                    case 2:
                        c.DropItem(new UntransTome());
                        break;
                }
            }

            if (Utility.RandomDouble() < 0.30)
            {
                switch (Utility.Random(4))
                {
                    case 0:
                        c.DropItem(new DraconicOrbKey());
                        break;
                    case 1:
                        c.DropItem(new DraconicOrbKeyBlue());
                        break;
                    case 2:
                        c.DropItem(new DraconicOrbKeyRed());
                        break;
                    case 3:
                        c.DropItem(new DraconicOrbKeyOrange());
                        break;
                }
            }
            if (c != null && !c.Deleted && c is Corpse)
            {
                var corpse = (Corpse) c;
                if (Utility.RandomDouble() < 0.01 && corpse.Killer != null && !corpse.Killer.Deleted)
                {
                    GiveVArtifactTo(corpse.Killer);
                }
            }
        }

        public static void GiveVArtifactTo(Mobile m)
        {
            var item = (Item) Activator.CreateInstance(VArtifacts[Utility.Random(VArtifacts.Length)]);

            if (m.AddToBackpack(item))
                m.SendLocalizedMessage(1062317);
                    // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
            else
                m.SendMessage("As your backpack is full, your reward has been placed at your feet.");
            {
            }
        }

        public override int GetAttackSound()
        {
            return 1513;
        }

        public override int GetAngerSound()
        {
            return 1558;
        }

        public override int GetDeathSound()
        {
            return 1514;
        }

        public override int GetHurtSound()
        {
            return 1515;
        }

        public override int GetIdleSound()
        {
            return 1516;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadInt();
        }
    }
}