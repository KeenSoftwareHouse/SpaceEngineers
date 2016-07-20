using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.AI
{
    public class MySpaceBotFactory : MyBotFactoryBase
    {
        public override int MaximumUncontrolledBotCount
        {
            get { return 10; }
        }

        public override int MaximumBotPerPlayer
        {
            get { return 10; }
        }

        public override bool CanCreateBotOfType(string behaviorType, bool load)
        {
            return true;
        }

        public override bool GetBotSpawnPosition(string behaviorType, out VRageMath.Vector3D spawnPosition)
        {
            // CH: TODO: Do this more generically, so that modders would be able to define their own bot types and the way they spawn
            if (behaviorType == "Spider")
            {
                MatrixD spawnMatrix;
                bool success = GetSpiderSpawnPosition(out spawnMatrix, null);
                spawnPosition = spawnMatrix.Translation;
                return success;
            }
            else if (MySession.Static.LocalCharacter != null)
            {
                var pos = MySession.Static.LocalCharacter.PositionComp.GetPosition();
                Vector3 up;
                Vector3D right, forward;

                up = MyGravityProviderSystem.CalculateNaturalGravityInPoint(pos);
                if (up.LengthSquared() < 0.0001f) up = Vector3.Up;
                else up = Vector3D.Normalize(up);
                forward = Vector3.CalculatePerpendicularVector(up);
                right = Vector3.Cross(forward, up);
                spawnPosition = MyUtils.GetRandomDiscPosition(ref pos, 5.0f, ref forward, ref right);
                return true;
            }

            spawnPosition = Vector3D.Zero;
            return false;
        }

        public static bool GetSpiderSpawnPosition(out MatrixD spawnPosition, Vector3D? oldPosition)
        {
            spawnPosition = MatrixD.Identity;

            Vector3D? position = null;
            MyPlanet planet = null;
            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                if (player.Id.SerialId != 0) continue;
                if (player.Character == null) continue;

                position = player.GetPosition();
                planet = MyGamePruningStructure.GetClosestPlanet(position.Value);

                var animalSpawnInfo = GetDayOrNightAnimalSpawnInfo(planet, position.Value);
                if (animalSpawnInfo == null || animalSpawnInfo.Animals == null ||
                    !animalSpawnInfo.Animals.Any(x => x.AnimalType.Contains("Spider")))
                {
                    position = null;
                    planet = null;
                    continue;
                } 

                if (oldPosition != null) // prevent teleporting from planet to planet
                {
                    var planetOld = MyGamePruningStructure.GetClosestPlanet(oldPosition.Value);
                    if (planet != planetOld)
                    {
                        position = null;
                        planet = null;
                        continue;
                    }
                }
                break;
            }

            if (!position.HasValue || planet == null) 
                return false;

            Vector3D gravity = planet.Components.Get<MyGravityProviderComponent>().GetWorldGravity(position.Value);
            if (Vector3D.IsZero(gravity))
                gravity = Vector3D.Down;
            else
                gravity.Normalize();

            Vector3D tangent, bitangent;
            gravity.CalculatePerpendicularVector(out tangent);
            bitangent = Vector3D.Cross(gravity, tangent);

            Vector3D start = position.Value;
            start = MyUtils.GetRandomDiscPosition(ref start, 20.0f, ref tangent, ref bitangent);

            start -= gravity * 500;
            Vector3D translation = planet.GetClosestSurfacePointGlobal(ref start);
            Vector3D dirToPlayer = position.Value - translation;
            if (!Vector3D.IsZero(dirToPlayer))
            {
                dirToPlayer.Normalize();
            }
            else
            {
                dirToPlayer = Vector3D.CalculatePerpendicularVector(gravity);
            }
            spawnPosition = MatrixD.CreateWorld(translation, dirToPlayer, -gravity);

            return true;
        }

        public override bool GetBotGroupSpawnPositions(string behaviorType, int count, List<Vector3D> spawnPositions)
        {
            throw new NotImplementedException();
        }

        // Obtain day or night spawning info based on given planet and position. Position is in global space.
        public static MyPlanetAnimalSpawnInfo GetDayOrNightAnimalSpawnInfo(MyPlanet planet, Vector3D position)
        {
            if (planet == null)
            {
                return null;
            }

            if (planet.Generator.NightAnimalSpawnInfo != null
                && planet.Generator.NightAnimalSpawnInfo.Animals != null
                && planet.Generator.NightAnimalSpawnInfo.Animals.Length > 0
                && IsThereNight(planet, ref position))
            {
                return planet.Generator.NightAnimalSpawnInfo;
            }
            else if (planet.Generator.AnimalSpawnInfo != null
                && planet.Generator.AnimalSpawnInfo.Animals != null
                && planet.Generator.AnimalSpawnInfo.Animals.Length > 0)
            {
                return planet.Generator.AnimalSpawnInfo;
            }
            else
            {
                return null;
            }
        }

        // Is there night on the given place on planet? Position is in global space.
        static bool IsThereNight(MyPlanet planet, ref Vector3D position)
        {
            // gravitation vector and vector to sun are facing same direction

            // We don't even have to normalize :D
            Vector3 grav = position - planet.PositionComp.GetPosition();
            return Vector3.Dot(MySector.DirectionToSunNormalized, grav) > 0;
        }
    }
}
