<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>38</ID>
      <Position>
        <X>1554</X>
        <Y>560</Y>
      </Position>
      <VariableName>GPSPosition</VariableName>
      <VariableType>VRageMath.Vector3D</VariableType>
      <VariableValue>0</VariableValue>
      <OutputNodeIds />
      <Vector>
        <X>-1082.52001953125</X>
        <Y>-1318.800048828125</Y>
        <Z>6478.60986328125</Z>
      </Vector>
      <OutputNodeIdsX />
      <OutputNodeIdsY />
      <OutputNodeIdsZ />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>103</ID>
      <Position>
        <X>1583</X>
        <Y>770</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableType>System.Int32</VariableType>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>125</ID>
      <Position>
        <X>1766</X>
        <Y>173</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>120</SequenceOutputID>
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
        <string>Gap_jumped</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>121</ID>
      <Position>
        <X>2661</X>
        <Y>189</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS(String name)</Type>
      <SequenceInputID>120</SequenceInputID>
      <SequenceOutputID>131</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>34</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>120</ID>
      <Position>
        <X>2183</X>
        <Y>190</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveNotification(Int32 messageId)</Type>
      <SequenceInputID>125</SequenceInputID>
      <SequenceOutputID>121</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>127</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>115</ID>
      <Position>
        <X>2107</X>
        <Y>492</Y>
      </Position>
      <Value>Press SPACE to jump</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>105</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>113</ID>
      <Position>
        <X>2397</X>
        <Y>338</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>105</SequenceInputID>
      <SequenceOutputID>35</SequenceOutputID>
      <ValueInputID>
        <NodeID>105</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>105</ID>
      <Position>
        <X>2183</X>
        <Y>356</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>31</SequenceInputID>
      <SequenceOutputID>113</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>115</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>113</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>113</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>127</ID>
      <Position>
        <X>2029</X>
        <Y>280</Y>
      </Position>
      <BoundVariableName>MessageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>120</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>39</ID>
      <Position>
        <X>2479</X>
        <Y>521</Y>
      </Position>
      <BoundVariableName>GPSPosition</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>35</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>36</ID>
      <Position>
        <X>2487</X>
        <Y>480</Y>
      </Position>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>35</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>35</ID>
      <Position>
        <X>2657</X>
        <Y>357</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>113</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>34</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>36</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>39</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>-1</NodeID>
          <VariableName />
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
        <string />
        <string />
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>34</ID>
      <Position>
        <X>2470</X>
        <Y>289</Y>
      </Position>
      <Value>Jump the gap</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>35</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>121</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>33</ID>
      <Position>
        <X>1969</X>
        <Y>402</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>32</ID>
      <Position>
        <X>1968</X>
        <Y>468</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>31</ID>
      <Position>
        <X>1824</X>
        <Y>338</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs>
        <int>105</int>
      </SequenceOutputIDs>
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>131</ID>
      <Position>
        <X>2768</X>
        <Y>192</Y>
      </Position>
      <Type>VRage.Game.VisualScripting.IMyMissionLogicScript.Complete(String transitionName)</Type>
      <SequenceInputID>121</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>-1</NodeID>
          <VariableName />
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
  </Nodes>
  <Name>Jump_script</Name>
</MyObjectBuilder_VisualScript>