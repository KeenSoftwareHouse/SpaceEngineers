<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <Interface>VRage.Game.VisualScripting.IMyStateMachineScript</Interface>
    <DependencyFilePaths>
      <string>VisualScripts\Library\Once.vs</string>
    </DependencyFilePaths>
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>672128031</ID>
        <Position>
          <X>998.1328</X>
          <Y>111.3183</Y>
        </Position>
        <VariableName>Position</VariableName>
        <VariableType>VRageMath.Vector3D</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>808663622</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIds>
        <Vector>
          <X>-56.290000915527344</X>
          <Y>126.18000030517578</Y>
          <Z>93.580001831054688</Z>
        </Vector>
        <OutputNodeIdsX />
        <OutputNodeIdsY />
        <OutputNodeIdsZ />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>2147244731</ID>
        <Position>
          <X>1005.82385</X>
          <Y>677.493958</Y>
        </Position>
        <VariableName>Direction</VariableName>
        <VariableType>VRageMath.Vector3D</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>808663622</NodeID>
            <VariableName>direction</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIds>
        <Vector>
          <X>0.50019925832748413</X>
          <Y>-0.8457527756690979</Y>
          <Z>-0.18574956059455872</Z>
        </Vector>
        <OutputNodeIdsX />
        <OutputNodeIdsY />
        <OutputNodeIdsZ />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>2147244736</ID>
        <Position>
          <X>1353.07227</X>
          <Y>675.11554</Y>
        </Position>
        <VariableName>Up</VariableName>
        <VariableType>VRageMath.Vector3D</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>808663622</NodeID>
            <VariableName>up</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIds>
        <Vector>
          <X>0.62910217046737671</X>
          <Y>0.50234740972518921</Y>
          <Z>-0.5931926965713501</Z>
        </Vector>
        <OutputNodeIdsX />
        <OutputNodeIdsY />
        <OutputNodeIdsZ />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>671188205</ID>
        <Position>
          <X>1677.19446</X>
          <Y>439.194916</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.IMyStateMachineScript.Complete(String transitionName)</Type>
        <SequenceInputID>808663622</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>671188212</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>transitionName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>671188212</ID>
        <Position>
          <X>1536.61755</X>
          <Y>496.829651</Y>
        </Position>
        <Value>Reinitialize</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>671188205</NodeID>
              <VariableName>transitionName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>672127904</ID>
        <Position>
          <X>1238.75122</X>
          <Y>282.960236</Y>
        </Position>
        <Value>Combatant mk.1_13 Weapons Ready</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808663622</NodeID>
              <VariableName>subtypeId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>672128041</ID>
        <Position>
          <X>1146.04712</X>
          <Y>571.7201</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.GetLocalPlayerId()</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs />
        <OutputParametersIDs>
          <IdentifierList>
            <OriginType>System.Int64</OriginType>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>808663622</NodeID>
                <VariableName>ownerId</VariableName>
                <OriginType>System.Int64</OriginType>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>20</ID>
        <Position>
          <X>826</X>
          <Y>403</Y>
        </Position>
        <MethodName>Init</MethodName>
        <SequenceOutputIDs>
          <int>678010446</int>
        </SequenceOutputIDs>
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ScriptScriptNode">
        <ID>678010446</ID>
        <Position>
          <X>985.0619</X>
          <Y>402.400757</Y>
        </Position>
        <Name>Once</Name>
        <SequenceOutput>808663622</SequenceOutput>
        <SequenceInput>20</SequenceInput>
        <Inputs />
        <Outputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>21</ID>
        <Position>
          <X>826</X>
          <Y>483</Y>
        </Position>
        <MethodName>Update</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>22</ID>
        <Position>
          <X>826</X>
          <Y>563</Y>
        </Position>
        <MethodName>Dispose</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808663622</ID>
        <Position>
          <X>1346.93091</X>
          <Y>411.917358</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SpawnGroup(String subtypeId, Vector3D position, Vector3D direction, Vector3D up, Int64 ownerId, String newGridName)</Type>
        <SequenceInputID>678010446</SequenceInputID>
        <SequenceOutputID>671188205</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>672127904</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>subtypeId</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>672128031</NodeID>
            <OriginName>position</OriginName>
            <OriginType>VRageMath.Vector3D</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244731</NodeID>
            <OriginName>direction</OriginName>
            <OriginType>VRageMath.Vector3D</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244736</NodeID>
            <OriginName>up</OriginName>
            <OriginType>VRageMath.Vector3D</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>672128041</NodeID>
            <VariableName>Return</VariableName>
            <OriginName>ownerId</OriginName>
            <OriginType>System.Int64</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244737</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>newGridName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244737</ID>
        <Position>
          <X>1387.25732</X>
          <Y>595.34845</Y>
        </Position>
        <Value>Player_Ship_Combatant</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808663622</NodeID>
              <VariableName>newGridName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2141362485</ID>
        <Position>
          <X>1079.87769</X>
          <Y>469.651062</Y>
        </Position>
        <Value>Player_Ship_Combatant</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244901</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244901</ID>
        <Position>
          <X>1103.603</X>
          <Y>397.5326</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveEntity(String entityName)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2141362485</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>289543978</ID>
        <Position>
          <X>0</X>
          <Y>0</Y>
        </Position>
        <MethodName>Deserialize</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>Sector_03v1_Respawn</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>