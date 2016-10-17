<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <Interface>VRage.Game.VisualScripting.IMyStateMachineScript</Interface>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>808703249</ID>
        <Position>
          <X>1598.415</X>
          <Y>444.7724</Y>
        </Position>
        <VariableName>Count_Rocket</VariableName>
        <VariableType>System.Int32</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>808703250</NodeID>
            <VariableName>Input_A</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808703252</NodeID>
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
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>808678399</ID>
        <Position>
          <X>1580.46875</X>
          <Y>-243.422455</Y>
        </Position>
        <VariableName>Count_Gatling</VariableName>
        <VariableType>System.Int32</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>808678400</NodeID>
            <VariableName>Input_A</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808678402</NodeID>
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
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_EventScriptNode">
        <ID>808665985</ID>
        <Position>
          <X>552.199463</X>
          <Y>-487.214783</Y>
        </Position>
        <Name>Sandbox.Game.MyVisualScriptLogicProvider.BlockBuilt</Name>
        <SequenceOutputID>808715676</SequenceOutputID>
        <OutputIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>808665987</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids />
          </IdentifierList>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>808665991</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids />
          </IdentifierList>
        </OutputIDs>
        <OutputNames>
          <string>typeId</string>
          <string>subtypeId</string>
          <string>gridName</string>
          <string>blockId</string>
        </OutputNames>
        <OuputTypes>
          <string>System.String</string>
          <string>System.String</string>
          <string>System.String</string>
          <string>System.Int64</string>
        </OuputTypes>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_EventScriptNode">
        <ID>808690835</ID>
        <Position>
          <X>529.2197</X>
          <Y>198.7374</Y>
        </Position>
        <Name>Sandbox.Game.MyVisualScriptLogicProvider.BlockBuilt</Name>
        <SequenceOutputID>808715682</SequenceOutputID>
        <OutputIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>808690837</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids />
          </IdentifierList>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>808690841</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
          <IdentifierList>
            <Ids />
          </IdentifierList>
        </OutputIDs>
        <OutputNames>
          <string>typeId</string>
          <string>subtypeId</string>
          <string>gridName</string>
          <string>blockId</string>
        </OutputNames>
        <OuputTypes>
          <string>System.String</string>
          <string>System.String</string>
          <string>System.String</string>
          <string>System.Int64</string>
        </OuputTypes>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808678391</ID>
        <Position>
          <X>2098.87842</X>
          <Y>-406.5688</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808678390</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808672191</ID>
        <Position>
          <X>880.187</X>
          <Y>-608.8524</Y>
        </Position>
        <Value>Red</Value>
        <Type>VRage.Game.MyFontEnum</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808665999</NodeID>
              <VariableName>font</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808665987</ID>
        <Position>
          <X>1103.308</X>
          <Y>-355.657471</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808665988</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808665985</NodeID>
          <VariableName>typeId</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808665989</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808666003</ID>
        <Position>
          <X>1248.18689</X>
          <Y>-641.8524</Y>
        </Position>
        <Value>8000</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808666000</NodeID>
              <VariableName>disappearTimeMs</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808672192</ID>
        <Position>
          <X>1248.18689</X>
          <Y>-601.8524</Y>
        </Position>
        <Value>Red</Value>
        <Type>VRage.Game.MyFontEnum</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808666000</NodeID>
              <VariableName>font</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808666002</ID>
        <Position>
          <X>842.186768</X>
          <Y>-687.8524</Y>
        </Position>
        <Value>Incorrect block type.</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808665999</NodeID>
              <VariableName>message</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808665991</ID>
        <Position>
          <X>1300.3551</X>
          <Y>-353.3005</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808665992</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808665985</NodeID>
          <VariableName>gridName</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808665993</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808665992</ID>
        <Position>
          <X>1292.5769</X>
          <Y>-484.1153</Y>
        </Position>
        <InputID>
          <NodeID>808665991</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>808665988</SequenceInputID>
        <SequenceTrueOutputID>808678398</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>808703248</ID>
        <Position>
          <X>1596.41968</X>
          <Y>226.552521</Y>
        </Position>
        <VariableName>Count_Rocket</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>808690842</SequenceInputID>
        <SequenceOutputID>808703254</SequenceOutputID>
        <ValueInputID>
          <NodeID>808703250</NodeID>
          <VariableName>Output</VariableName>
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808665989</ID>
        <Position>
          <X>1092.40845</X>
          <Y>-234.277786</Y>
        </Position>
        <Value>MyObjectBuilder_SmallGatlingGun</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808665987</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808665978</ID>
        <Position>
          <X>1187.67651</X>
          <Y>659.2545</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.StoreInteger(String key, Int32 value)</Type>
        <SequenceInputID>12</SequenceInputID>
        <SequenceOutputID>2147244745</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808665979</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>key</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808665980</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>value</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>13</ID>
        <Position>
          <X>820.343262</X>
          <Y>720.5879</Y>
        </Position>
        <MethodName>Update</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808678392</ID>
        <Position>
          <X>2131.05</X>
          <Y>-372.275818</Y>
        </Position>
        <Value>2</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808678390</NodeID>
              <VariableName>value</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808665979</ID>
        <Position>
          <X>1030.38367</X>
          <Y>678.426147</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808665978</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808665993</ID>
        <Position>
          <X>1292.577</X>
          <Y>-235.920776</Y>
        </Position>
        <Value>Player_Ship_Combatant</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808665991</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808703254</ID>
        <Position>
          <X>1953.44763</X>
          <Y>226.498566</Y>
        </Position>
        <InputID>
          <NodeID>808703252</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceTrueOutputID>808703240</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808703252</ID>
        <Position>
          <X>1799.60986</X>
          <Y>320.011536</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808703254</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808703249</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808703253</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808665980</ID>
        <Position>
          <X>1063.55518</X>
          <Y>718.719</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808665978</NodeID>
              <VariableName>value</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808666001</ID>
        <Position>
          <X>881.1869</X>
          <Y>-645.8524</Y>
        </Position>
        <Value>8000</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808665999</NodeID>
              <VariableName>disappearTimeMs</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808678400</ID>
        <Position>
          <X>1579.47351</X>
          <Y>-370.3725</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808678398</NodeID>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>808678399</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808678401</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>808678398</ID>
        <Position>
          <X>1578.47339</X>
          <Y>-461.642334</Y>
        </Position>
        <VariableName>Count_Gatling</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>808665992</SequenceInputID>
        <SequenceOutputID>808678404</SequenceOutputID>
        <ValueInputID>
          <NodeID>808678400</NodeID>
          <VariableName>Output</VariableName>
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808678404</ID>
        <Position>
          <X>1935.50146</X>
          <Y>-461.6963</Y>
        </Position>
        <InputID>
          <NodeID>808678402</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceTrueOutputID>808678390</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808678403</ID>
        <Position>
          <X>1701.33752</X>
          <Y>-301.022064</Y>
        </Position>
        <Value>2</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808678402</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808678402</ID>
        <Position>
          <X>1781.66333</X>
          <Y>-368.183319</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808678404</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808678399</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808678403</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808703253</ID>
        <Position>
          <X>1719.28357</X>
          <Y>387.1728</Y>
        </Position>
        <Value>2</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808703252</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808678401</ID>
        <Position>
          <X>1501.82324</X>
          <Y>-304.832855</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808678400</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808665999</ID>
        <Position>
          <X>1027.18689</X>
          <Y>-685.8524</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification(String message, Int32 disappearTimeMs, MyFontEnum font, Int64 playerId)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808666002</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>message</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808666001</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>disappearTimeMs</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808672191</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>font</OriginName>
            <OriginType>VRage.Game.MyFontEnum</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808690854</ID>
        <Position>
          <X>1230.13318</X>
          <Y>6.34243774</Y>
        </Position>
        <Value>Incorrect placement, aim at the ship grid.</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690850</NodeID>
              <VariableName>message</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808690852</ID>
        <Position>
          <X>860.133057</X>
          <Y>0.342437744</Y>
        </Position>
        <Value>Incorrect block type placed.</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690849</NodeID>
              <VariableName>message</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808703240</ID>
        <Position>
          <X>2274.11743</X>
          <Y>262.454529</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.StoreInteger(String key, Int32 value)</Type>
        <SequenceInputID>808703254</SequenceInputID>
        <SequenceOutputID>2147244776</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808703241</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>key</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808703242</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>value</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>12</ID>
        <Position>
          <X>820.343262</X>
          <Y>640.5879</Y>
        </Position>
        <MethodName>Init</MethodName>
        <SequenceOutputIDs>
          <int>808665978</int>
        </SequenceOutputIDs>
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808703251</ID>
        <Position>
          <X>1519.76953</X>
          <Y>383.362</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808703250</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808703242</ID>
        <Position>
          <X>2148.99634</X>
          <Y>315.919037</Y>
        </Position>
        <Value>3</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808703240</NodeID>
              <VariableName>value</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>14</ID>
        <Position>
          <X>820.343262</X>
          <Y>800.5879</Y>
        </Position>
        <MethodName>Dispose</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808666004</ID>
        <Position>
          <X>1236.22864</X>
          <Y>-772.3621</Y>
        </Position>
        <Value>Incorrect placement, aim at the ship grid.</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808666000</NodeID>
              <VariableName>message</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808678390</ID>
        <Position>
          <X>2256.17139</X>
          <Y>-425.740326</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.StoreInteger(String key, Int32 value)</Type>
        <SequenceInputID>808678404</SequenceInputID>
        <SequenceOutputID>2147244755</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808678391</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>key</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808678392</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>value</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808690838</ID>
        <Position>
          <X>1113.47607</X>
          <Y>201.722565</Y>
        </Position>
        <InputID>
          <NodeID>808690837</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>808715682</SequenceInputID>
        <SequenceTrueOutputID>808690842</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808697042</ID>
        <Position>
          <X>1266.13318</X>
          <Y>86.34244</Y>
        </Position>
        <Value>Red</Value>
        <Type>VRage.Game.MyFontEnum</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690850</NodeID>
              <VariableName>font</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808690843</ID>
        <Position>
          <X>1310.52332</X>
          <Y>452.274078</Y>
        </Position>
        <Value>Player_Ship_Combatant</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690841</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808690841</ID>
        <Position>
          <X>1318.30139</X>
          <Y>334.894348</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808690842</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808690835</NodeID>
          <VariableName>gridName</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808690843</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808715683</ID>
        <Position>
          <X>867.958069</X>
          <Y>331.9628</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808715682</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808715684</NodeID>
          <VariableName>Return</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808715686</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808690839</ID>
        <Position>
          <X>1109.35474</X>
          <Y>451.917053</Y>
        </Position>
        <Value>MyObjectBuilder_SmallMissileLauncher</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690837</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808703250</ID>
        <Position>
          <X>1597.4198</X>
          <Y>317.822357</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808703248</NodeID>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>808703249</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808703251</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808690837</ID>
        <Position>
          <X>1121.25427</X>
          <Y>332.537384</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808690838</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808690835</NodeID>
          <VariableName>typeId</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808690839</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808715678</ID>
        <Position>
          <X>709.763</X>
          <Y>-350.689362</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.LoadInteger(String key)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808715679</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>key</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs>
          <IdentifierList>
            <OriginType>System.Int32</OriginType>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>808715677</NodeID>
                <VariableName>Input_A</VariableName>
                <OriginType>System.Int32</OriginType>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808715679</ID>
        <Position>
          <X>545.7142</X>
          <Y>-320.9909</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808715678</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808715680</ID>
        <Position>
          <X>722.4909</X>
          <Y>-255.937073</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808715677</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808715686</ID>
        <Position>
          <X>749.1641</X>
          <Y>439.443054</Y>
        </Position>
        <Value>2</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808715683</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808715685</ID>
        <Position>
          <X>572.3874</X>
          <Y>374.389221</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808715684</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808715684</ID>
        <Position>
          <X>666.4362</X>
          <Y>309.690765</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.LoadInteger(String key)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808715685</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>key</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs>
          <IdentifierList>
            <OriginType>System.Int32</OriginType>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>808715683</NodeID>
                <VariableName>Input_A</VariableName>
                <OriginType>System.Int32</OriginType>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>808715677</ID>
        <Position>
          <X>841.284851</X>
          <Y>-363.417328</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>808715676</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>808715678</NodeID>
          <VariableName>Return</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>808715680</NodeID>
          <VariableName>Value</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808665988</ID>
        <Position>
          <X>1095.52979</X>
          <Y>-486.4723</Y>
        </Position>
        <InputID>
          <NodeID>808665987</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>808715676</SequenceInputID>
        <SequenceTrueOutputID>808665992</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808690849</ID>
        <Position>
          <X>1045.13318</X>
          <Y>2.34243774</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification(String message, Int32 disappearTimeMs, MyFontEnum font, Int64 playerId)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808690852</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>message</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808690851</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>disappearTimeMs</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808697041</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>font</OriginName>
            <OriginType>VRage.Game.MyFontEnum</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808715676</ID>
        <Position>
          <X>836.6126</X>
          <Y>-495.7016</Y>
        </Position>
        <InputID>
          <NodeID>808715677</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>808665985</SequenceInputID>
        <SequenceTrueOutputID>808665988</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808666000</ID>
        <Position>
          <X>1397.18689</X>
          <Y>-683.8524</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification(String message, Int32 disappearTimeMs, MyFontEnum font, Int64 playerId)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808666004</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>message</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808666003</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>disappearTimeMs</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808672192</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>font</OriginName>
            <OriginType>VRage.Game.MyFontEnum</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244777</ID>
        <Position>
          <X>2411.10571</X>
          <Y>295.282562</Y>
        </Position>
        <Value>Projector_Rocket</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244776</NodeID>
              <VariableName>projectorName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808690851</ID>
        <Position>
          <X>899.1332</X>
          <Y>42.3424377</Y>
        </Position>
        <Value>8000</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690849</NodeID>
              <VariableName>disappearTimeMs</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808690850</ID>
        <Position>
          <X>1415.13318</X>
          <Y>4.34243774</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification(String message, Int32 disappearTimeMs, MyFontEnum font, Int64 playerId)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>808690854</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>message</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808690853</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>disappearTimeMs</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>808697042</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>font</OriginName>
            <OriginType>VRage.Game.MyFontEnum</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808690853</ID>
        <Position>
          <X>1266.13318</X>
          <Y>46.3424377</Y>
        </Position>
        <Value>8000</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690850</NodeID>
              <VariableName>disappearTimeMs</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244746</ID>
        <Position>
          <X>1358.50146</X>
          <Y>691.5838</Y>
        </Position>
        <Value>Projector_Combatant</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244745</NodeID>
              <VariableName>projectorName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808690842</ID>
        <Position>
          <X>1310.52319</X>
          <Y>204.079559</Y>
        </Position>
        <InputID>
          <NodeID>808690841</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceTrueOutputID>808703248</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244750</ID>
        <Position>
          <X>1902.75146</X>
          <Y>680.5838</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlightForProjection(String projectorName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244745</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>pulseTimeInFrames</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>thickness</ParameterName>
            <Value>3</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>color</ParameterName>
            <Value>Blue</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>projectorName</ParameterName>
            <Value>Projector_Gatling</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244755</ID>
        <Position>
          <X>2585.75146</X>
          <Y>-462.4162</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlightForProjection(String projectorName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>808678390</SequenceInputID>
        <SequenceOutputID>2147244765</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>thickness</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>pulseTimeInFrames</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>enabled</ParameterName>
            <Value>False</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>color</ParameterName>
            <Value>Blue</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>thickness</ParameterName>
            <Value>1</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>projectorName</ParameterName>
            <Value>Projector_Gatling</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>808715693</ID>
        <Position>
          <X>2799.21118</X>
          <Y>260.762054</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.IMyStateMachineScript.Complete(String transitionName)</Type>
        <SequenceInputID>2147244776</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>transitionName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244765</ID>
        <Position>
          <X>2987.26343</X>
          <Y>-448.873627</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlightForProjection(String projectorName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244755</SequenceInputID>
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
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>pulseTimeInFrames</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>color</ParameterName>
            <Value>Blue</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>enabled</ParameterName>
            <Value>True</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>thickness</ParameterName>
            <Value>3</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>projectorName</ParameterName>
            <Value>Projector_Rocket</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244776</ID>
        <Position>
          <X>2549.47827</X>
          <Y>267.111</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlightForProjection(String projectorName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>808703240</SequenceInputID>
        <SequenceOutputID>808715693</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244777</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>projectorName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>thickness</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>pulseTimeInFrames</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>enabled</ParameterName>
            <Value>False</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>thickness</ParameterName>
            <Value>3</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>808715682</ID>
        <Position>
          <X>863.2858</X>
          <Y>199.678528</Y>
        </Position>
        <InputID>
          <NodeID>808715683</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>808690835</SequenceInputID>
        <SequenceTrueOutputID>808690838</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808703241</ID>
        <Position>
          <X>2116.82446</X>
          <Y>281.626068</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808703240</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>808697041</ID>
        <Position>
          <X>898.1333</X>
          <Y>79.34244</Y>
        </Position>
        <Value>Red</Value>
        <Type>VRage.Game.MyFontEnum</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>808690849</NodeID>
              <VariableName>font</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244745</ID>
        <Position>
          <X>1533.772</X>
          <Y>661.773</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlightForProjection(String projectorName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>808665978</SequenceInputID>
        <SequenceOutputID>2147244750</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244746</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>projectorName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>pulseTimeInFrames</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>thickness</ParameterName>
            <Value>3</Value>
          </MyParameterValue>
          <MyParameterValue>
            <ParameterName>color</ParameterName>
            <Value>Blue</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>947488490</ID>
        <Position>
          <X>528.007935</X>
          <Y>-4.756836</Y>
        </Position>
        <MethodName>Deserialize</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>Sector_03v1_Build</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>