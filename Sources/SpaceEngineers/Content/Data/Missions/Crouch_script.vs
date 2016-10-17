<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>44</ID>
      <Position>
        <X>2069</X>
        <Y>316</Y>
      </Position>
      <VariableName>GPSPosition</VariableName>
      <VariableType>VRageMath.Vector3D</VariableType>
      <VariableValue>0</VariableValue>
      <OutputNodeIds />
      <Vector>
        <X>-1088.2099609375</X>
        <Y>-1305.969970703125</Y>
        <Z>6491.43994140625</Z>
      </Vector>
      <OutputNodeIdsX />
      <OutputNodeIdsY />
      <OutputNodeIdsZ />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>132</ID>
      <Position>
        <X>2255</X>
        <Y>319</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>79</SequenceOutputID>
      <OutputIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputIDs>
      <OutputNames>
        <string>triggerName</string>
        <string>playerId</string>
      </OutputNames>
      <OuputTypes>
        <string>System.String</string>
        <string>System.Int64</string>
      </OuputTypes>
      <Keys>
        <string>crouch</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>142</ID>
      <Position>
        <X>2257</X>
        <Y>145</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>143</SequenceOutputID>
      <OutputIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputIDs>
      <OutputNames>
        <string>triggerName</string>
        <string>playerId</string>
      </OutputNames>
      <OuputTypes>
        <string>System.String</string>
        <string>System.Int64</string>
      </OuputTypes>
      <Keys>
        <string>T_4</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>41</ID>
      <Position>
        <X>2315</X>
        <Y>493</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>42</ID>
      <Position>
        <X>2314</X>
        <Y>565</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>43</ID>
      <Position>
        <X>2106</X>
        <Y>648</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>80</ID>
      <Position>
        <X>2524</X>
        <Y>376</Y>
      </Position>
      <Value />
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>79</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>81</ID>
      <Position>
        <X>2415</X>
        <Y>271</Y>
      </Position>
      <Value>Press C to crouch</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>79</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>89</ID>
      <Position>
        <X>2496</X>
        <Y>413</Y>
      </Position>
      <BoundVariableName>GPSPosition</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>79</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>294</ID>
      <Position>
        <X>2416</X>
        <Y>231</Y>
      </Position>
      <Value>Press C to crouch</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>143</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>543</ID>
      <Position>
        <X>3191</X>
        <Y>223</Y>
      </Position>
      <Value>6000</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>542</NodeID>
            <VariableName>disappearTimeMs</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>544</ID>
      <Position>
        <X>3129</X>
        <Y>185</Y>
      </Position>
      <Value>go to engineering</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>542</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>554</ID>
      <Position>
        <X>2983</X>
        <Y>165</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>143</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>555</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>556</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>-1</NodeID>
          <VariableName />
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>557</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs />
      <InputParameterValues>
        <string />
        <string />
        <string />
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>558</ID>
      <Position>
        <X>2068</X>
        <Y>94</Y>
      </Position>
      <VariableName>Engineering</VariableName>
      <VariableType>VRageMath.Vector3D</VariableType>
      <VariableValue>0</VariableValue>
      <OutputNodeIds />
      <Vector>
        <X>0</X>
        <Y>0</Y>
        <Z>0</Z>
      </Vector>
      <OutputNodeIdsX />
      <OutputNodeIdsY />
      <OutputNodeIdsZ />
    </MyObjectBuilder_ScriptNode>
  </Nodes>
  <Name>Crouch_script</Name>
</MyObjectBuilder_VisualScript>