<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>21</ID>
        <Position>
          <X>-37</X>
          <Y>-114</Y>
        </Position>
        <VariableName>Counter</VariableName>
        <VariableType>System.Int32</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>24</NodeID>
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
        <ID>19</ID>
        <Position>
          <X>-202</X>
          <Y>-241</Y>
        </Position>
        <Name />
        <SequenceOutputID>988</SequenceOutputID>
        <OutputIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>26</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>988</NodeID>
                <VariableName>Comparator</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputIDs>
        <OutputNames>
          <string>barrierStrenght</string>
          <string>Reset</string>
        </OutputNames>
        <OuputTypes>
          <string>System.Int32</string>
          <string>System.Boolean</string>
        </OuputTypes>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>20</ID>
        <Position>
          <X>836</X>
          <Y>-176</Y>
        </Position>
        <SequenceInputID>106</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>22</ID>
        <Position>
          <X>295</X>
          <Y>-197</Y>
        </Position>
        <VariableName>Counter</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>988</SequenceInputID>
        <SequenceOutputID>25</SequenceOutputID>
        <ValueInputID>
          <NodeID>24</NodeID>
          <VariableName>Output</VariableName>
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>24</ID>
        <Position>
          <X>130</X>
          <Y>-1</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>22</NodeID>
            <VariableName>Value</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>21</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>98</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>25</ID>
        <Position>
          <X>553</X>
          <Y>-196</Y>
        </Position>
        <InputID>
          <NodeID>26</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>22</SequenceInputID>
        <SequenceTrueOutputID>106</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>26</ID>
        <Position>
          <X>387</X>
          <Y>-126</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>25</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>&gt;=</Operation>
        <InputAID>
          <NodeID>19</NodeID>
          <VariableName>barrierStrenght</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>99</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>98</ID>
        <Position>
          <X>-36</X>
          <Y>66</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>24</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>99</ID>
        <Position>
          <X>268</X>
          <Y>-3</Y>
        </Position>
        <BoundVariableName>Counter</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>26</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>106</ID>
        <Position>
          <X>718</X>
          <Y>-178</Y>
        </Position>
        <VariableName>Counter</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>25</SequenceInputID>
        <SequenceOutputID>20</SequenceOutputID>
        <ValueInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>988</ID>
        <Position>
          <X>90.9141541</X>
          <Y>-291.710754</Y>
        </Position>
        <InputID>
          <NodeID>19</NodeID>
          <VariableName>Reset</VariableName>
        </InputID>
        <SequenceInputID>19</SequenceInputID>
        <SequenceTrueOutputID>990</SequenceTrueOutputID>
        <SequnceFalseOutputID>22</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>990</ID>
        <Position>
          <X>296.512</X>
          <Y>-307.2812</Y>
        </Position>
        <VariableName>Counter</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>988</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <ValueInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>CounterBarrierWithReset</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>