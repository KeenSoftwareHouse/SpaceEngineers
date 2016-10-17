<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <VisualScript>
    <Interface>VRage.Game.VisualScripting.IMyStateMachineScript</Interface>
    <DependencyFilePaths />
    <Nodes>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
        <ID>2147244668</ID>
        <Position>
          <X>1061.55762</X>
          <Y>554.4253</Y>
        </Position>
        <VariableName>Variable_Name</VariableName>
        <VariableType>VRageMath.Vector3D</VariableType>
        <VariableValue>0</VariableValue>
        <OutputNodeIds>
          <MyVariableIdentifier>
            <NodeID>2147244667</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIds>
        <Vector>
          <X>-57.099998474121094</X>
          <Y>126.26000213623047</Y>
          <Z>94.0199966430664</Z>
        </Vector>
        <OutputNodeIdsX />
        <OutputNodeIdsY />
        <OutputNodeIdsZ />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
        <ID>2147244675</ID>
        <Position>
          <X>823.7862</X>
          <Y>165.220779</Y>
        </Position>
        <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
        <SequenceOutputID>2147244677</SequenceOutputID>
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
          <string>Trigger_Hangar_Internal</string>
          <string />
        </Keys>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244838</ID>
        <Position>
          <X>1991.74585</X>
          <Y>246.570862</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244833</SequenceInputID>
        <SequenceOutputID>2147244843</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244839</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244840</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>enabled</OriginName>
            <OriginType>System.Boolean</OriginType>
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
            <NodeID>2147244841</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>playerId</OriginName>
            <OriginType>System.Int64</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244833</ID>
        <Position>
          <X>1568.603</X>
          <Y>248.743408</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244677</SequenceInputID>
        <SequenceOutputID>2147244838</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244834</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244835</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>enabled</OriginName>
            <OriginType>System.Boolean</OriginType>
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
            <NodeID>2147244836</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>playerId</OriginName>
            <OriginType>System.Int64</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244846</ID>
        <Position>
          <X>2240.1687</X>
          <Y>396.1333</Y>
        </Position>
        <Value>Blue</Value>
        <Type>VRageMath.Color</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244843</NodeID>
              <VariableName>color</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244843</ID>
        <Position>
          <X>2391.78735</X>
          <Y>247.3501</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244838</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244844</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244845</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>enabled</OriginName>
            <OriginType>System.Boolean</OriginType>
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
            <NodeID>2147244846</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>playerId</OriginName>
            <OriginType>System.Int64</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244845</ID>
        <Position>
          <X>2250.77515</X>
          <Y>327.9029</Y>
        </Position>
        <Value>True</Value>
        <Type>System.Boolean</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244843</NodeID>
              <VariableName>enabled</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244841</ID>
        <Position>
          <X>1840.1272</X>
          <Y>395.354126</Y>
        </Position>
        <Value>Blue</Value>
        <Type>VRageMath.Color</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244838</NodeID>
              <VariableName>color</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244840</ID>
        <Position>
          <X>1850.73364</X>
          <Y>327.123657</Y>
        </Position>
        <Value>True</Value>
        <Type>System.Boolean</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244838</NodeID>
              <VariableName>enabled</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244836</ID>
        <Position>
          <X>1416.98438</X>
          <Y>397.526672</Y>
        </Position>
        <Value>Blue</Value>
        <Type>VRageMath.Color</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244833</NodeID>
              <VariableName>color</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244835</ID>
        <Position>
          <X>1427.59082</X>
          <Y>329.296265</Y>
        </Position>
        <Value>True</Value>
        <Type>System.Boolean</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244833</NodeID>
              <VariableName>enabled</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244834</ID>
        <Position>
          <X>1363.2439</X>
          <Y>292.882019</Y>
        </Position>
        <Value>Player_Ship_Landing_Gear_01</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244833</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244875</ID>
        <Position>
          <X>2924.239</X>
          <Y>1040.176</Y>
        </Position>
        <Value>False</Value>
        <Type>System.Boolean</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244873</NodeID>
              <VariableName>enabled</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244870</ID>
        <Position>
          <X>2524.19751</X>
          <Y>1039.39685</Y>
        </Position>
        <Value>False</Value>
        <Type>System.Boolean</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244868</NodeID>
              <VariableName>enabled</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244865</ID>
        <Position>
          <X>2101.05469</X>
          <Y>1041.56946</Y>
        </Position>
        <Value>False</Value>
        <Type>System.Boolean</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244863</NodeID>
              <VariableName>enabled</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244873</ID>
        <Position>
          <X>3065.25122</X>
          <Y>959.6233</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244868</SequenceInputID>
        <SequenceOutputID>2147244684</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244874</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244875</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>enabled</OriginName>
            <OriginType>System.Boolean</OriginType>
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
            <NodeID>2147244876</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>playerId</OriginName>
            <OriginType>System.Int64</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244874</ID>
        <Position>
          <X>2859.89233</X>
          <Y>1003.76184</Y>
        </Position>
        <Value>Player_Ship_Landing_Gear_03</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244873</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244869</ID>
        <Position>
          <X>2459.85083</X>
          <Y>1002.98267</Y>
        </Position>
        <Value>Player_Ship_Landing_Gear_02</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244868</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244868</ID>
        <Position>
          <X>2665.20972</X>
          <Y>958.844</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244863</SequenceInputID>
        <SequenceOutputID>2147244873</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244869</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244870</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>enabled</OriginName>
            <OriginType>System.Boolean</OriginType>
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
            <NodeID>2147244871</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>playerId</OriginName>
            <OriginType>System.Int64</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244863</ID>
        <Position>
          <X>2242.067</X>
          <Y>961.0166</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Int32 thickness, Int32 pulseTimeInFrames, Color color, Int64 playerId)</Type>
        <SequenceInputID>2147244704</SequenceInputID>
        <SequenceOutputID>2147244868</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244864</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244865</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>enabled</OriginName>
            <OriginType>System.Boolean</OriginType>
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
            <NodeID>2147244866</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>color</OriginName>
            <OriginType>VRageMath.Color</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>playerId</OriginName>
            <OriginType>System.Int64</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244876</ID>
        <Position>
          <X>2913.63257</X>
          <Y>1108.40649</Y>
        </Position>
        <Value>Blue</Value>
        <Type>VRageMath.Color</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244873</NodeID>
              <VariableName>color</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244871</ID>
        <Position>
          <X>2513.591</X>
          <Y>1107.62732</Y>
        </Position>
        <Value>Blue</Value>
        <Type>VRageMath.Color</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244868</NodeID>
              <VariableName>color</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244844</ID>
        <Position>
          <X>2186.42847</X>
          <Y>291.488647</Y>
        </Position>
        <Value>Player_Ship_Landing_Gear_03</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244843</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244705</ID>
        <Position>
          <X>1852.5885</X>
          <Y>943.7708</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244704</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244839</ID>
        <Position>
          <X>1786.387</X>
          <Y>290.709473</Y>
        </Position>
        <Value>Player_Ship_Landing_Gear_02</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244838</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244706</ID>
        <Position>
          <X>1884.76038</X>
          <Y>983.720642</Y>
        </Position>
        <Value>10</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244704</NodeID>
              <VariableName>value</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>2147244665</ID>
        <Position>
          <X>826</X>
          <Y>477</Y>
        </Position>
        <MethodName>Update</MethodName>
        <SequenceOutputIDs>
          <int>2147244696</int>
        </SequenceOutputIDs>
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>2147244664</ID>
        <Position>
          <X>826</X>
          <Y>397</Y>
        </Position>
        <MethodName>Init</MethodName>
        <SequenceOutputIDs>
          <int>2147244667</int>
        </SequenceOutputIDs>
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244702</ID>
        <Position>
          <X>1690.55</X>
          <Y>932.55896</Y>
        </Position>
        <Value>Hangar</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244701</NodeID>
              <VariableName>name</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244701</ID>
        <Position>
          <X>1680.65039</X>
          <Y>868.9193</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS(String name)</Type>
        <SequenceInputID>2147244690</SequenceInputID>
        <SequenceOutputID>2147244704</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244702</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>name</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244700</ID>
        <Position>
          <X>759.997437</X>
          <Y>1085.294</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244699</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244699</ID>
        <Position>
          <X>921.217834</X>
          <Y>1057.00977</Y>
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
            <NodeID>2147244700</NodeID>
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
                <NodeID>2147244695</NodeID>
                <VariableName>Input_B</VariableName>
                <OriginType>System.Int32</OriginType>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244697</ID>
        <Position>
          <X>931.1174</X>
          <Y>996.198547</Y>
        </Position>
        <Value>9</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244695</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>2147244696</ID>
        <Position>
          <X>1064.05347</X>
          <Y>826.4929</Y>
        </Position>
        <InputID>
          <NodeID>2147244695</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>2147244665</SequenceInputID>
        <SequenceTrueOutputID>2147244903</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>2147244695</ID>
        <Position>
          <X>1066.88171</X>
          <Y>983.4707</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>2147244696</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>2147244697</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>2147244699</NodeID>
          <VariableName>Return</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>2147244690</ID>
        <Position>
          <X>1499.40161</X>
          <Y>832.567</Y>
        </Position>
        <InputID>
          <NodeID>2147244689</NodeID>
          <VariableName>Output</VariableName>
        </InputID>
        <SequenceInputID>2147244903</SequenceInputID>
        <SequenceTrueOutputID>2147244701</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244866</ID>
        <Position>
          <X>2090.44824</X>
          <Y>1109.7998</Y>
        </Position>
        <Value>Blue</Value>
        <Type>VRageMath.Color</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244863</NodeID>
              <VariableName>color</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244692</ID>
        <Position>
          <X>1362.40161</X>
          <Y>961.567</Y>
        </Position>
        <Value>True</Value>
        <Type>System.Boolean</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244689</NodeID>
              <VariableName>Input_A</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244686</ID>
        <Position>
          <X>1288.40161</X>
          <Y>1014.5672</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.IsLandingGearLocked(String entityName)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244691</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs>
          <IdentifierList>
            <OriginType>System.Boolean</OriginType>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>2147244689</NodeID>
                <VariableName>Input_B</VariableName>
                <OriginType>System.Boolean</OriginType>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
        <ID>2147244689</ID>
        <Position>
          <X>1499.40161</X>
          <Y>975.567</Y>
        </Position>
        <OutputNodeIDs>
          <MyVariableIdentifier>
            <NodeID>2147244690</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </OutputNodeIDs>
        <Operation>==</Operation>
        <InputAID>
          <NodeID>2147244692</NodeID>
          <VariableName>Value</VariableName>
        </InputAID>
        <InputBID>
          <NodeID>2147244686</NodeID>
          <VariableName>Return</VariableName>
        </InputBID>
        <ValueA />
        <ValueB />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244684</ID>
        <Position>
          <X>3344.74268</X>
          <Y>962.591736</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.IMyStateMachineScript.Complete(String transitionName)</Type>
        <SequenceInputID>2147244873</SequenceInputID>
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
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244678</ID>
        <Position>
          <X>1014.2655</X>
          <Y>251.577789</Y>
        </Position>
        <Value>Questlog_Progress</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244677</NodeID>
              <VariableName>key</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244679</ID>
        <Position>
          <X>1046.43738</X>
          <Y>285.870758</Y>
        </Position>
        <Value>9</Value>
        <Type>System.Int32</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244677</NodeID>
              <VariableName>value</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244677</ID>
        <Position>
          <X>1170.55847</X>
          <Y>179.406326</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.StoreInteger(String key, Int32 value)</Type>
        <SequenceInputID>2147244675</SequenceInputID>
        <SequenceOutputID>2147244833</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244678</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>key</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244679</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>value</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244670</ID>
        <Position>
          <X>952.5576</X>
          <Y>551.4253</Y>
        </Position>
        <Value />
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244667</NodeID>
              <VariableName>description</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244669</ID>
        <Position>
          <X>956.5576</X>
          <Y>448.425323</Y>
        </Position>
        <Value>Hangar</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244667</NodeID>
              <VariableName>name</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244667</ID>
        <Position>
          <X>1057.31445</X>
          <Y>414.046844</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
        <SequenceInputID>2147244664</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244669</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>name</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244670</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>description</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244668</NodeID>
            <OriginName>position</OriginName>
            <OriginType>VRageMath.Vector3D</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>-1</NodeID>
            <VariableName />
            <OriginName>disappearsInS</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>2147244666</ID>
        <Position>
          <X>826</X>
          <Y>557</Y>
        </Position>
        <MethodName>Dispose</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244691</ID>
        <Position>
          <X>1097.15576</X>
          <Y>1138.35071</Y>
        </Position>
        <Value>Player_Ship_Landing_Gear_01</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244686</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
            <MyVariableIdentifier>
              <NodeID>2147244902</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
        <ID>2147244864</ID>
        <Position>
          <X>2036.70776</X>
          <Y>1005.15527</Y>
        </Position>
        <Value>Player_Ship_Landing_Gear_01</Value>
        <Type>System.String</Type>
        <OutputIds>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>2147244863</NodeID>
              <VariableName>entityName</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </OutputIds>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244704</ID>
        <Position>
          <X>1874.53125</X>
          <Y>867.35675</Y>
        </Position>
        <Version>1</Version>
        <Type>VRage.Game.VisualScripting.MyVisualScriptLogicProvider.StoreInteger(String key, Int32 value)</Type>
        <SequenceInputID>2147244701</SequenceInputID>
        <SequenceOutputID>2147244863</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244705</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>key</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>2147244706</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>value</OriginName>
            <OriginType>System.Int32</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs />
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
        <ID>2147244902</ID>
        <Position>
          <X>1183.29468</X>
          <Y>936.3434</Y>
        </Position>
        <Version>1</Version>
        <Type>Sandbox.Game.MyVisualScriptLogicProvider.EntityExists(String entityName)</Type>
        <SequenceInputID>-1</SequenceInputID>
        <SequenceOutputID>-1</SequenceOutputID>
        <InstanceInputID>
          <NodeID>-1</NodeID>
          <VariableName />
        </InstanceInputID>
        <InputParameterIDs>
          <MyVariableIdentifier>
            <NodeID>2147244691</NodeID>
            <VariableName>Value</VariableName>
            <OriginName>entityName</OriginName>
            <OriginType>System.String</OriginType>
          </MyVariableIdentifier>
        </InputParameterIDs>
        <OutputParametersIDs>
          <IdentifierList>
            <OriginType>System.Boolean</OriginType>
            <Ids>
              <MyVariableIdentifier>
                <NodeID>2147244903</NodeID>
                <VariableName>Comparator</VariableName>
                <OriginType>System.Boolean</OriginType>
              </MyVariableIdentifier>
            </Ids>
          </IdentifierList>
        </OutputParametersIDs>
        <InputParameterValues />
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
        <ID>2147244903</ID>
        <Position>
          <X>1330.75635</X>
          <Y>801.963</Y>
        </Position>
        <InputID>
          <NodeID>2147244902</NodeID>
          <VariableName>Return</VariableName>
        </InputID>
        <SequenceInputID>2147244696</SequenceInputID>
        <SequenceTrueOutputID>2147244690</SequenceTrueOutputID>
        <SequnceFalseOutputID>-1</SequnceFalseOutputID>
      </MyObjectBuilder_ScriptNode>
      <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
        <ID>66605139</ID>
        <Position>
          <X>825.9005</X>
          <Y>633.567566</Y>
        </Position>
        <MethodName>Deserialize</MethodName>
        <SequenceOutputIDs />
        <OutputIDs />
        <OutputNames />
        <OuputTypes />
      </MyObjectBuilder_ScriptNode>
    </Nodes>
    <Name>Sector_03v1_Return</Name>
  </VisualScript>
</MyObjectBuilder_VSFiles>