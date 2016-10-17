<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <StateMachine>
    <Name>Sector_03v1</Name>
    <OwnerId>0</OwnerId>
    <Cursors>
      <MyObjectBuilder_ScriptSMCursor>
        <NodeName>Start</NodeName>
      </MyObjectBuilder_ScriptSMCursor>
    </Cursors>
    <Nodes>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="-421.978943" y="-5.21478271" />
        <Name>Start</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Start.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Start</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="679.627" y="199.014648" />
        <Name>Depart</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Depart.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Depart</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="216.732666" y="199.860733" />
        <Name>Build</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Build.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Build</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="919.1735" y="208.12294" />
        <Name>Encounter</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Encounter.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Encounter</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="589.5449" y="-187.488708" />
        <Name>Respawn</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Respawn.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Respawn</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="216.544891" y="-191.488708" />
        <Name>Ship Viability</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_ShipViability.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_ShipViability</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="213.111816" y="8.24057" />
        <Name>Questlog</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Questlog.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Questlog</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode xsi:type="MyObjectBuilder_ScriptSMSpreadNode">
        <Position x="-127.375244" y="35.53705" />
        <Name>Spread_6</Name>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode xsi:type="MyObjectBuilder_ScriptSMFinalNode">
        <Position x="1649.86414" y="212.937744" />
        <Name>Final_16</Name>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="1430.05" y="200.09964" />
        <Name>End</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_End.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_End</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="438.223267" y="200.137085" />
        <Name>Cockpit</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Cockpit.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Cockpit</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
      <MyObjectBuilder_ScriptSMNode>
        <Position x="1185.21655" y="203.567627" />
        <Name>Return</Name>
        <ScriptFilePath>VisualScripts\Missions\Sector_03v1\Objectives\Sector_03v1_Return.vs</ScriptFilePath>
        <ScriptClassName>Sector_03v1_Return</ScriptClassName>
      </MyObjectBuilder_ScriptSMNode>
    </Nodes>
    <Transitions>
      <MyObjectBuilder_ScriptSMTransition>
        <Name />
        <From>Spread_6</From>
        <To>Ship Viability</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name />
        <From>Spread_6</From>
        <To>Build</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Completed</Name>
        <From>Start</From>
        <To>Spread_6</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Respawn</Name>
        <From>Ship Viability</From>
        <To>Respawn</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Reinitialize</Name>
        <From>Respawn</From>
        <To>Ship Viability</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name />
        <From>Spread_6</From>
        <To>Questlog</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Completed</Name>
        <From>Build</From>
        <To>Cockpit</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Completed</Name>
        <From>Depart</From>
        <To>Encounter</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Completed</Name>
        <From>Encounter</From>
        <To>Return</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Completed</Name>
        <From>End</From>
        <To>Final_16</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Completed</Name>
        <From>Cockpit</From>
        <To>Depart</To>
      </MyObjectBuilder_ScriptSMTransition>
      <MyObjectBuilder_ScriptSMTransition>
        <Name>Completed</Name>
        <From>Return</From>
        <To>End</To>
      </MyObjectBuilder_ScriptSMTransition>
    </Transitions>
  </StateMachine>
</MyObjectBuilder_VSFiles>