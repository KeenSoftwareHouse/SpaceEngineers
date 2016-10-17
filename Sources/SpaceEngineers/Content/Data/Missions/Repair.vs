<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>145</ID>
      <Position>
        <X>2875.8</X>
        <Y>1190</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>146</ID>
      <Position>
        <X>2875.8</X>
        <Y>1270</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>147</ID>
      <Position>
        <X>2875.8</X>
        <Y>1350</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>167</ID>
      <Position>
        <X>4338</X>
        <Y>1012</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddNotification(String message)</Type>
      <SequenceInputID>157</SequenceInputID>
      <SequenceOutputID>168</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>182</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>168</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>168</NodeID>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
      </OutputParametersIDs>
      <InputParameterValues>
        <string />
      </InputParameterValues>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>168</ID>
      <Position>
        <X>4629</X>
        <Y>995</Y>
      </Position>
      <VariableName>VariableName</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>167</SequenceInputID>
      <SequenceOutputID>169</SequenceOutputID>
      <ValueInputID>
        <NodeID>167</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>169</ID>
      <Position>
        <X>4876</X>
        <Y>1015</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.AddGPS(String name, String description, Vector3D position, Int32 disappearsInS)</Type>
      <SequenceInputID>168</SequenceInputID>
      <SequenceOutputID>175</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>171</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>170</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>180</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>170</ID>
      <Position>
        <X>4660</X>
        <Y>1138</Y>
      </Position>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>169</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>171</ID>
      <Position>
        <X>4624</X>
        <Y>925</Y>
      </Position>
      <Value>Repair the button pannel</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>169</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>172</ID>
      <Position>
        <X>4234</X>
        <Y>1117</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Single thickness, Int32 pulseTimeInFrames)</Type>
      <SequenceInputID>156</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>174</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>173</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>173</ID>
      <Position>
        <X>4078</X>
        <Y>1337</Y>
      </Position>
      <Value>True</Value>
      <Type>System.Boolean</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>172</NodeID>
            <VariableName>enabled</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>174</ID>
      <Position>
        <X>4037</X>
        <Y>1297</Y>
      </Position>
      <Value>Player_welder</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>172</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>175</ID>
      <Position>
        <X>5171</X>
        <Y>1016</Y>
      </Position>
      <Type>Sandbox.Game.MyVisualScriptLogicProvider.SetHighlight(String entityName, Boolean enabled, Single thickness, Int32 pulseTimeInFrames)</Type>
      <SequenceInputID>169</SequenceInputID>
      <SequenceOutputID>-1</SequenceOutputID>
      <InstanceInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </InstanceInputID>
      <InputParameterIDs>
        <MyVariableIdentifier>
          <NodeID>177</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
        <MyVariableIdentifier>
          <NodeID>176</NodeID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>176</ID>
      <Position>
        <X>5025</X>
        <Y>1249</Y>
      </Position>
      <Value>True</Value>
      <Type>System.Boolean</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>175</NodeID>
            <VariableName>enabled</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>177</ID>
      <Position>
        <X>4986</X>
        <Y>1200</Y>
      </Position>
      <Value>Brokenpannel</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>175</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_FunctionScriptNode">
      <ID>180</ID>
      <Position>
        <X>4570</X>
        <Y>1290</Y>
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
          <NodeID>181</NodeID>
          <VariableName>Value</VariableName>
        </MyVariableIdentifier>
      </InputParameterIDs>
      <OutputParametersIDs>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>169</NodeID>
              <VariableName>position</VariableName>
            </MyVariableIdentifier>
          </Ids>
        </IdentifierList>
        <IdentifierList>
          <Ids>
            <MyVariableIdentifier>
              <NodeID>169</NodeID>
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
      <ID>181</ID>
      <Position>
        <X>4404</X>
        <Y>1319</Y>
      </Position>
      <Value>Brokenpannel</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>180</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>182</ID>
      <Position>
        <X>3950</X>
        <Y>939</Y>
      </Position>
      <Value>Equip your welder. LMB to use it on the button pannel.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>167</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
  </Nodes>
  <Name>Repair</Name>
</MyObjectBuilder_VisualScript>