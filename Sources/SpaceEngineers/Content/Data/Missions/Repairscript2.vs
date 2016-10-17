<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>148</ID>
      <Position>
        <X>3061.8</X>
        <Y>1270</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>149</ID>
      <Position>
        <X>3061.8</X>
        <Y>1350</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>150</ID>
      <Position>
        <X>3061.8</X>
        <Y>1430</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>151</ID>
      <Position>
        <X>3002</X>
        <Y>1100</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>152</SequenceOutputID>
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
        <string>T_5</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>152</ID>
      <Position>
        <X>3360</X>
        <Y>1117</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>151</SequenceInputID>
      <SequenceOutputID>158</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>166</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>158</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>158</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>153</ID>
      <Position>
        <X>3624</X>
        <Y>1011</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveNotification(Int32 messageId)</Type>
      <SequenceInputID>-1</SequenceInputID>
      <SequenceOutputID>157</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>165</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>154</ID>
      <Position>
        <X>2784</X>
        <Y>1343</Y>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>156</ID>
      <Position>
        <X>3946</X>
        <Y>1117</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>158</SequenceInputID>
      <SequenceOutputID>172</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>160</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>159</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>161</NodeID>
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
      <ID>157</ID>
      <Position>
        <X>3944</X>
        <Y>1013</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS(String name)</Type>
      <SequenceInputID>153</SequenceInputID>
      <SequenceOutputID>167</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>160</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>158</ID>
      <Position>
        <X>3618</X>
        <Y>1099</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>152</SequenceInputID>
      <SequenceOutputID>156</SequenceOutputID>
      <ValueInputID>
        <NodeID>152</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>159</ID>
      <Position>
        <X>3825</X>
        <Y>1159</Y>
      </Position>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>156</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>160</ID>
      <Position>
        <X>3757</X>
        <Y>1080</Y>
      </Position>
      <Value>Pick up the welder</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>156</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>157</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>161</ID>
      <Position>
        <X>3618</X>
        <Y>1283</Y>
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
          <NodeID>162</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>156</NodeID>
              <VariableName>position</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>156</NodeID>
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
      <ID>162</ID>
      <Position>
        <X>3425</X>
        <Y>1312</Y>
      </Position>
      <Value>Player_welder</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>161</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>165</ID>
      <Position>
        <X>3396</X>
        <Y>1056</Y>
      </Position>
      <BoundVariableName>MessageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>153</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>166</ID>
      <Position>
        <X>2730</X>
        <Y>1055</Y>
      </Position>
      <Value>The next door is locked. Repair the button pannel to unlock the door.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>152</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
  </Nodes>
  <Name>Repairscript2</Name>
</MyObjectBuilder_VisualScript>