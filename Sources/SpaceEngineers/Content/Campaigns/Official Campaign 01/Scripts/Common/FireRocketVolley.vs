<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>1574657783</ID>
        <Position>
          <X>753.3308</X>
          <Y>119.320831</Y>
        </Position>
        <VariableName>Launcher</VariableName>
        <VariableType>System.Int32</VariableType>
        <VariableValue>1</VariableValue>
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
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>1117714484</ID>
        <Position>
          <X>755.3308</X>
          <Y>218.320831</Y>
        </Position>
        <VariableName>Timer</VariableName>
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
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InputScriptNode">
        <ID>238089931</ID>
        <Position>
          <X>743.6949</X>
          <Y>322.050232</Y>
        </Position>
        <Name />
        <SequenceOutputID>579741974</SequenceOutputID>
        <OutputIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>579741974</NodeID>
                <VariableName>Comparator</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>1687150768</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>618025036</NodeID>
                <VariableName>Input_B</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>1479128979</NodeID>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputIDs>
        <OutputNames>
          <string>Reset</string>
          <string>LauncherName</string>
          <string>LauncherCount</string>
          <string>DelayBetweenShots</string>
        </OutputNames>
        <OuputTypes>
          <string>System.Boolean</string>
          <string>System.String</string>
          <string>System.Int32</string>
          <string>System.Int32</string>
        </OuputTypes>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>847010548</ID>
        <Position>
          <X>1594.86353</X>
          <Y>539.5506</Y>
        </Position>
        <SequenceInputID>1896814389</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>144303622</ID>
        <Position>
          <X>1945.86353</X>
          <Y>334.5506</Y>
        </Position>
        <SequenceInputID>687867635</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>1925906144</ID>
        <Position>
          <X>1572.86353</X>
          <Y>92.5506</Y>
        </Position>
        <SequenceInputID>1344208247</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>753798531</ID>
        <Position>
          <X>2384.9834</X>
          <Y>500.7931</Y>
        </Position>
        <SequenceInputID>1304226324</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>1344208247</ID>
        <Position>
          <X>1420.33081</X>
          <Y>93.32083</Y>
        </Position>
        <VariableName>Timer</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>491970790</SequenceInputID>
        <SequenceOutputID>1925906144</SequenceOutputID>
        <ValueInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>1687150768</ID>
        <Position>
          <X>1100.328</X>
          <Y>902.879944</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>1991757898</NodeID>
            <VariableName>weaponName</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>238089931</NodeID>
          <VariableName>LauncherName</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>1183801404</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>1183801404</ID>
        <Position>
          <X>950.421265</X>
          <Y>973.590637</Y>
        </Position>
        <BoundVariableName>Launcher</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1687150768</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>1991757898</ID>
        <Position>
          <X>1960.0282</X>
          <Y>493.150421</Y>
        </Position>
        <Version>1</Version>
        <DeclaringType />
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.WeaponShootOnce(String weaponName)</Type>
        <ExtOfType />
        <SequenceInputID>1479128979</SequenceInputID>
        <SequenceOutputID>1304226324</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>1687150768</NodeID>
            <VariableName>Output</VariableName>
            <OriginName>weaponName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>1522036208</ID>
        <Position>
          <X>1903.55688</X>
          <Y>686.4605</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1284907591</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>1721553044</ID>
        <Position>
          <X>1888.09717</X>
          <Y>610.3513</Y>
        </Position>
        <BoundVariableName>Launcher</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1284907591</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>579741974</ID>
        <Position>
          <X>1071.603</X>
          <Y>312.2742</Y>
        </Position>
        <InputID>
          <NodeID>238089931</NodeID>
          <VariableName>Reset</VariableName>
          <OriginName />
          <OriginType />
        </InputID>
        <SequenceInputID>238089931</SequenceInputID>
        <SequenceTrueOutputID>491970790</SequenceTrueOutputID>
        <SequnceFalseOutputID>1896814389</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>1896814389</ID>
        <Position>
          <X>1299.97949</X>
          <Y>412.0643</Y>
        </Position>
        <InputID>
          <NodeID>618025036</NodeID>
          <VariableName>Output</VariableName>
          <OriginName />
          <OriginType />
        </InputID>
        <SequenceInputID>579741974</SequenceInputID>
        <SequenceTrueOutputID>590796230</SequenceTrueOutputID>
        <SequnceFalseOutputID>847010548</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>1284907591</ID>
        <Position>
          <X>2030.802</X>
          <Y>582.9995</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>1304226324</NodeID>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>1721553044</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>1522036208</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>1479128979</ID>
        <Position>
          <X>1789.70154</X>
          <Y>472.716675</Y>
        </Position>
        <VariableName>Timer</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>590796230</SequenceInputID>
        <SequenceOutputID>1991757898</SequenceOutputID>
        <ValueInputID>
          <NodeID>238089931</NodeID>
          <VariableName>DelayBetweenShots</VariableName>
          <OriginName />
          <OriginType />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>291439006</ID>
        <Position>
          <X>1024.16541</X>
          <Y>445.700134</Y>
        </Position>
        <BoundVariableName>Launcher</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>618025036</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>1333691913</ID>
        <Position>
          <X>1362.675</X>
          <Y>566.207153</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>590796230</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>&gt;</Operation>
        <InputAID>
          <NodeID>1731242986</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>1588293951</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>491970790</ID>
        <Position>
          <X>1282.33081</X>
          <Y>97.32083</Y>
        </Position>
        <VariableName>Launcher</VariableName>
        <VariableValue>1</VariableValue>
        <SequenceInputID>579741974</SequenceInputID>
        <SequenceOutputID>1344208247</SequenceOutputID>
        <ValueInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2143383966</ID>
        <Position>
          <X>1480.11121</X>
          <Y>349.76062</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1031826891</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>590796230</ID>
        <Position>
          <X>1591.644</X>
          <Y>427.594879</Y>
        </Position>
        <InputID>
          <NodeID>1333691913</NodeID>
          <VariableName>Output</VariableName>
          <OriginName />
          <OriginType />
        </InputID>
        <SequenceInputID>1896814389</SequenceInputID>
        <SequenceTrueOutputID>687867635</SequenceTrueOutputID>
        <SequnceFalseOutputID>1479128979</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>618025036</ID>
        <Position>
          <X>1153.66345</X>
          <Y>472.608826</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>1896814389</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>&lt;=</Operation>
        <InputAID>
          <NodeID>291439006</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>238089931</NodeID>
          <VariableName>LauncherCount</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>1731242986</ID>
        <Position>
          <X>1215.329</X>
          <Y>604.023132</Y>
        </Position>
        <BoundVariableName>Timer</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1333691913</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>1588293951</ID>
        <Position>
          <X>1237.36914</X>
          <Y>652.2206</Y>
        </Position>
        <Value>0</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1333691913</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>1304226324</ID>
        <Position>
          <X>2228.21045</X>
          <Y>498.565765</Y>
        </Position>
        <VariableName>Launcher</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>1991757898</SequenceInputID>
        <SequenceOutputID>753798531</SequenceOutputID>
        <ValueInputID>
          <NodeID>1284907591</NodeID>
          <VariableName>Output</VariableName>
          <OriginName />
          <OriginType />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>1069304207</ID>
        <Position>
          <X>1469.75586</X>
          <Y>281.8454</Y>
        </Position>
        <BoundVariableName>Timer</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1031826891</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>687867635</ID>
        <Position>
          <X>1795.11121</X>
          <Y>337.76062</Y>
        </Position>
        <VariableName>Timer</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>590796230</SequenceInputID>
        <SequenceOutputID>144303622</SequenceOutputID>
        <ValueInputID>
          <NodeID>1031826891</NodeID>
          <VariableName>Output</VariableName>
          <OriginName />
          <OriginType />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>1031826891</ID>
        <Position>
          <X>1606.72058</X>
          <Y>278.2195</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>687867635</NodeID>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>-</Operation>
        <InputAID>
          <NodeID>1069304207</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>2143383966</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>FireRocketVolley</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>