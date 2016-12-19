<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>311619742</ID>
        <Position>
          <X>703.8065</X>
          <Y>418.686462</Y>
        </Position>
        <VariableName>DroneId</VariableName>
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
        <ID>2044405504</ID>
        <Position>
          <X>784</X>
          <Y>327</Y>
        </Position>
        <Name />
        <SequenceOutputID>984221828</SequenceOutputID>
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_OutputScriptNode">
        <ID>125310003</ID>
        <Position>
          <X>2356.83936</X>
          <Y>371.4544</Y>
        </Position>
        <SequenceInputID>25760006</SequenceInputID>
        <Inputs />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>128328707</ID>
        <Position>
          <X>432.560181</X>
          <Y>201.7351</Y>
        </Position>
        <Version>1</Version>
        <DeclaringType />
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.LoadInteger(String key)</Type>
        <ExtOfType />
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs />
        <OutputParametersIDs>
          <IdentifierList>
            <OriginType>System.Int32</OriginType>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>1045359760</NodeID>
                <VariableName>Input_A</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>key</ParameterName>
            <Value>ShelfDroneId</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>1045359760</ID>
        <Position>
          <X>736.4008</X>
          <Y>192.325455</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>984221828</NodeID>
            <VariableName>value</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2031917568</NodeID>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>128328707</NodeID>
          <VariableName>Return</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>546594772</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>546594772</ID>
        <Position>
          <X>600.175537</X>
          <Y>282.3014</Y>
        </Position>
        <Value>1</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1045359760</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>984221828</ID>
        <Position>
          <X>936.533936</X>
          <Y>363.0275</Y>
        </Position>
        <Version>1</Version>
        <DeclaringType />
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.StoreInteger(String key, Int32 value)</Type>
        <ExtOfType />
        <SequenceInputID>2044405504</SequenceInputID>
        <SequenceOutputID>2031917568</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>1045359760</NodeID>
            <VariableName>Output</VariableName>
            <OriginName>value</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>key</ParameterName>
            <Value>ShelfDroneId</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
        <ID>2031917568</ID>
        <Position>
          <X>1170.30334</X>
          <Y>304.164673</Y>
        </Position>
        <VariableName>DroneId</VariableName>
        <VariableValue>0</VariableValue>
        <SequenceInputID>984221828</SequenceInputID>
        <SequenceOutputID>1545297353</SequenceOutputID>
        <ValueInputID>
          <NodeID>1045359760</NodeID>
          <VariableName>Output</VariableName>
          <OriginName />
          <OriginType />
        </ValueInputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>1194928738</ID>
        <Position>
          <X>1383.44434</X>
          <Y>245.196136</Y>
        </Position>
        <Version>1</Version>
        <DeclaringType />
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.GetEntityByName(String name)</Type>
        <ExtOfType />
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>1974181778</NodeID>
            <VariableName>Output</VariableName>
            <OriginName>name</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs>
          <IdentifierList>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>778973648</NodeID>
                <VariableName>item</VariableName>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>name</ParameterName>
            <Value>ShelfWaypoint1</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>1545297353</ID>
        <Position>
          <X>1409.05054</X>
          <Y>316.663666</Y>
        </Position>
        <Version>1</Version>
        <DeclaringType />
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetGridReactors(String gridName, Boolean turnOn)</Type>
        <ExtOfType />
        <SequenceInputID>2031917568</SequenceInputID>
        <SequenceOutputID>778973648</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>51075721</NodeID>
            <VariableName>Output</VariableName>
            <OriginName>gridName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>gridName</ParameterName>
            <Value>ShelfDrone</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_NewListScriptNode">
        <ID>175846681</ID>
        <Position>
          <X>1452.16187</X>
          <Y>414.090424</Y>
        </Position>
        <Type>VRage.Game.Entity.MyEntity</Type>
        <DefaultEntries />
        <Connections>
          <MyVariableIdentifier>
            <NodeID>25760006</NodeID>
            <VariableName>waypoints</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>778973648</NodeID>
            <VariableName>Instance</VariableName>
          </MyVariableIdentifier>
        </Connections>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>51075721</ID>
        <Position>
          <X>1256.5719</X>
          <Y>397.7914</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>1545297353</NodeID>
            <VariableName>gridName</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>25760006</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>1962697311</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>1628818634</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>1962697311</ID>
        <Position>
          <X>1091.4491</X>
          <Y>455.739258</Y>
        </Position>
        <Value>ShelfDrone</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>51075721</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>1628818634</ID>
        <Position>
          <X>1111.24817</X>
          <Y>546.249146</Y>
        </Position>
        <BoundVariableName>DroneId</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>51075721</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
        <ID>764208654</ID>
        <Position>
          <X>1067.74353</X>
          <Y>211.329</Y>
        </Position>
        <BoundVariableName>DroneId</BoundVariableName>
        <OutputIDs>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1974181778</NodeID>
              <VariableName>Input_B</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIDs>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>782348121</ID>
        <Position>
          <X>1047.94446</X>
          <Y>120.819107</Y>
        </Position>
        <Value>ShelfWaypoint</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>1974181778</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>1974181778</ID>
        <Position>
          <X>1220.06726</X>
          <Y>172.871246</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>1194928738</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>+</Operation>
        <InputAID>
          <NodeID>782348121</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputAID>
        <InputBID>
          <NodeID>764208654</NodeID>
          <VariableName>Value</VariableName>
          <OriginName />
          <OriginType />
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>25760006</ID>
        <Position>
          <X>1997.375</X>
          <Y>388.178864</Y>
        </Position>
        <Version>1</Version>
        <DeclaringType />
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetDroneBehaviourAdvanced(String entityName, String presetName, Boolean activate, Boolean assignToPirates, System.Collections.Generic.List`1[[VRage.Game.Entity.MyEntity, VRage.Game, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]] waypoints, Boolean cycleWaypoints, System.Collections.Generic.List`1[[VRage.Game.Entity.MyEntity, VRage.Game, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]] targets)</Type>
        <ExtOfType />
        <SequenceInputID>778973648</SequenceInputID>
        <SequenceOutputID>125310003</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>51075721</NodeID>
            <VariableName>Output</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>175846681</NodeID>
            <VariableName>Instance</VariableName>
            <OriginName>waypoints</OriginName>
            <OriginType>System.Collections.Generic.List`1[[VRage.Game.Entity.MyEntity, VRage.Game, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues>
          <MyParameterValue>
            <ParameterName>presetName</ParameterName>
            <Value>C_CloseRangeGatlingDrone</Value>
          </MyParameterValue>
        </InputParameterValues>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>778973648</ID>
        <Position>
          <X>1729.93469</X>
          <Y>333.218262</Y>
        </Position>
        <Version>1</Version>
        <DeclaringType>System.Collections.Generic.List`1[[VRage.Game.Entity.MyEntity, VRage.Game, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]</DeclaringType>
        <Type>System.Collections.Generic.List`1[[VRage.Game.Entity.MyEntity, VRage.Game, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]].Add(MyEntity item)</Type>
        <ExtOfType />
        <SequenceInputID>1545297353</SequenceInputID>
        <SequenceOutputID>25760006</SequenceOutputID>
        <InstanceInputID>
          <NodeID>175846681</NodeID>
          <VariableName>Instance</VariableName>
          <OriginName />
          <OriginType />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>1194928738</NodeID>
            <VariableName>Return</VariableName>
            <OriginName>item</OriginName>
            <OriginType>VRage.Game.Entity.MyEntity</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>ActivateShelfDrone</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>