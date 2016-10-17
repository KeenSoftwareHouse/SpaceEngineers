<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>974</ID>
        <Position>
          <X>856</X>
          <Y>464</Y>
        </Position>
        <VariableName>m_triggered</VariableName>
        <VariableType>System.Boolean</VariableType>
        <VariableValue>False</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>975</NodeID>
            <VariableName>Comparator</VariableName>
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
        <ID>973</ID>
        <Position>
          <X>872</X>
          <Y>368</Y>
        </Position>
        <Name />
        <SequenceOutputID>983</SequenceOutputID>
        <OutputIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>983</NodeID>
                <VariableName>Comparator</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputIDs>
        <OutputNames>
          <string>Reset</string>
        </OutputNames>
        <OuputTypes>
          <string>System.Boolean</string>
        </OuputTypes>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>977</ID>
        <Position>
          <X>1577</X>
          <Y>490</Y>
        </Position>
        <SequenceInputID>976</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>975</ID>
        <Position>
          <X>1232</X>
          <Y>440</Y>
        </Position>
        <InputID>
          <NodeID>974</NodeID>
          <VariableName>Value</VariableName>
        </InputID>
        <SequenceInputID>983</SequenceInputID>
        <SequenceTrueOutputID>-1</SequenceTrueOutputID>
        <SequnceFalseOutputID>976</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>976</ID>
        <Position>
          <X>1424</X>
          <Y>488</Y>
        </Position>
        <VariableName>m_triggered</VariableName>
        <VariableValue>True</VariableValue>
        <SequenceInputID>975</SequenceInputID>
        <SequenceOutputID>977</SequenceOutputID>
        <ValueInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>983</ID>
        <Position>
          <X>1047.23572</X>
          <Y>358.9372</Y>
        </Position>
        <InputID>
          <NodeID>973</NodeID>
          <VariableName>Reset</VariableName>
        </InputID>
        <SequenceInputID>973</SequenceInputID>
        <SequenceTrueOutputID>987</SequenceTrueOutputID>
        <SequnceFalseOutputID>975</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>987</ID>
        <Position>
          <X>1232</X>
          <Y>344</Y>
        </Position>
        <VariableName>m_triggered</VariableName>
        <VariableValue>False</VariableValue>
        <SequenceInputID>983</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <ValueInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>OnceWithReset</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>