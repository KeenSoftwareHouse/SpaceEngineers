using System;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Provides an easy method to create a MyDetectedEntityInfo struct from the detected entity and sensor owner ID
    /// </summary>
    public static class MyDetectedEntityInfoHelper
    {
        public static MyDetectedEntityInfo Create(MyEntity entity, long sensorOwner, Vector3D? hitPosition = null)
        {
            if (entity == null)
            {
                return new MyDetectedEntityInfo();
            }

            MatrixD orientation = MatrixD.Zero; 
            Vector3 velocity = Vector3D.Zero;
            int timeStamp = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            MyDetectedEntityType type;
            BoundingBoxD boundingBox = entity.PositionComp.WorldAABB;
            MyRelationsBetweenPlayerAndBlock relationship;
            string name;

            if (entity.Physics != null)
            {
                orientation = entity.Physics.GetWorldMatrix().GetOrientation();
                velocity = entity.Physics.LinearVelocity;
            }

            //using GetTopMostParent in case we are looking at a MyCubeBlock; we want the grid the block is on
            var grid = entity.GetTopMostParent() as MyCubeGrid;
            if (grid != null)
            {
                if (grid.GridSizeEnum == MyCubeSize.Small)
                    type = MyDetectedEntityType.SmallGrid;
                else
                    type = MyDetectedEntityType.LargeGrid;

                if (grid.BigOwners.Count == 0)
                    relationship = MyRelationsBetweenPlayerAndBlock.NoOwnership;
                else
                    relationship = MyIDModule.GetRelation(sensorOwner, grid.BigOwners[0], MyOwnershipShareModeEnum.Faction);

                if (relationship == MyRelationsBetweenPlayerAndBlock.Owner || relationship == MyRelationsBetweenPlayerAndBlock.FactionShare)
                    name = grid.DisplayName;
                else
                {
                    if (grid.GridSizeEnum == MyCubeSize.Small)
                        name = MyTexts.GetString(MySpaceTexts.DetectedEntity_SmallGrid);
                    else
                        name = MyTexts.GetString(MySpaceTexts.DetectedEntity_LargeGrid);
                }
                
                return new MyDetectedEntityInfo(grid.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            var character = entity as MyCharacter;
            if (character != null)
            {
                if (character.IsPlayer)
                    type = MyDetectedEntityType.CharacterHuman;
                else
                    type = MyDetectedEntityType.CharacterOther;

                relationship = MyIDModule.GetRelation(sensorOwner, character.GetPlayerIdentityId(), MyOwnershipShareModeEnum.Faction);

                if (relationship == MyRelationsBetweenPlayerAndBlock.Owner || relationship == MyRelationsBetweenPlayerAndBlock.FactionShare)
                    name = character.DisplayNameText;
                else
                {
                    if (character.IsPlayer)
                        name = MyTexts.GetString(MySpaceTexts.DetectedEntity_CharacterHuman);
                    else
                        name = MyTexts.GetString(MySpaceTexts.DetectedEntity_CharacterOther);
                }

                BoundingBoxD bound = character.Model.BoundingBox.Transform(character.WorldMatrix);

                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, bound, timeStamp);
            }

            relationship = MyRelationsBetweenPlayerAndBlock.Neutral;

            var floating = entity as MyFloatingObject;
            if (floating != null)
            {
                type = MyDetectedEntityType.FloatingObject;
                name = floating.Item.Content.SubtypeName;
                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            var backpack = entity as MyInventoryBagEntity;
            if (backpack != null)
            {
                type = MyDetectedEntityType.FloatingObject;
                name = backpack.DisplayName;
                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            var planet = entity as MyPlanet;
            if (planet != null)
            {
                type = MyDetectedEntityType.Planet;
                name = MyTexts.GetString(MySpaceTexts.DetectedEntity_Planet);
                //shrink the planet's bounding box to only encompass terrain
                boundingBox = BoundingBoxD.CreateFromSphere(new BoundingSphereD(planet.PositionComp.GetPosition(), planet.MaximumRadius));
                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            var voxelPhysics = entity as MyVoxelPhysics;
            if (voxelPhysics != null)
            {
                type = MyDetectedEntityType.Planet;
                name = MyTexts.GetString(MySpaceTexts.DetectedEntity_Planet);
                //shrink the planet's bounding box to only encompass terrain
                boundingBox = BoundingBoxD.CreateFromSphere(new BoundingSphereD(voxelPhysics.Parent.PositionComp.GetPosition(), voxelPhysics.Parent.MaximumRadius));
                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            var voxel = entity as MyVoxelMap;
            if (voxel != null)
            {
                type = MyDetectedEntityType.Asteroid;
                name = MyTexts.GetString(MySpaceTexts.DetectedEntity_Asteroid);
                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            var meteor = entity as MyMeteor;
            if (meteor != null)
            {
                type = MyDetectedEntityType.Meteor;
                name = MyTexts.GetString(MySpaceTexts.DetectedEntity_Meteor);
                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            var missile = entity as MyMissile;
            if (missile != null)
            {
                type = MyDetectedEntityType.Missile;
                name = entity.DisplayName;
                return new MyDetectedEntityInfo(entity.EntityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            //error case
            return new MyDetectedEntityInfo(0, String.Empty, MyDetectedEntityType.Unknown, null, new MatrixD(), new Vector3(), MyRelationsBetweenPlayerAndBlock.NoOwnership, new BoundingBoxD(), MySandboxGame.TotalGamePlayTimeInMilliseconds);
        }
    }
}
