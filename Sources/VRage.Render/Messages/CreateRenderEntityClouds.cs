using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using VRageMath;
using VRageRender.Import;

namespace VRageRender.Messages
{
	[ProtoContract]
	public struct MyCloudLayerSettings
	{
		[ProtoMember]
		public string Model;

	    [ProtoMember]
	    [XmlArrayItem("Texture")]
	    public List<string> Textures;
		[ProtoMember]
		public float RelativeAltitude;

		[ProtoMember]
		public Vector3D RotationAxis;
		[ProtoMember]
		public float AngularVelocity;
		[ProtoMember]
		public float InitialRotation;

		[ProtoMember]
		public bool ScalingEnabled;
		[ProtoMember]
		public float FadeOutRelativeAltitudeStart;
		[ProtoMember]
		public float FadeOutRelativeAltitudeEnd;
		[ProtoMember]
		public float ApplyFogRelativeDistance;
	}

	public class MyRenderMessageCreateRenderEntityClouds : MyRenderMessageBase
	{
		public uint ID;
		public string DebugName;
		public string Model;
	    public List<string> Textures;
		public Vector3D CenterPoint;

		public double Altitude;
		public double MinScaledAltitude;
		public bool ScalingEnabled;

		public Vector3D RotationAxis;
		public float AngularVelocity;
		public float InitialRotation;

		public double FadeOutRelativeAltitudeStart;
		public double FadeOutRelativeAltitudeEnd;
		public float ApplyFogRelativeDistance;

		public double MaxPlanetHillRadius;

		public MyMeshDrawTechnique Technique;

	    public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
	    public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderEntityClouds; } }

		public override string ToString()
		{
			return DebugName ?? String.Empty + ", " + Model ?? String.Empty;
		}
	}
}