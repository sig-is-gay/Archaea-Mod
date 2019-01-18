﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArchaeaMod.Items
{
    public class ArchaeaItem
    {
        public static void ActiveChannelStyle(Player player)
        {
            player.direction = Main.MouseWorld.X > player.position.X ? 1 : -1;
            float PI = (float)Math.PI;
            float angle = NPCs.ArchaeaNPC.AngleTo(player.Center, Main.MouseWorld);
            player.itemRotation = angle + (player.direction == -1 ? PI : 0);
            player.itemLocation.X = NPCs.ArchaeaNPC.AngleBased(player.position, angle, player.width / 4).X - player.width / 2 - 4;
        }
        public static bool NotEquipped(Player player, Item item, ref int index)
        {
            for (int i = 0; i < player.armor.Length; i++)
            {
                if (player.armor[i] != item)
                    index++;
                else break;
                if (index == player.armor.Length - 1 && player.armor[i] != item)
                    return true;
            }
            index = 0;
            return false;
        }
        public static bool Elapsed(int interval)
        {
            return Math.Round(Main.time, 0) % interval == 0;
        }
    }

    public class Draw
    {
        public const float radian = 0.017f;
        public float radians(float distance)
        {
            return radian * (45f / distance);
        }
    }

    public class Target
    {
        public NPC npc;
        public Player player;
        public const int
            Default = 0,
            ShockWave = 1,
            Frozen = 2,
            Fire = 3;
        private Mod mod
        {
            get { return ModLoader.GetMod("ArchaeaMod_Debug"); }
        }
        public Target(NPC npc, Player player)
        {
            this.npc = npc;
            this.player = player;
        }
        public void AttackEffect(int type)
        {
            switch (type)
            {
                case Default:
                    break;
                case ShockWave:
                    float angle = NPCs.ArchaeaNPC.AngleTo(player.Center, npc.Center);
                    npc.velocity.Y -= 8f;
                    npc.velocity += NPCs.ArchaeaNPC.AngleToSpeed(angle, 12f);
                    NPCs.ArchaeaNPC.VelocityClamp(npc, -10f, 10f);
                    break;
                case Frozen:
                    npc.AddBuff(mod.BuffType<Frozen>(), 60);
                    break;
                case Fire:
                    npc.AddBuff(BuffID.OnFire, 10);
                    break;
            }
        }
        public static Target[] GetTargets(Player player, float range)
        {
            Target[] targets = new Target[255];
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (!Shield.GetOnCondition(Main.npc[i])) continue;
                if (player.Distance(Main.npc[i].Center) > range) continue;
                targets[i] = new Target(Main.npc[i], player);
            }
            return targets;
        }
        public static Target[] GetTargets(NPC npc, float range)
        {
            Target[] targets = new Target[255];
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (!Shield.GetOnCondition(Main.npc[i])) continue;
                if (npc.Distance(Main.npc[i].Center) > range) continue;
                targets[i] = new Target(Main.npc[i], null);
            }
            return targets;
        }
        public static Target[] GetTargets(Projectile projectile, float range)
        {
            Target[] targets = new Target[255];
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (!Shield.GetOnCondition(Main.npc[i])) continue;
                if (projectile.Distance(Main.npc[i].Center) > range) continue;
                targets[i] = new Target(Main.npc[i], null);
            }
            return targets;
        }
    }

    public class Pixel : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shard");
        }
        public override void SetDefaults()
        {
            projectile.width = 1;
            projectile.height = 1;
            projectile.damage = 0;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.friendly = true;
            projectile.timeLeft = 120;
            projectile.alpha = 255;
        }
        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * alpha;
        }
        private bool direction;
        private int ai;
        public const int
            None = -1,
            Default = 0,
            Sword = 1;
        private float rotate;
        private float alpha;
        private Dust dust;
        private Player owner
        {
            get { return Main.player[projectile.owner]; }
        }
        public override bool PreAI()
        {
            switch (ai)
            {
                case 0:
                    direction = owner.direction == 1 ? true : false;
                    rotate = direction ? 0f : (float)Math.PI;
                    dust = SetDust();
                    goto case 1;
                case 1:
                    ai = 1;
                    break;
            }
            return true;
        }
        public void AIType()
        {
            switch ((int)projectile.ai[1])
            {
                case None:
                    projectile.alpha = 0;
                    alpha = 1f;
                    projectile.timeLeft = 100;
                    break;
                case Default:
                    dust.position = projectile.position;
                    break;
                case Sword:
                    NPCs.ArchaeaNPC.RotateIncrement(true, ref rotate, (float)Math.PI / 2f, 0.15f, out rotate);
                    projectile.velocity += NPCs.ArchaeaNPC.AngleToSpeed(rotate, 0.25f);
                    dust.position = projectile.position;
                    break;
            }
        }
        public override void AI()
        {
            AIType();
        }
        public override void Kill(int timeLeft)
        {
            switch ((int)projectile.ai[1])
            {
                case Default:
                    break;
                case Sword:
                    NPCs.ArchaeaNPC.DustSpread(projectile.Center, 6, 4, 2f);
                    break;
            }
        }
        public const int
            Fire = 1,
            Dark = 2;
        private Dust defaultDust
        {
            get { return Dust.NewDustDirect(Vector2.Zero, 1, 1, 0); }
        }
        public Dust SetDust()
        {
            switch ((int)projectile.ai[0])
            {
                case 0:
                    break;
                case Fire:
                    return Dust.NewDustDirect(projectile.Center, 2, 2, DustID.Fire, 0f, 0f, 0, default(Color), Main.rand.NextFloat(1f, 3f));
            }
            return defaultDust;
        }
    }

    public class Frozen : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Frozen");
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.velocity.X /= 2f;
            Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.t_Frozen);
            dust.noGravity = true;
            dust.noLight = false;
        }
    }
}