using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Library.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    partial class MyProjectorBase : IMyProjector
    {
        IMyCubeGrid IMyProjector.ProjectedGrid
        {
            get { return ProjectedGrid; }
        }

        BuildCheckResult IMyProjector.CanBuild( IMySlimBlock projectedBlock, bool checkHavokIntersections )
        {
            return CanBuild( (MySlimBlock)projectedBlock, checkHavokIntersections );
        }

        void IMyProjector.Build( IMySlimBlock cubeBlock, long owner, long builder, bool requestInstant )
        {
            Build( (MySlimBlock)cubeBlock, owner, builder, requestInstant );
        }

        #region Ingame

        Vector3I ModAPI.Ingame.IMyProjector.ProjectionOffset
        {
            get {return m_projectionOffset;}
            set { m_projectionOffset = value; }
        }

        Vector3I ModAPI.Ingame.IMyProjector.ProjectionRotation
        {
            get { return m_projectionRotation; }
            set { m_projectionRotation = value; }
        }

        void ModAPI.Ingame.IMyProjector.UpdateOffsetAndRotation()
        {
            OnOffsetsChanged();
        }

        [Obsolete("Use ProjectionOffset vector instead.")]
        int ModAPI.Ingame.IMyProjector.ProjectionOffsetX
        {
            get { return m_projectionOffset.X; }
        }
        [Obsolete("Use ProjectionOffset vector instead.")]
        int ModAPI.Ingame.IMyProjector.ProjectionOffsetY
        {
            get { return m_projectionOffset.Y; }
        }
        [Obsolete("Use ProjectionOffset vector instead.")]
        int ModAPI.Ingame.IMyProjector.ProjectionOffsetZ
        {
            get { return m_projectionOffset.Z; }
        }

        [Obsolete("Use ProjectionRotation vector instead.")]
        int ModAPI.Ingame.IMyProjector.ProjectionRotX
        {
            get { return m_projectionRotation.X * 90; }
        }
        [Obsolete("Use ProjectionRotation vector instead.")]
        int ModAPI.Ingame.IMyProjector.ProjectionRotY
        {
            get { return m_projectionRotation.Y * 90; }
        }
        [Obsolete("Use ProjectionRotation vector instead.")]
        int ModAPI.Ingame.IMyProjector.ProjectionRotZ
        {
            get { return m_projectionRotation.Z * 90; }
        }
        
        bool ModAPI.Ingame.IMyProjector.IsProjecting
        {
            get { return IsProjecting(); }
        }

        int ModAPI.Ingame.IMyProjector.RemainingBlocks
        {
            get { return m_remainingBlocks; }
        }

        int ModAPI.Ingame.IMyProjector.TotalBlocks
        {
            get { return m_totalBlocks; }
        }

        int ModAPI.Ingame.IMyProjector.RemainingArmorBlocks
        {
            get { return m_remainingArmorBlocks; }
        }

        int ModAPI.Ingame.IMyProjector.BuildableBlocksCount
        {
            get { return m_buildableBlocksCount; }
        }

        Dictionary<MyDefinitionBase, int> ModAPI.Ingame.IMyProjector.RemainingBlocksPerType
        {
            get
            {
                Dictionary<MyDefinitionBase, int> result = new Dictionary<MyDefinitionBase, int>();
                foreach ( var entry in m_remainingBlocksPerType )
                    result.Add( entry.Key, entry.Value );
                return result;
            }
        }

        bool ModAPI.Ingame.IMyProjector.LoadRandomBlueprint( string searchPattern )
        {
            bool success = false;
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            string[] files = System.IO.Directory.GetFiles( Path.Combine( MyFileSystem.ContentPath, "Data", "Blueprints" ), searchPattern );

            if ( files.Length > 0 )
            {
                var index = MyRandom.Instance.Next() % files.Length;
                success = LoadBlueprint( files[index] );
            }
#endif // !XB1
            return success;
        }

        bool ModAPI.Ingame.IMyProjector.LoadBlueprint( string path )
        {
            return LoadBlueprint( path );
        }

        private bool LoadBlueprint( string path )
        {
            bool success = false;
            MyObjectBuilder_Definitions blueprint = MyGuiBlueprintScreenBase.LoadPrefab( path );

            if ( blueprint != null )
                success = MyGuiBlueprintScreen.CopyBlueprintPrefabToClipboard( blueprint, m_clipboard );

            OnBlueprintScreen_Closed( null );
            return success;
        }

        #endregion
    }
}
