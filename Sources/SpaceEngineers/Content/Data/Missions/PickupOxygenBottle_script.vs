<?xml version="1.0"?>
<MyObjectBuilder_VisualScript xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Interface>VRage.Game.VisualScripting.IMyMissionLogicScript</Interface>
  <DependencyFilePaths />
  <Nodes>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>49</ID>
      <Position>
        <X>2175</X>
        <Y>-6</Y>
      </Position>
      <VariableName>HangarGpsPosition</VariableName>
      <VariableType>VRageMath.Vector3D</VariableType>
      <VariableValue>0</VariableValue>
      <OutputNodeIds />
      <Vector>
        <X>-1109.93994140625</X>
        <Y>-1195.3499755859375</Y>
        <Z>6544.10009765625</Z>
      </Vector>
      <OutputNodeIdsX />
      <OutputNodeIdsY />
      <OutputNodeIdsZ />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>202</ID>
      <Position>
        <X>2321</X>
        <Y>-8</Y>
      </Position>
      <VariableName>Explowindow</VariableName>
      <VariableType>VRageMath.Vector3D</VariableType>
      <VariableValue>0</VariableValue>
      <OutputNodeIds />
      <Vector>
        <X>-1099.4100341796875</X>
        <Y>-1244.8699951171875</Y>
        <Z>6536.10009765625</Z>
      </Vector>
      <OutputNodeIdsX />
      <OutputNodeIdsY />
      <OutputNodeIdsZ />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableScriptNode">
      <ID>217</ID>
      <Position>
        <X>2387</X>
        <Y>196</Y>
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
      <ID>205</ID>
      <Position>
        <X>2480</X>
        <Y>196</Y>
      </Position>
      <VariableName>explotrue</VariableName>
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
      <ID>306</ID>
      <Position>
        <X>2385</X>
        <Y>341</Y>
      </Position>
      <VariableName>Dialogue</VariableName>
      <VariableType>System.Boolean</VariableType>
      <VariableValue>False</VariableValue>
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
      <ID>299</ID>
      <Position>
        <X>2564</X>
        <Y>197</Y>
      </Position>
      <VariableName>T_7trigger</VariableName>
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
      <ID>228</ID>
      <Position>
        <X>3435</X>
        <Y>54</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.PlayerPickedUp</Name>
      <SequenceOutputID>231</SequenceOutputID>
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
              <NodeID>230</NodeID>
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
      <ID>246</ID>
      <Position>
        <X>4749</X>
        <Y>-315</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>248</SequenceOutputID>
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
        <string>T_8</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_KeyEventScriptNode">
      <ID>200</ID>
      <Position>
        <X>2619</X>
        <Y>401</Y>
      </Position>
      <Name>Sandbox.Game.MyVisualScriptLogicProvider.AreaTrigger_Entered</Name>
      <SequenceOutputID>303</SequenceOutputID>
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
        <string>T_7</string>
        <string />
      </Keys>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>240</ID>
      <Position>
        <X>5144</X>
        <Y>79</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>239</SequenceInputID>
      <SequenceOutputID>242</SequenceOutputID>
      <ValueInputID>
        <NodeID>239</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>243</ID>
      <Position>
        <X>5102</X>
        <Y>206</Y>
      </Position>
      <BoundVariableName>HangarGpsPosition</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>242</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>235</ID>
      <Position>
        <X>3894</X>
        <Y>181</Y>
      </Position>
      <BoundVariableName>MessageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>233</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>241</ID>
      <Position>
        <X>4652</X>
        <Y>23</Y>
      </Position>
      <Value>Continue to the hangars</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>239</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>244</ID>
      <Position>
        <X>5168</X>
        <Y>170</Y>
      </Position>
      <Value />
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>242</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>237</ID>
      <Position>
        <X>4504</X>
        <Y>153</Y>
      </Position>
      <Value>5000</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>236</NodeID>
            <VariableName>disappearTimeMs</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>238</ID>
      <Position>
        <X>4240</X>
        <Y>22</Y>
      </Position>
      <Value>Oxygen will be replenished from bottles in your inventory.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>236</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>226</ID>
      <Position>
        <X>4942</X>
        <Y>859</Y>
      </Position>
      <Value>True</Value>
      <Type>System.Boolean</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>221</NodeID>
            <VariableName>enabled</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>232</ID>
      <Position>
        <X>3510</X>
        <Y>216</Y>
      </Position>
      <Value>Player_oxy</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>230</NodeID>
            <VariableName>Input_B</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>224</ID>
      <Position>
        <X>3944</X>
        <Y>753</Y>
      </Position>
      <Value>Player_oxy</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>223</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>317</ID>
      <Position>
        <X>3433</X>
        <Y>558</Y>
      </Position>
      <Value>5000</Value>
      <Type />
      <OutputIds>
        <Ids />
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>46</ID>
      <Position>
        <X>2470</X>
        <Y>662</Y>
      </Position>
      <MethodName>Update</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ArithmeticScriptNode">
      <ID>230</ID>
      <Position>
        <X>3720</X>
        <Y>94</Y>
      </Position>
      <OutputNodeIDs>
        <MyVariableIdentifier>
          <NodeID>231</NodeID>
          <VariableName>Comparator</VariableName>
        </MyVariableIdentifier>
      </OutputNodeIDs>
      <Operation>==</Operation>
      <InputAID>
        <NodeID>228</NodeID>
        <VariableName>entityName</VariableName>
      </InputAID>
      <InputBID>
        <NodeID>232</NodeID>
        <VariableName>Value</VariableName>
      </InputBID>
      <ValueA />
      <ValueB />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>318</ID>
      <Position>
        <X>3432</X>
        <Y>595</Y>
      </Position>
      <Value>False</Value>
      <Type />
      <OutputIds>
        <Ids />
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>245</ID>
      <Position>
        <X>5208</X>
        <Y>-103</Y>
      </Position>
      <Value>Hangar airlock</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>242</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>247</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
      <ID>231</ID>
      <Position>
        <X>3915</X>
        <Y>55</Y>
      </Position>
      <InputID>
        <NodeID>230</NodeID>
        <VariableName>Output</VariableName>
      </InputID>
      <SequenceInputID>228</SequenceInputID>
      <SequenceTrueOutputID>233</SequenceTrueOutputID>
      <SequnceFalseOutputID>-1</SequnceFalseOutputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>201</ID>
      <Position>
        <X>3234</X>
        <Y>563</Y>
      </Position>
      <Value>4</Value>
      <Type>System.Single</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>199</NodeID>
            <VariableName>radius</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>47</ID>
      <Position>
        <X>2470</X>
        <Y>742</Y>
      </Position>
      <MethodName>Dispose</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>225</ID>
      <Position>
        <X>4886</X>
        <Y>518</Y>
      </Position>
      <Value>Player_oxy</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>221</NodeID>
            <VariableName>entityName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>216</ID>
      <Position>
        <X>3707</X>
        <Y>615</Y>
      </Position>
      <Value>Go outside and find a bottle of oxygen.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>214</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>211</ID>
      <Position>
        <X>3561</X>
        <Y>794</Y>
      </Position>
      <Value>8000</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>209</NodeID>
            <VariableName>disappearTimeMs</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>213</ID>
      <Position>
        <X>3058</X>
        <Y>754</Y>
      </Position>
      <Value>An explosion ocured and the room is decompressed.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>207</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>210</ID>
      <Position>
        <X>3307</X>
        <Y>804</Y>
      </Position>
      <Value>4000</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>207</NodeID>
            <VariableName>disappearTimeMs</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>220</ID>
      <Position>
        <X>4218</X>
        <Y>307</Y>
      </Position>
      <Value>Oxygen bottle</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>227</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
          <MyVariableIdentifier>
            <NodeID>218</NodeID>
            <VariableName>name</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>250</ID>
      <Position>
        <X>4942</X>
        <Y>-237</Y>
      </Position>
      <BoundVariableName>MessageID</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>248</NodeID>
            <VariableName>messageId</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>203</ID>
      <Position>
        <X>3205</X>
        <Y>522</Y>
      </Position>
      <BoundVariableName>Explowindow</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>199</NodeID>
            <VariableName>position</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>212</ID>
      <Position>
        <X>3393</X>
        <Y>755</Y>
      </Position>
      <Value>your oxygen suply is depleting slowly.</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>209</NodeID>
            <VariableName>message</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>215</ID>
      <Position>
        <X>4175</X>
        <Y>444</Y>
      </Position>
      <VariableName>MessageID</VariableName>
      <VariableValue>0</VariableValue>
      <SequenceInputID>214</SequenceInputID>
      <SequenceOutputID>218</SequenceOutputID>
      <ValueInputID>
        <NodeID>214</NodeID>
        <VariableName>Return</VariableName>
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>301</ID>
      <Position>
        <X>3234</X>
        <Y>611</Y>
      </Position>
      <Value>200000</Value>
      <Type>System.Int32</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>199</NodeID>
            <VariableName>damage</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_VariableSetterScriptNode">
      <ID>302</ID>
      <Position>
        <X>3029</X>
        <Y>441</Y>
      </Position>
      <VariableName>T_7trigger</VariableName>
      <VariableValue>False</VariableValue>
      <SequenceInputID>303</SequenceInputID>
      <SequenceOutputID>540</SequenceOutputID>
      <ValueInputID>
        <NodeID>-1</NodeID>
        <VariableName />
      </ValueInputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_BranchingScriptNode">
      <ID>303</ID>
      <Position>
        <X>2845</X>
        <Y>423</Y>
      </Position>
      <InputID>
        <NodeID>305</NodeID>
        <VariableName>Value</VariableName>
      </InputID>
      <SequenceInputID>200</SequenceInputID>
      <SequenceTrueOutputID>302</SequenceTrueOutputID>
      <SequnceFalseOutputID>-1</SequnceFalseOutputID>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_GetterScriptNode">
      <ID>305</ID>
      <Position>
        <X>2669</X>
        <Y>572</Y>
      </Position>
      <BoundVariableName>T_7trigger</BoundVariableName>
      <OutputIDs>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>303</NodeID>
            <VariableName>Comparator</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIDs>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>219</ID>
      <Position>
        <X>4824</X>
        <Y>459</Y>
      </Position>
      <Value />
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>218</NodeID>
            <VariableName>description</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_InterfaceMethodNode">
      <ID>45</ID>
      <Position>
        <X>2470</X>
        <Y>582</Y>
      </Position>
      <MethodName>Init</MethodName>
      <SequenceOutputIDs />
      <OutputIDs />
      <OutputNames />
      <OuputTypes />
    </MyObjectBuilder_ScriptNode>
    <MyObjectBuilder_ScriptNode xsi:type="MyObjectBuilder_ConstantScriptNode">
      <ID>541</ID>
      <Position>
        <X>3026</X>
        <Y>527</Y>
      </Position>
      <Value>Timer_ship</Value>
      <Type>System.String</Type>
      <OutputIds>
        <Ids>
          <MyVariableIdentifier>
            <NodeID>540</NodeID>
            <VariableName>blockName</VariableName>
          </MyVariableIdentifier>
        </Ids>
      </OutputIds>
    </MyObjectBuilder_ScriptNode>
  </Nodes>
  <Name>PickupOxygenBottle_script</Name>
</MyObjectBuilder_VisualScript>