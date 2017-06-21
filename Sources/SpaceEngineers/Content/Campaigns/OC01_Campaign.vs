<?xml version="1.0"?>
<MyObjectBuilder_VSFiles xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Campaign>
    <StateMachine>
      <Nodes>
        <MyObjectBuilder_CampaignSMNode>
          <Name>Mission_05</Name>
          <SaveFilePath>Campaigns\Official Campaign 01\Worlds\Mission 05\Sandbox.sbc</SaveFilePath>
          <Location x="237.241608" y="844.5343" />
        </MyObjectBuilder_CampaignSMNode>
        <MyObjectBuilder_CampaignSMNode>
          <Name>Mission_04</Name>
          <SaveFilePath>Campaigns\Official Campaign 01\Worlds\Mission 04\Sandbox.sbc</SaveFilePath>
          <Location x="219.399841" y="654.202332" />
        </MyObjectBuilder_CampaignSMNode>
        <MyObjectBuilder_CampaignSMNode>
          <Name>Mission_02</Name>
          <SaveFilePath>Campaigns\Official Campaign 01\Worlds\Mission 02\Sandbox.sbc</SaveFilePath>
          <Location x="210" y="288" />
        </MyObjectBuilder_CampaignSMNode>
        <MyObjectBuilder_CampaignSMNode>
          <Name>Mission_01</Name>
          <SaveFilePath>Campaigns\Official Campaign 01\Worlds\Mission 01\Sandbox.sbc</SaveFilePath>
          <Location x="130" y="94" />
        </MyObjectBuilder_CampaignSMNode>
        <MyObjectBuilder_CampaignSMNode>
          <Name>Mission_03</Name>
          <SaveFilePath>Campaigns\Official Campaign 01\Worlds\Mission 03\Sandbox.sbc</SaveFilePath>
          <Location x="215.157166" y="467.526154" />
        </MyObjectBuilder_CampaignSMNode>
      </Nodes>
      <Transitions>
        <MyObjectBuilder_CampaignSMTransition>
          <Name>LevelCompleted</Name>
          <From>Mission_04</From>
          <To>Mission_05</To>
        </MyObjectBuilder_CampaignSMTransition>
        <MyObjectBuilder_CampaignSMTransition>
          <Name>LevelCompleted</Name>
          <From>Mission_03</From>
          <To>Mission_04</To>
        </MyObjectBuilder_CampaignSMTransition>
        <MyObjectBuilder_CampaignSMTransition>
          <Name>LevelCompleted</Name>
          <From>Mission_01</From>
          <To>Mission_02</To>
        </MyObjectBuilder_CampaignSMTransition>
        <MyObjectBuilder_CampaignSMTransition>
          <Name>LevelCompleted</Name>
          <From>Mission_02</From>
          <To>Mission_03</To>
        </MyObjectBuilder_CampaignSMTransition>
      </Transitions>
    </StateMachine>
    <LocalizationPaths>
      <Path>Campaigns\Official Campaign 01\Localization</Path>
    </LocalizationPaths>
    <LocalizationLanguages>
      <Language>English</Language>
    </LocalizationLanguages>
    <Name>The First Jump</Name>
    <Description>Mission Brief: Investigate potential conflict, orbital station belonging to the corporation Results Oriented Sciences Division-4 is reportedly coming under attack by unknown raiders.


R.O.S.4 is a deep science focused company seeking to expand human knowledge by exploring space. It is a privately owned corporation run by an enigmatic trillionaire CEO named Marcus.</Description>
    <ImagePath>Campaigns\Official Campaign 01\Screens\Campaign.png</ImagePath>
    <IsMultiplayer>false</IsMultiplayer>
    <Difficulty>Easy</Difficulty>
  </Campaign>
</MyObjectBuilder_VSFiles>