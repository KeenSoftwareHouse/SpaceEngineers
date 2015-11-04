using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.World
{
    public partial class MyIdentity
    {
        // This is to prevent construction of new identities in unwanted places
        public class Friend
        {
            public virtual MyIdentity CreateNewIdentity(string name, string model = null)
            {
                return new MyIdentity(name, MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, model);
            }

            public virtual MyIdentity CreateNewIdentity(string name, long identityId, string model)
            {
                return new MyIdentity(name, identityId, model);
            }

            public virtual MyIdentity CreateNewIdentity(MyObjectBuilder_Identity objectBuilder)
            {
                return new MyIdentity(objectBuilder);
            }
        }

        public long IdentityId { get; private set; }
        public string DisplayName { get; private set; }

        public MyCharacter Character { get; private set; }
        public string Model { get; private set; }
        public Vector3? ColorMask { get; private set; }

        public bool IsDead { get; private set; }

        public event Action<MyCharacter, MyCharacter> CharacterChanged;


        private MyIdentity(string name, MyEntityIdentifier.ID_OBJECT_TYPE identityType, string model = null)
        {
            Debug.Assert(identityType == MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, "Trying to create invalid identity type!");
            IdentityId = MyEntityIdentifier.AllocateId(identityType, MyEntityIdentifier.ID_ALLOCATION_METHOD.SERIAL_START_WITH_1);
            Init(name, IdentityId, model);
        }

        private MyIdentity(string name, long identityId, string model)
        {
            identityId = MyEntityIdentifier.FixObsoleteIdentityType(identityId);
            Init(name, identityId, model);
            MyEntityIdentifier.MarkIdUsed(identityId);
        }

        private MyIdentity(MyObjectBuilder_Identity objectBuilder)
        {
            Init(objectBuilder.DisplayName, MyEntityIdentifier.FixObsoleteIdentityType(objectBuilder.IdentityId), objectBuilder.Model);
            MyEntityIdentifier.MarkIdUsed(IdentityId);

            if (objectBuilder.ColorMask.HasValue)
                ColorMask = objectBuilder.ColorMask;
            IsDead = true;

            MyEntity character;
            MyEntities.TryGetEntityById(objectBuilder.CharacterEntityId, out character);

            if (character is MyCharacter)
                Character = character as MyCharacter;
        }

        public MyObjectBuilder_Identity GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_Identity();
            objectBuilder.IdentityId = IdentityId;
            objectBuilder.DisplayName = DisplayName;
            objectBuilder.CharacterEntityId = Character == null ? 0 : Character.EntityId;
            objectBuilder.Model = Model;
            objectBuilder.ColorMask = ColorMask;

            return objectBuilder;
        }

        private void Init(string name, long identityId, string model)
        {
            DisplayName = name;
            IdentityId = identityId;

            IsDead = true;
            Model = model;
            ColorMask = null;
        }
    
        public void SetColorMask(Vector3 color) 
        {
            ColorMask = color;
        }

        public void ChangeCharacter(MyCharacter character)
        {
            var oldCharacter = Character;

            if (Character != null)
            {
                Character.SyncObject.CharacterModelSwitched -= character_CharacterModelSwitched;
                Character.OnClosing -= character_OnClosing;
            }

            Character = character;

            if (character != null)
            {
                character.OnClosing += character_OnClosing;
                character.SyncObject.CharacterModelSwitched += character_CharacterModelSwitched;

                SaveModelAndColorFromCharacter();

                IsDead = character.IsDead;
            }

            if (CharacterChanged != null)
                CharacterChanged(oldCharacter, Character);
        }

        private void SaveModelAndColorFromCharacter()
        {
            Model = Character.ModelName;
            ColorMask = Character.ColorMask;
        }

        public void SetDead(bool dead)
        {
            IsDead = dead;
        }

        /// <summary>
        /// This should only be called during initialization
        /// It is used to assume the identity of someone else,
        /// but keep your name
        /// </summary>
        /// <param name="name"></param>
        public void SetDisplayName(string name)
        {
            DisplayName = name;
        }

        private void character_OnClosing(MyEntity obj)
        {
            Character.SyncObject.CharacterModelSwitched -= character_CharacterModelSwitched;
            Character.OnClosing -= character_OnClosing;
            Character = null;
        }

        private void character_CharacterModelSwitched(string model, Vector3 colorMaskHSV)
        {
            Model = model;
            ColorMask = colorMaskHSV;
        }

        public void ChangeToOxygenSafeSuit()
        {
            if (Model == null)
            {
                return;
            }
            MyCharacterDefinition characterDefinition;
            MyDefinitionManager.Static.Characters.TryGetValue(Model, out characterDefinition);

            if (characterDefinition != null && characterDefinition.NeedsOxygen)
            {
               Model = MyDefinitionManager.Static.Characters.First().Model;
            }
        }

        private static List<MySyncGrid.MySingleOwnershipRequest> m_requests = new List<MySyncGrid.MySingleOwnershipRequest>();
        private static HashSet<IMyEntity> m_entitiesCache = new HashSet<IMyEntity>();
        public void TransferAllBlocksTo(long newOwnerIdentityId)
        {
            MyAPIGateway.Entities.GetEntities(m_entitiesCache, (x) => x is IMyCubeGrid);
            foreach (var ent in m_entitiesCache)
            {
                var grid = ent as MyCubeGrid;
                foreach (var block in grid.GetFatBlocks<MyTerminalBlock>())
                    if (block.IDModule != null && block.OwnerId == IdentityId)
                        m_requests.Add(new MySyncGrid.MySingleOwnershipRequest()
                        {
                            BlockId = block.EntityId,
                            Owner = newOwnerIdentityId
                        });
            }
            m_entitiesCache.Clear();

            if (m_requests.Count > 0)
                MySyncGrid.ChangeOwnersRequest(MyOwnershipShareModeEnum.None, m_requests,IdentityId);
            m_requests.Clear();

        }
        
    }
}
