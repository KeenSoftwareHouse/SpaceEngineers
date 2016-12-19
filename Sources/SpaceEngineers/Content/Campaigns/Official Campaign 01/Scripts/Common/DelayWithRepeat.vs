<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>1417203446</ID>
        <Position>
          <X>2567.75562</X>
          <Y>676.2553</Y>
        </Position>
        <VariableName>Counter</VariableName>
        <VariableType>System.Int32</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>787849390</NodeID>
            <VariableName>Input_B</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>1934783754</NodeID>
            <VariableName>Input_A</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIds>
        <Vector>
          <X>0</X>
          <Y>0</Y>
          <Z>0</Z>
        </Vector>
        <OutputNodeIdsX />
        <OutputNodeIdsY />
        <OutputNodeIdsZ />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InputScriptNode">
        <ID>1224696199</ID>
        <Position>
          <X>2524.97559</X>
          <Y>543.4341</Y>
        </Position>
        <Name />
        <SequenceOutputID>2026244960</SequenceOutputID>
        <OutputIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>787849390</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputIDs>
        <OutputNames>
          <string>NumOfPasses</string>
        </OutputNames>
        <OuputTypes>
          <string>System.Int32</string>
        </OuputTypes>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>1460930376</ID>
        <Position>
          <X>3393.6167</X>
          <Y>631.4145</Y>
        </Position>
        <SequenceInputID>541194039</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147094628</ID>
        <Position>
          <X>2763.75562</X>
          <Y>896.7553</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1934783754</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>1157492420</ID>
        <Position>
          <X>3185.75562</X>
          <Y>520.2553</Y>
        </Position>
        <VariableName>Counter</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>2026244960</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <ValueInputID>
          <NodeID>1934783754</NodeID>
          <VariableName>Output</VariableName>
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>1934783754</ID>
        <Position>
          <X>2907.75562</X>
          <Y>777.2553</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>1157492420</NodeID>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>1417203446</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>2147094628</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>787849390</ID>
        <Position>
          <X>2740.75562</X>
          <Y>634.2553</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>2026244960</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>&gt;</Operation>
        <InputAID>
          <NodeID>1224696199</NodeID>
          <VariableName>NumOfPasses</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>1417203446</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>2026244960</ID>
        <Position>
          <X>2898.75562</X>
          <Y>571.2553</Y>
        </Position>
        <InputID>
          <NodeID>787849390</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>1224696199</SequenceInputID>
        <SequenceTrueOutputID>1157492420</SequenceTrueOutputID>
        <SequnceFalseOutputID>541194039</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>541194039</ID>
        <Position>
          <X>3187.14648</X>
          <Y>620.6306</Y>
        </Position>
        <VariableName>Counter</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>2026244960</SequenceInputID>
        <SequenceOutputID>1460930376</SequenceOutputID>
        <ValueInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>DelayWithRepeat</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>