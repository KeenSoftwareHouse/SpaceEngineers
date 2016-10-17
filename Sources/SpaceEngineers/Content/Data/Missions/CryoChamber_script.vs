<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>6</ID>
      <Position>
        <X>836</X>
        <Y>600</Y>
      </Position>
      <VariableName>MessageId</VariableName>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_EventScriptNode">
      <ID>547</ID>
      <Position>
        <X>801</X>
        <Y>271</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.PlayerLeftCockpit</Name>
      <SequenceOutputID>507</SequenceOutputID>
      <OutputIDs>
        <IdentifierList>
          <Ids />
        </IdentifierList>
        <IdentifierList>
          <Ids />
        </IdentifierList>
        <IdentifierList>
          <Ids />
        </IdentifierList>
      </OutputIDs>
      <OutputNames>
        <string>entityName</string>
        <string>playerId</string>
        <string>gridName</string>
      </OutputNames>
      <OuputTypes>
        <string>System.String</string>
        <string>System.Int64</string>
        <string>System.String</string>
      </OuputTypes>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>546</ID>
      <Position>
        <X>792</X>
        <Y>112</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>97</SequenceOutputID>
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
        <string>t_cryoChamber</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>0</ID>
      <Position>
        <X>852</X>
        <Y>385</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs>
        <int>95</int>
      </SequenceOutputIDs>
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>21</ID>
      <Position>
        <X>2156</X>
        <Y>292</Y>
      </Position>
      <Value> </Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>98</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>20</ID>
      <Position>
        <X>2153</X>
        <Y>195</Y>
      </Position>
      <Value>Door</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>98</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>99</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>17</ID>
      <Position>
        <X>2018</X>
        <Y>232</Y>
      </Position>
      <VariableName>MessageId</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>94</SequenceInputID>
      <SequenceOutputID>98</SequenceOutputID>
      <ValueInputID>
        <NodeID>94</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>16</ID>
      <Position>
        <X>1484</X>
        <Y>297</Y>
      </Position>
      <Value>Press F to open the door.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>94</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>518</ID>
      <Position>
        <X>1421</X>
        <Y>-155</Y>
      </Position>
      <Value>80</Value>
      <Type>System.Single</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>510</NodeID>
            <VariableName>value</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>99</ID>
      <Position>
        <X>2332</X>
        <Y>111</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS(String name)</Type>
      <SequenceInputID>97</SequenceInputID>
      <SequenceOutputID>129</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>20</NodeID>
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
      <ID>15</ID>
      <Position>
        <X>1162</X>
        <Y>208</Y>
      </Position>
      <BoundVariableName>MessageId</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>96</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>97</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>101</ID>
      <Position>
        <X>2611</X>
        <Y>251</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Single thickness, Int32 pulseTimeInFrames)</Type>
      <SequenceInputID>98</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>102</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>93</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>1</ID>
      <Position>
        <X>852</X>
        <Y>465</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>23</ID>
      <Position>
        <X>1964</X>
        <Y>430</Y>
      </Position>
      <Value>slidingDoor_1</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>100</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>93</ID>
      <Position>
        <X>2425</X>
        <Y>477</Y>
      </Position>
      <Value>True</Value>
      <Type>System.Boolean</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>101</NodeID>
            <VariableName>enabled</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>94</ID>
      <Position>
        <X>1705</X>
        <Y>251</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>96</SequenceInputID>
      <SequenceOutputID>17</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>16</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>17</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>17</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>14</ID>
      <Position>
        <X>1815</X>
        <Y>382</Y>
      </Position>
      <VariableName>MessageId</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>95</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <ValueInputID>
        <NodeID>95</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>2</ID>
      <Position>
        <X>852</X>
        <Y>545</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>13</ID>
      <Position>
        <X>1337</X>
        <Y>429</Y>
      </Position>
      <Value>Press F to exit the cryo pod.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>95</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>95</ID>
      <Position>
        <X>1578</X>
        <Y>400</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>0</SequenceInputID>
      <SequenceOutputID>14</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>13</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>14</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>14</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>100</ID>
      <Position>
        <X>2112</X>
        <Y>402</Y>
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
          <NodeID>23</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>98</NodeID>
              <VariableName>position</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>98</NodeID>
              <VariableName>position</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>102</ID>
      <Position>
        <X>2385</X>
        <Y>428</Y>
      </Position>
      <Value>slidingDoor_1</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>101</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>129</ID>
      <Position>
        <X>2611</X>
        <Y>109</Y>
      </Position>
      <Type>VRage.Game.VisualScripting.IMyMissionLogicScript.Complete(String transitionName)</Type>
      <SequenceInputID>99</SequenceInputID>
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
      <ID>507</ID>
      <Position>
        <X>1000</X>
        <Y>-116</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(Int64 playerId, Single value)</Type>
      <SequenceInputID>547</SequenceInputID>
      <SequenceOutputID>508</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>511</NodeID>
          <VariableName>Return</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>515</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>508</ID>
      <Position>
        <X>1191</X>
        <Y>-114</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersEnergyLevel(Int64 playerId, Single value)</Type>
      <SequenceInputID>507</SequenceInputID>
      <SequenceOutputID>509</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>512</NodeID>
          <VariableName>Return</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>516</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>509</ID>
      <Position>
        <X>1373</X>
        <Y>-114</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersOxygenLevel(Int64 playerId, Single value)</Type>
      <SequenceInputID>508</SequenceInputID>
      <SequenceOutputID>510</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>513</NodeID>
          <VariableName>Return</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>517</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>98</ID>
      <Position>
        <X>2333</X>
        <Y>251</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>17</SequenceInputID>
      <SequenceOutputID>101</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>20</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>21</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>100</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>96</ID>
      <Position>
        <X>1317</X>
        <Y>251</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveNotification(Int32 messageId)</Type>
      <SequenceInputID>510</SequenceInputID>
      <SequenceOutputID>94</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>15</NodeID>
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
      <ID>510</ID>
      <Position>
        <X>1565</X>
        <Y>-114</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersHealth(Int64 playerId, Single value)</Type>
      <SequenceInputID>509</SequenceInputID>
      <SequenceOutputID>96</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>514</NodeID>
          <VariableName>Return</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>518</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>513</ID>
      <Position>
        <X>1193</X>
        <Y>-216</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.GetLocalPlayerId()</Type>
      <SequenceInputID>-1</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs />
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>509</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>509</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>514</ID>
      <Position>
        <X>1372</X>
        <Y>-214</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.GetLocalPlayerId()</Type>
      <SequenceInputID>-1</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs />
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>510</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>510</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>515</ID>
      <Position>
        <X>863</X>
        <Y>-162</Y>
      </Position>
      <Value>0</Value>
      <Type>System.Single</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>507</NodeID>
            <VariableName>value</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>516</ID>
      <Position>
        <X>1046</X>
        <Y>-162</Y>
      </Position>
      <Value>0.7</Value>
      <Type>System.Single</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>508</NodeID>
            <VariableName>value</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>517</ID>
      <Position>
        <X>1237</X>
        <Y>-160</Y>
      </Position>
      <Value>0.6</Value>
      <Type>System.Single</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>509</NodeID>
            <VariableName>value</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>511</ID>
      <Position>
        <X>862</X>
        <Y>-216</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.GetLocalPlayerId()</Type>
      <SequenceInputID>-1</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs />
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>507</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>507</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>512</ID>
      <Position>
        <X>999</X>
        <Y>-218</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.GetLocalPlayerId()</Type>
      <SequenceInputID>-1</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs />
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>508</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>508</NodeID>
              <VariableName>playerId</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>97</ID>
      <Position>
        <X>1317</X>
        <Y>116</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveNotification(Int32 messageId)</Type>
      <SequenceInputID>546</SequenceInputID>
      <SequenceOutputID>99</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>15</NodeID>
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
  </Nodes>
  <Name>CryoChamber_script</Name>
</MyObjectBuilder_VisualScript>