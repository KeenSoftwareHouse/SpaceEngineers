<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>974</ID>
        <Position>
          <X>826</X>
          <Y>264</Y>
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
          <X>852</X>
          <Y>401</Y>
        </Position>
        <Name />
        <SequenceOutputID>975</SequenceOutputID>
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>977</ID>
        <Position>
          <X>1392</X>
          <Y>398</Y>
        </Position>
        <SequenceInputID>976</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>975</ID>
        <Position>
          <X>1051</X>
          <Y>364</Y>
        </Position>
        <InputID>
          <NodeID>974</NodeID>
          <VariableName>Value</VariableName>
        </InputID>
        <SequenceInputID>973</SequenceInputID>
        <SequenceTrueOutputID>-1</SequenceTrueOutputID>
        <SequnceFalseOutputID>976</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>976</ID>
        <Position>
          <X>1239</X>
          <Y>396</Y>
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
    </Nodes>
    <Name>Once</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>