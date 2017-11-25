using System;
using System.Reflection;
using CombatExtended;
using Harmony;
using UnityEngine;
using Verse;

namespace FrontierDevelopments.Shields.Handlers
{
    public class ProjectCEHandler
    {
        private static readonly bool Enabled = true;
        
        private static readonly PropertyInfo DestinationProperty = typeof(ProjectileCE).GetProperty("Destination", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo OriginField = typeof(ProjectileCE).GetField("origin", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo FTicksProperty = typeof(ProjectileCE).GetProperty("fTicks", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo StartingTicksToImpactProperty = typeof(ProjectileCE).GetProperty("StartingTicksToImpact", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo LauncherField = typeof(ProjectileCE).GetField("launcher", BindingFlags.NonPublic | BindingFlags.Instance);

        static ProjectCEHandler()
        {
            if (DestinationProperty == null)
            {
                Enabled = false;
                Log.Error("FrontierDevelopments Shields :: ProjectileCE handler reflection error on property ProjectileCE.Destination");
            }
            if (OriginField == null)
            {
                Enabled = false;
                Log.Error("FrontierDevelopments Shields :: ProjectileCE handler reflection error on field ProjectileCE.origin");
            }
            if (FTicksProperty == null)
            {
                Enabled = false;
                Log.Error("FrontierDevelopments Shields :: ProjectileCE handler reflection error on property ProjectileCE.fTicks");
            }
            if (StartingTicksToImpactProperty == null)
            {
                Enabled = false;
                Log.Error("FrontierDevelopments Shields :: ProjectileCE handler reflection error on property ProjectileCE.startingTicksToImpact");
            }
            if (LauncherField == null)
            {
                Enabled = false;
                Log.Error("Shield ProjectileCE handler reflection error on property ProjectileCE.launcher");
            }

            Log.Message("FrontierDevelopments Shields :: ProjectileCE handler " + (Enabled ? "enabled" : "disabled due to errors"));
        }
        
        [HarmonyPatch(typeof(ProjectileCE), "Tick")]
        static class Patch_ProjectileCE_Tick
        {
            static bool Prefix(ProjectileCE __instance)
            {
                if (!Enabled) return true;
                
                var projectile = __instance;
                
                var flightTicks = (float)FTicksProperty.GetValue(projectile, null);
                var startingTicksToImpact = (float)StartingTicksToImpactProperty.GetValue(projectile, null);
            
                var origin = (Vector2)OriginField.GetValue(projectile);
                var destination = (Vector2) DestinationProperty.GetValue(projectile, null);
                var position = Vector2.Lerp(origin, destination, flightTicks / startingTicksToImpact);
                
                try
                {
                    if (projectile.def.projectile.flyOverhead)
                    {
                        if (projectile.def.projectile.flyOverhead)
                        {
                            // the shield has blocked the projectile - invert to get if harmony should allow the original block
                            return !Mod.ShieldManager.ImpactShield(projectile.Map, position, origin, destination, (shield, vector3) =>
                            {
                                if (shield.Damage(projectile.def.projectile.damageAmountBase, position))
                                {
                                    projectile.Destroy();
                                    return true;
                                }
                                return false;
                            });
                        }
                    }
                    else
                    {
                        var nextPosition = Vector2.Lerp(origin, destination, (flightTicks + 1) / startingTicksToImpact);
                        var ray = new Ray2D(position, nextPosition - position);
                        
                        // the shield has blocked the projectile - invert to get if harmony should allow the original block
                        return !Mod.ShieldManager.ImpactShield(projectile.Map, origin, ray, 1, (shield, point) =>
                        {
                            if (shield.Damage(projectile.def.projectile.damageAmountBase, point))
                            {
                                projectile.GetComp<CompExplosiveCE>()?.
                                    Explode(
                                        (Thing)LauncherField.GetValue(projectile), 
                                        new Vector3(point.x, 0, point.y), 
                                        projectile.Map);
                                projectile.Destroy();
                                return true;
                            }
                            return false;
                        });
                        
                    }
                }
                catch (InvalidOperationException) {}
                return true;
            }
        }
    }
}