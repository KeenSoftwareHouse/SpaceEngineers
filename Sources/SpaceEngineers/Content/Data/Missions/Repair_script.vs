<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>159</ID>
      <Position>
        <X>3035</X>
        <Y>564</Y>
      </Position>
      <VariableName>messageID</VariableName>
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
      <ID>295</ID>
      <Position>
        <X>2641</X>
        <Y>360</Y>
      </Position>
      <VariableName>T_5true</VariableName>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_EventScriptNode">
      <ID>183</ID>
      <Position>
        <X>2731</X>
        <Y>-29</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.PlayerPickedUp</Name>
      <SequenceOutputID>184</SequenceOutputID>
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
              <NodeID>185</NodeID>
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
      <ID>198</ID>
      <Position>
        <X>4293</X>
        <Y>-213</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>191</SequenceOutputID>
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
        <string>T_6</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>149</ID>
      <Position>
        <X>2750</X>
        <Y>269</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>296</SequenceOutputID>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>182</ID>
      <Position>
        <X>4587</X>
        <Y>103</Y>
      </Position>
      <Value>Press 1 to select your welder and repair the door.</Value>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
      <ID>184</ID>
      <Position>
        <X>3546</X>
        <Y>31</Y>
      </Position>
      <InputID>
        <NodeID>185</NodeID>
        <VariableName>Output</VariableName>
      </InputID>
      <SequenceInputID>183</SequenceInputID>
      <SequenceTrueOutputID>187</SequenceTrueOutputID>
      <SequnceFalseOutputID>-1</SequnceFalseOutputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
      <ID>185</ID>
      <Position>
        <X>2972</X>
        <Y>12</Y>
      </Position>
      <OutputNodeIDs>
        <MyVariableIdentifier>
          <NodeID>184</NodeID>
          <VariableName>Comparator</VariableName>
        </MyVariableIdentifier>
      </OutputNodeIDs>
      <Operation>==</Operation>
      <InputAID>
        <NodeID>183</NodeID>
        <VariableName>entityName</VariableName>
      </InputAID>
      <InputBID>
        <NodeID>186</NodeID>
        <VariableName>Value</VariableName>
      </InputBID>
      <ValueA />
      <ValueB />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>186</ID>
      <Position>
        <X>2786</X>
        <Y>133</Y>
      </Position>
      <Value>Player_welder</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>185</NodeID>
            <VariableName>Input_B</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>148</ID>
      <Position>
        <X>3193</X>
        <Y>764</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>195</ID>
      <Position>
        <X>5498</X>
        <Y>-63</Y>
      </Position>
      <Value>1500</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>194</NodeID>
            <VariableName>disappearTimeMs</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>196</ID>
      <Position>
        <X>5481</X>
        <Y>-101</Y>
      </Position>
      <Value>Good job.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>194</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>177</ID>
      <Position>
        <X>5451</X>
        <Y>199</Y>
      </Position>
      <Value>Brokendoor</Value>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>176</ID>
      <Position>
        <X>5333</X>
        <Y>441</Y>
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
      <ID>170</ID>
      <Position>
        <X>5084</X>
        <Y>161</Y>
      </Position>
      <Value />
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>193</ID>
      <Position>
        <X>4617</X>
        <Y>-71</Y>
      </Position>
      <BoundVariableName>messageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>191</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>298</ID>
      <Position>
        <X>3309</X>
        <Y>312</Y>
      </Position>
      <VariableName>T_5true</VariableName>
      <VariableValue>False</VariableValue>
      <SequenceInputID>296</SequenceInputID>
      <SequenceOutputID>150</SequenceOutputID>
      <ValueInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>171</ID>
      <Position>
        <X>5101</X>
        <Y>-68</Y>
      </Position>
      <Value>Door</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>169</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>190</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>151</ID>
      <Position>
        <X>3705</X>
        <Y>311</Y>
      </Position>
      <VariableName>messageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>150</SequenceInputID>
      <SequenceOutputID>152</SequenceOutputID>
      <ValueInputID>
        <NodeID>150</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>156</ID>
      <Position>
        <X>3840</X>
        <Y>508</Y>
      </Position>
      <Value />
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>152</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>157</ID>
      <Position>
        <X>4092</X>
        <Y>216</Y>
      </Position>
      <Value>Welder</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>152</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>358</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>158</ID>
      <Position>
        <X>3440</X>
        <Y>572</Y>
      </Position>
      <Value>Player_welder</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>155</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>147</ID>
      <Position>
        <X>2810.19116</X>
        <Y>557.831238</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>160</ID>
      <Position>
        <X>3236</X>
        <Y>428</Y>
      </Position>
      <Value>Pick up the welder to repair the door</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>150</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>146</ID>
      <Position>
        <X>2810.19116</X>
        <Y>462.694672</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>168</ID>
      <Position>
        <X>5102</X>
        <Y>54</Y>
      </Position>
      <VariableName>messageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>167</SequenceInputID>
      <SequenceOutputID>169</SequenceOutputID>
      <ValueInputID>
        <NodeID>167</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>188</ID>
      <Position>
        <X>3772</X>
        <Y>118</Y>
      </Position>
      <BoundVariableName>messageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>187</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>173</ID>
      <Position>
        <X>4347</X>
        <Y>389</Y>
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
      <ID>181</ID>
      <Position>
        <X>4673</X>
        <Y>362</Y>
      </Position>
      <Value>Brokendoor</Value>
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
      <ID>174</ID>
      <Position>
        <X>4306</X>
        <Y>349</Y>
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
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
      <ID>296</ID>
      <Position>
        <X>2991</X>
        <Y>270</Y>
      </Position>
      <InputID>
        <NodeID>297</NodeID>
        <VariableName>Value</VariableName>
      </InputID>
      <SequenceInputID>149</SequenceInputID>
      <SequenceTrueOutputID>298</SequenceTrueOutputID>
      <SequnceFalseOutputID>-1</SequnceFalseOutputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>297</ID>
      <Position>
        <X>2813</X>
        <Y>219</Y>
      </Position>
      <BoundVariableName>T_5true</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>296</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
  </Nodes>
  <Name>Repair_script</Name>
</MyObjectBuilder_VisualScript>