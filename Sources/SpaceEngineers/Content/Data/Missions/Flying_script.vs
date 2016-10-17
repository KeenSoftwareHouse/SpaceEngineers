<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>53</ID>
      <Position>
        <X>2108</X>
        <Y>1094</Y>
      </Position>
      <VariableName>Gap1</VariableName>
      <VariableType>VRageMath.Vector3D</VariableType>
      <VariableValue>0</VariableValue>
      <OutputNodeIds />
      <Vector>
        <X>-1108.56005859375</X>
        <Y>-1154.989990234375</Y>
        <Z>6536.83984375</Z>
      </Vector>
      <OutputNodeIdsX />
      <OutputNodeIdsY />
      <OutputNodeIdsZ />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>54</ID>
      <Position>
        <X>2112</X>
        <Y>878</Y>
      </Position>
      <VariableName>Gap2</VariableName>
      <VariableType>VRageMath.Vector3D</VariableType>
      <VariableValue>0</VariableValue>
      <OutputNodeIds />
      <Vector>
        <X>-1116.3800048828125</X>
        <Y>-1133.199951171875</Y>
        <Z>6547.3798828125</Z>
      </Vector>
      <OutputNodeIdsX />
      <OutputNodeIdsY />
      <OutputNodeIdsZ />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>254</ID>
      <Position>
        <X>2259</X>
        <Y>881</Y>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>325</ID>
      <Position>
        <X>2256</X>
        <Y>1171</Y>
      </Position>
      <VariableName>Dialogue</VariableName>
      <VariableType>System.Boolean</VariableType>
      <VariableValue>True</VariableValue>
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
      <ID>321</ID>
      <Position>
        <X>2257</X>
        <Y>1028</Y>
      </Position>
      <VariableName>T_9Trigger</VariableName>
      <VariableType>System.Boolean</VariableType>
      <VariableValue>True</VariableValue>
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
      <ID>288</ID>
      <Position>
        <X>2875</X>
        <Y>181</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>289</SequenceOutputID>
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
        <string>T_10</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>251</ID>
      <Position>
        <X>2460</X>
        <Y>818</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>323</SequenceOutputID>
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
        <string>T_9</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_EventScriptNode">
      <ID>281</ID>
      <Position>
        <X>3445</X>
        <Y>435</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.PlayerPickedUp</Name>
      <SequenceOutputID>280</SequenceOutputID>
      <OutputIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
        <IdentifierList>
          <Ids />
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>279</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids />
        </IdentifierList>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputIDs>
      <OutputNames>
        <string>itemTypeName</string>
        <string>itemSubTypeName</string>
        <string>entityName</string>
        <string>playerId</string>
        <string>amount</string>
      </OutputNames>
      <OuputTypes>
        <string>System.String</string>
        <string>System.String</string>
        <string>System.String</string>
        <string>System.Int64</string>
        <string>System.Int32</string>
      </OuputTypes>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>348</ID>
      <Position>
        <X>3479</X>
        <Y>-152</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>350</SequenceOutputID>
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
        <string>T_11</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>271</ID>
      <Position>
        <X>6056</X>
        <Y>139</Y>
      </Position>
      <Value />
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>269</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
      <ID>280</ID>
      <Position>
        <X>4093</X>
        <Y>436</Y>
      </Position>
      <InputID>
        <NodeID>279</NodeID>
        <VariableName>Output</VariableName>
      </InputID>
      <SequenceInputID>281</SequenceInputID>
      <SequenceTrueOutputID>283</SequenceTrueOutputID>
      <SequnceFalseOutputID>-1</SequnceFalseOutputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>278</ID>
      <Position>
        <X>6067</X>
        <Y>311</Y>
      </Position>
      <Value>Player_hydro</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>277</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
      <ID>279</ID>
      <Position>
        <X>3921</X>
        <Y>475</Y>
      </Position>
      <OutputNodeIDs>
        <MyVariableIdentifier>
          <NodeID>280</NodeID>
          <VariableName>Comparator</VariableName>
        </MyVariableIdentifier>
      </OutputNodeIDs>
      <Operation>==</Operation>
      <InputAID>
        <NodeID>281</NodeID>
        <VariableName>entityName</VariableName>
      </InputAID>
      <InputBID>
        <NodeID>282</NodeID>
        <VariableName>Value</VariableName>
      </InputBID>
      <ValueA />
      <ValueB />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>287</ID>
      <Position>
        <X>4680</X>
        <Y>472</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS(String name)</Type>
      <SequenceInputID>283</SequenceInputID>
      <SequenceOutputID>335</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>293</NodeID>
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
      <ID>277</ID>
      <Position>
        <X>6234</X>
        <Y>282</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.GetEntityPosition(String entityName)</Type>
      <SequenceInputID>-1</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>278</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>269</NodeID>
              <VariableName>position</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>269</NodeID>
              <VariableName>position</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>283</ID>
      <Position>
        <X>4450</X>
        <Y>472</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveNotification(Int32 messageId)</Type>
      <SequenceInputID>280</SequenceInputID>
      <SequenceOutputID>287</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>286</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>286</ID>
      <Position>
        <X>4285</X>
        <Y>557</Y>
      </Position>
      <BoundVariableName>MessageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>283</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>270</ID>
      <Position>
        <X>4589</X>
        <Y>1068</Y>
      </Position>
      <Value>Hydroghen bottle</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>269</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>282</ID>
      <Position>
        <X>3506</X>
        <Y>595</Y>
      </Position>
      <Value>Player_hydro</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>279</NodeID>
            <VariableName>Input_B</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>355</ID>
      <Position>
        <X>5178</X>
        <Y>-111</Y>
      </Position>
      <Type>VRage.Game.VisualScripting.IMyMissionLogicScript.Complete(String transitionName)</Type>
      <SequenceInputID>354</SequenceInputID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>255</ID>
      <Position>
        <X>3447</X>
        <Y>897</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>322</SequenceInputID>
      <SequenceOutputID>261</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>263</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>264</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>262</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>50</ID>
      <Position>
        <X>2458</X>
        <Y>1038</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>51</ID>
      <Position>
        <X>2458</X>
        <Y>1118</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>276</ID>
      <Position>
        <X>4819</X>
        <Y>1104</Y>
      </Position>
      <Value>Player_hydro</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>273</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>289</ID>
      <Position>
        <X>3465</X>
        <Y>255</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS(String name)</Type>
      <SequenceInputID>288</SequenceInputID>
      <SequenceOutputID>290</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>292</NodeID>
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
      <ID>257</ID>
      <Position>
        <X>4319</X>
        <Y>894</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>261</SequenceInputID>
      <SequenceOutputID>260</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>258</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>260</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>260</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>258</ID>
      <Position>
        <X>3936</X>
        <Y>1085</Y>
      </Position>
      <Value>Pick up the bottle of hydrogen.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>257</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>260</ID>
      <Position>
        <X>4571</X>
        <Y>879</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>257</SequenceInputID>
      <SequenceOutputID>269</SequenceOutputID>
      <ValueInputID>
        <NodeID>257</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>261</ID>
      <Position>
        <X>4091</X>
        <Y>897</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification(String message, Int32 disappearTimeMs)</Type>
      <SequenceInputID>255</SequenceInputID>
      <SequenceOutputID>257</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>268</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>265</NodeID>
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
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>342</ID>
      <Position>
        <X>3805</X>
        <Y>110</Y>
      </Position>
      <Value>use Q and E to roll.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>338</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>356</ID>
      <Position>
        <X>4466</X>
        <Y>-228</Y>
      </Position>
      <Value>Proceed to the medical bay.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>354</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>268</ID>
      <Position>
        <X>3407</X>
        <Y>1099</Y>
      </Position>
      <Value>You need hydrogen for your jetpack</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>261</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>52</ID>
      <Position>
        <X>2458</X>
        <Y>1198</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>274</ID>
      <Position>
        <X>4886</X>
        <Y>1014</Y>
      </Position>
      <Value>True</Value>
      <Type>System.Boolean</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>273</NodeID>
            <VariableName>enabled</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>338</ID>
      <Position>
        <X>4018</X>
        <Y>255</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>290</SequenceInputID>
      <SequenceOutputID>341</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>342</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>341</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>341</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>264</ID>
      <Position>
        <X>3324</X>
        <Y>948</Y>
      </Position>
      <Value />
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>255</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>337</ID>
      <Position>
        <X>5023</X>
        <Y>456</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>335</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <ValueInputID>
        <NodeID>335</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>322</ID>
      <Position>
        <X>3191</X>
        <Y>879</Y>
      </Position>
      <VariableName>T_9Trigger</VariableName>
      <VariableValue>False</VariableValue>
      <SequenceInputID>323</SequenceInputID>
      <SequenceOutputID>255</SequenceOutputID>
      <ValueInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>269</ID>
      <Position>
        <X>4766</X>
        <Y>898</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>260</SequenceInputID>
      <SequenceOutputID>273</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>270</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>271</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>277</NodeID>
          <VariableName>Return</VariableName>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>291</ID>
      <Position>
        <X>3603</X>
        <Y>353</Y>
      </Position>
      <BoundVariableName>MessageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>290</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>324</ID>
      <Position>
        <X>2511</X>
        <Y>983</Y>
      </Position>
      <BoundVariableName>T_9Trigger</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>323</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>336</ID>
      <Position>
        <X>4681</X>
        <Y>399</Y>
      </Position>
      <Value>Press X to turn on jetpack.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>335</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>293</ID>
      <Position>
        <X>4841</X>
        <Y>445</Y>
      </Position>
      <Value>Hydroghen bottle</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>287</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
      <ID>323</ID>
      <Position>
        <X>2695</X>
        <Y>855</Y>
      </Position>
      <InputID>
        <NodeID>324</NodeID>
        <VariableName>Value</VariableName>
      </InputID>
      <SequenceInputID>251</SequenceInputID>
      <SequenceTrueOutputID>322</SequenceTrueOutputID>
      <SequnceFalseOutputID>-1</SequnceFalseOutputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>343</ID>
      <Position>
        <X>4555</X>
        <Y>254</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>341</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>347</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>345</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>346</NodeID>
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
      <ID>292</ID>
      <Position>
        <X>2987</X>
        <Y>560</Y>
      </Position>
      <Value>Fly here</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>289</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>273</ID>
      <Position>
        <X>5044</X>
        <Y>892</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Single thickness, Int32 pulseTimeInFrames)</Type>
      <SequenceInputID>269</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>276</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>274</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>-1</NodeID>
          <VariableName />
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>262</ID>
      <Position>
        <X>3322</X>
        <Y>986</Y>
      </Position>
      <BoundVariableName>Gap1</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>255</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>349</ID>
      <Position>
        <X>4559</X>
        <Y>-108</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS(String name)</Type>
      <SequenceInputID>350</SequenceInputID>
      <SequenceOutputID>354</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>352</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>346</ID>
      <Position>
        <X>4211</X>
        <Y>356</Y>
      </Position>
      <BoundVariableName>Gap2</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>343</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>347</ID>
      <Position>
        <X>4369</X>
        <Y>137</Y>
      </Position>
      <Value>Fly here next</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>343</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>335</ID>
      <Position>
        <X>4782</X>
        <Y>473</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>287</SequenceInputID>
      <SequenceOutputID>337</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>336</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>337</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>337</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>350</ID>
      <Position>
        <X>3962</X>
        <Y>-135</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveNotification(Int32 messageId)</Type>
      <SequenceInputID>348</SequenceInputID>
      <SequenceOutputID>349</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>353</NodeID>
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
      <ID>352</ID>
      <Position>
        <X>4369</X>
        <Y>95</Y>
      </Position>
      <Value>Fly here next</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>349</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>353</ID>
      <Position>
        <X>3848</X>
        <Y>69</Y>
      </Position>
      <BoundVariableName>MessageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>350</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>354</ID>
      <Position>
        <X>4851</X>
        <Y>-109</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification(String message, Int32 disappearTimeMs)</Type>
      <SequenceInputID>349</SequenceInputID>
      <SequenceOutputID>355</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>356</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>357</NodeID>
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
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>265</ID>
      <Position>
        <X>3680</X>
        <Y>1101</Y>
      </Position>
      <Value>8000</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>261</NodeID>
            <VariableName>disappearTimeMs</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>290</ID>
      <Position>
        <X>3751</X>
        <Y>255</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveNotification(Int32 messageId)</Type>
      <SequenceInputID>289</SequenceInputID>
      <SequenceOutputID>338</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>291</NodeID>
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
      <ID>263</ID>
      <Position>
        <X>2985</X>
        <Y>602</Y>
      </Position>
      <Value>Fly here</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>255</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>341</ID>
      <Position>
        <X>4274</X>
        <Y>236</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>338</SequenceInputID>
      <SequenceOutputID>343</SequenceOutputID>
      <ValueInputID>
        <NodeID>338</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>345</ID>
      <Position>
        <X>4415</X>
        <Y>337</Y>
      </Position>
      <Value />
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>343</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>357</ID>
      <Position>
        <X>4694</X>
        <Y>30</Y>
      </Position>
      <Value>4000</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>354</NodeID>
            <VariableName>disappearTimeMs</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
  </Nodes>
  <Name>Flying_script</Name>
</MyObjectBuilder_VisualScript>