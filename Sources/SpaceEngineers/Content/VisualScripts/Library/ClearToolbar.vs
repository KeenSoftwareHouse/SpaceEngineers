<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InputScriptNode">
        <ID>999</ID>
        <Position>
          <X>553</X>
          <Y>286</Y>
        </Position>
        <Name />
        <SequenceOutputID>1000</SequenceOutputID>
        <OutputIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>1000</NodeID>
                <VariableName>First Index</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>1000</NodeID>
                <VariableName>Last Index</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputIDs>
        <OutputNames>
          <string>FromSlot</string>
          <string>ToSlot</string>
        </OutputNames>
        <OuputTypes>
          <string>System.Int32</string>
          <string>System.Int32</string>
        </OuputTypes>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>1014</ID>
        <Position>
          <X>1153.499</X>
          <Y>366.61908</Y>
        </Position>
        <SequenceInputID>1000</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ForLoopScriptNode">
        <ID>1000</ID>
        <Position>
          <X>796.092651</X>
          <Y>256.066132</Y>
        </Position>
        <SequenceInputs>
          <int>999</int>
        </SequenceInputs>
        <SequenceBody>1001</SequenceBody>
        <SequenceOutput>1014</SequenceOutput>
        <FirstIndexValueInput>
          <NodeID>999</NodeID>
          <VariableName>FromSlot</VariableName>
          <OriginName />
          <OriginType />
        </FirstIndexValueInput>
        <LastIndexValueInput>
          <NodeID>999</NodeID>
          <VariableName>ToSlot</VariableName>
          <OriginName />
          <OriginType />
        </LastIndexValueInput>
        <IncrementValueInput>
          <NodeID>0</NodeID>
        </IncrementValueInput>
        <CounterValueOutputs>
          <MyVariableIdentifier>
            <NodeID>1001</NodeID>
            <VariableName>slot</VariableName>
          </MyVariableIdentifier>
        </CounterValueOutputs>
        <FirstIndexValue>0</FirstIndexValue>
        <LastIndexValue>10</LastIndexValue>
        <IncrementValue>1</IncrementValue>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>1001</ID>
        <Position>
          <X>1148.09265</X>
          <Y>273.066132</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.ClearToolbarSlot(Int32 slot, Int64 playerId)</Type>
        <SequenceInputID>1000</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>1000</NodeID>
            <VariableName>Counter</VariableName>
            <OriginName>slot</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>ClearToolbar</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>