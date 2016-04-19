using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlStats : MyGuiControlBase
    {
        public class MyGuiControlStat : MyGuiControlBase
        {
			MyEntityStat m_stat;
			MyGuiControlLabel m_statNameLabel;
            MyGuiControlPanel m_progressBarBorder;
            MyGuiControlPanel m_progressBarDivider;
			MyGuiControlProgressBar m_progressBar;
			MyGuiControlPanel m_effectArrow;
			MyGuiControlLabel m_statValueLabel;
            Color m_criticalValueColorFrom;
            Color m_criticalValueColorTo;

			private static MyGuiCompositeTexture m_arrowUp = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_STAT_EFFECT_ARROW_UP.Texture);
			private static MyGuiCompositeTexture m_arrowDown = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_STAT_EFFECT_ARROW_DOWN.Texture);

			private float m_lastTotalValue = 0.0f;
            private float m_potentialChange = 0.0f;
            private float m_flashingProgress = 0.0f;
			private int m_lastFlashTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

			private bool m_recalculatePotential = false;
            public float PotentialChange
            {
                get { return m_potentialChange; }
                set 
                { 
                    m_potentialChange = value;
                    m_progressBar.PotentialBar.Visible = value != 0.0f;
                    m_recalculatePotential = value != 0.0f; 
                }
            }

            public MyGuiControlStat(MyEntityStat stat, Vector2 position, Vector2 size, MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
				: base(position: position, size: size, originAlign: originAlign)
            {
				Debug.Assert(stat != null);
				m_stat = stat;
                var vecColor = m_stat.StatDefinition.GuiDef.CriticalColorFrom;
                m_criticalValueColorFrom = new Color(vecColor.X, vecColor.Y, vecColor.Z);
                vecColor = m_stat.StatDefinition.GuiDef.CriticalColorTo;
                m_criticalValueColorTo = new Color(vecColor.X, vecColor.Y, vecColor.Z);
				if(m_stat != null)
				{
					m_stat.OnStatChanged += UpdateStatControl;
				}
            }

			public override void OnRemoving()
			{
				if(m_stat != null)
				{
                    m_stat.OnStatChanged -= UpdateStatControl;
				}
				base.OnRemoving();
			}

			public override void Update()
			{
				base.Update();

				if (m_stat != null)
				{
                    var totalValue = m_potentialChange;
					var effects = m_stat.GetEffects();
					foreach(var effect in effects)
					{
						if (effect.Value.Duration >= 0)
							totalValue += effect.Value.Amount;
					}

					if (totalValue < 0)
					{
						m_effectArrow.Visible = true;
						m_effectArrow.BackgroundTexture = m_arrowDown;
						m_progressBar.PotentialBar.Visible = false;
					}
					else if (totalValue > 0)
					{
						m_effectArrow.Visible = true;
						m_effectArrow.BackgroundTexture = m_arrowUp;

						if (m_stat.MaxValue != 0)
						{
							if (!m_progressBar.PotentialBar.Visible || m_lastTotalValue != totalValue)
							{
								m_progressBar.PotentialBar.Visible = true;
								m_recalculatePotential = true;
							}
						}
					}
					else
					{
						m_effectArrow.Visible = false;
						m_progressBar.PotentialBar.Visible = false;
					}
					m_lastTotalValue = totalValue;


                    if (m_stat.CurrentRatio <= m_stat.StatDefinition.GuiDef.CriticalRatio)
                    {
                        m_flashingProgress = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastFlashTime) * 0.001f;
                        m_progressBarBorder.Visible = true;
                        m_progressBarBorder.BorderColor = Vector4.Lerp(m_criticalValueColorFrom, m_criticalValueColorTo, m_flashingProgress);
                        if (m_flashingProgress >= 1f)
                            m_lastFlashTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    }
                    else
                    {
                        m_progressBarBorder.Visible = false;
                    }
				}

			}

			public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
			{
				if (m_recalculatePotential)
					RecalculatePotentialBar();
				base.Draw(transitionAlpha, backgroundTransitionAlpha);
			}

			private void UpdateStatControl(float newValue, float oldValue, object statChangeData)
			{
				m_progressBar.Value = m_stat.CurrentRatio;
				if (m_statValueLabel != null)	// Update the text
				{
					StringBuilder statText = new StringBuilder();
					statText.AppendDecimal((int)m_stat.Value, 0);
					statText.Append("/");
					statText.AppendDecimal(m_stat.MaxValue, 0);
					m_statValueLabel.Text = statText.ToString();
				}
				m_recalculatePotential = true;
			}

			private void RecalculateStatRegenLeft()
			{
				if (!Sync.IsServer)
					return;

				m_stat.CalculateRegenLeftForLongestEffect();
			}

			private void RecalculatePotentialBar()
			{
				if (!m_progressBar.PotentialBar.Visible)
					return;
				RecalculateStatRegenLeft();
				var pixelHorizontal = 1.01f / MyGuiManager.GetFullscreenRectangle().Height;
				var pixelVertical = 1.01f / MyGuiManager.GetFullscreenRectangle().Height;
				m_progressBar.PotentialBar.Size = new Vector2(m_progressBar.Size.X * (MathHelper.Clamp((m_stat.StatRegenLeft + m_stat.Value + m_potentialChange) / m_stat.MaxValue, 0f, 1f)) - pixelHorizontal, m_progressBar.Size.Y - 2.0f * pixelVertical);
			}

			public void RecreateControls()
			{
				Elements.Clear();

				var guiTextScale = (float)Math.Pow(1.2f, m_stat.StatDefinition.GuiDef.HeightMultiplier - 1.0f);
				var guiArrowScale = (float)Math.Pow(1.3f, m_stat.StatDefinition.GuiDef.HeightMultiplier - 1.0f) / m_stat.StatDefinition.GuiDef.HeightMultiplier;
				var textScale = MyGuiConstants.HUD_TEXT_SCALE*0.6f*guiTextScale;
				var barLength = 0.0875f;
				var arrowIconSize = new Vector2(Size.Y * 1.5f, Size.Y) * 0.5f * guiArrowScale;
				var leftTextWidth = 0.16f;
				var textHeightOffset = -0.1f;

				var leftTextOffset = -1.0f / 2.0f + leftTextWidth;
				var barOffset = leftTextOffset + 0.025f;
				var arrowIconOffset = barOffset + barLength/Size.X + 0.05f;
				var rightTextOffset = arrowIconOffset + arrowIconSize.X + 0.035f;

                var statGuiDef = m_stat.StatDefinition.GuiDef;

				m_statNameLabel = new MyGuiControlLabel(position: Size * new Vector2(leftTextOffset, textHeightOffset),
														text: m_stat.StatId.ToString(),
														textScale: textScale,
														size: new Vector2(leftTextWidth*Size.X, 1.0f),
														originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
				Elements.Add(m_statNameLabel);

				var vecColor = m_stat.StatDefinition.GuiDef.Color;
				var barColor = new Color(vecColor.X, vecColor.Y, vecColor.Z);

				m_progressBar = new MyGuiControlProgressBar(position: Size * new Vector2(barOffset, 0.0f),
															originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
															size: new Vector2(barLength, Size.Y),
															backgroundTexture: new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_STAT_BAR_BG.Texture),
															progressBarColor: barColor,
                                                            enableBorderAutohide: true);
				if (m_stat != null)
					m_progressBar.Value = m_stat.CurrentRatio;

				m_progressBar.ForegroundBar.BorderColor = Color.Black;
				m_progressBar.ForegroundBar.BorderEnabled = true;
				m_progressBar.ForegroundBar.BorderSize = 1;
				m_progressBar.PotentialBar.Position = m_progressBar.ForegroundBar.Position;
				m_recalculatePotential = true;
				Elements.Add(m_progressBar);

                m_progressBarDivider = new MyGuiControlPanel(position: Size * new Vector2(barOffset, 0.0f),
                                                            originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                                                            size: new Vector2(barLength * statGuiDef.CriticalRatio, Size.Y));
                m_progressBarDivider.Visible = statGuiDef.DisplayCriticalDivider;
                m_progressBarDivider.BorderColor = Color.Black;
                m_progressBarDivider.BorderSize = 1;
                m_progressBarDivider.BorderEnabled = true;
                Elements.Add(m_progressBarDivider);


                m_progressBarBorder = new MyGuiControlPanel(position: Size * new Vector2(barOffset, 0.0f),
                                                            originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                                                            size: new Vector2(barLength, Size.Y));
                m_progressBarBorder.Visible = false;
                m_progressBarBorder.BorderColor = Color.Black;
                m_progressBarBorder.BorderSize = 2;
                m_progressBarBorder.BorderEnabled = true;
                Elements.Add(m_progressBarBorder);

				m_effectArrow = new MyGuiControlPanel(	position: Size * new Vector2(arrowIconOffset, 0.0f),
														originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
														size: arrowIconSize,
														texture: MyGuiConstants.TEXTURE_HUD_STAT_EFFECT_ARROW_UP.Texture);
				Elements.Add(m_effectArrow);

				StringBuilder statText = new StringBuilder();
				statText.AppendDecimal((int)m_stat.Value, 0);
				statText.Append("/");
				statText.AppendDecimal(m_stat.MaxValue, 0);
				m_statValueLabel = new MyGuiControlLabel(position: Size * new Vector2(rightTextOffset, textHeightOffset),
														text: statText.ToString(),
														textScale: textScale,
														originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
				Elements.Add(m_statValueLabel);
			}
        }

        private MyCharacterStatComponent m_statComponent;
		private Dictionary<MyStringHash, MyGuiControlStat> m_statControls;

		private List<MyEntityStat> m_sortedStats = new List<MyEntityStat>();

        public MyGuiControlStats()
			: base(backgroundTexture: new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_STATS_BG.Texture))
        {
        }

        public override void Update()
        {
            base.Update();

            MyCharacterStatComponent statComponent = null;
            if (MySession.Static.LocalCharacter != null)
                statComponent = MySession.Static.LocalCharacter.StatComp;

			if (statComponent != null && statComponent != m_statComponent && statComponent.Stats.Count > 0) // statComponent can be changed during the update, however, it may not be filled up with stats yet in that time..
            {
				m_statComponent = statComponent;
				m_sortedStats.Clear();
				if(m_statComponent != null)
				{
					foreach (var stat in m_statComponent.Stats)
						m_sortedStats.Add(stat);

					m_sortedStats.Sort((leftStat, rightStat) => { return rightStat.StatDefinition.GuiDef.Priority - leftStat.StatDefinition.GuiDef.Priority; });
				}

                RecreateControls();
            }
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            if (!MyFakes.ENABLE_STATS_GUI || MySession.Static.CreativeMode || m_statControls == null || m_statControls.Count == 0) return;

            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }
		
        private void RecreateControls()
        {
			Elements.Clear();
			var stats = m_sortedStats;
			if (stats.Count == 0)
				return;

            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            if (Position == Vector2.Zero)
            {                
                var position = new Vector2(0.025f, 0.016f);
                Position = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref position);
            }

			ColorMask = new Vector4(ColorMask.X, ColorMask.Y, ColorMask.Z, 0.75f);

			float heightMultiplier = 0.0f;
			foreach(var stat in stats)
			{
				heightMultiplier += stat.StatDefinition.GuiDef.HeightMultiplier;
			}

			var verticalPadding = 0.005f;
			var statControlPadding = MyGuiConstants.TEXTURE_HUD_STATS_BG.PaddingSizeGui;
			var statControlHeight = 0.025f - 2.0f*statControlPadding.Y;
			var statControlGap = statControlHeight/4.0f;
			Size = new Vector2(0.191f, 4.0f*verticalPadding + statControlHeight*heightMultiplier + (stats.Count-1)*statControlGap );

            m_statControls = new Dictionary<MyStringHash, MyGuiControlStat>();

			var statControlWidth = Size.X - 2.0f*statControlPadding.X;
			var nextStatControlY = -Size.Y/2.0f + verticalPadding;

			foreach (var stat in stats)
			{
				var statControl = new MyGuiControlStat(	stat,
														position: new Vector2(0.0f, nextStatControlY) + statControlPadding,
														originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
														size: new Vector2(statControlWidth, stat.StatDefinition.GuiDef.HeightMultiplier * statControlHeight));
				m_statControls.Add(stat.StatId, statControl);
				Elements.Add(statControl);
				statControl.RecreateControls();
				nextStatControlY += statControl.Size.Y + statControlGap;
			}
        }

        private void SetPotentialStatChange(string id, float value)
        {
            var hashId = MyStringHash.Get(id);
            MyGuiControlStat statControl;
            if (m_statControls.TryGetValue(hashId, out statControl))
                statControl.PotentialChange = value;
        }

        public void SetPotentialStatChange(MyDefinitionId consumableId)
        {
            var definition = MyDefinitionManager.Static.GetDefinition(consumableId) as MyConsumableItemDefinition;
            // no longer relevant since we are using MyUsableItemDefinition for food too
            //Debug.Assert(definition != null, "Consumable definition not found!");
            if (definition == null)
                return;

            foreach (var statValue in definition.Stats)
            {
                SetPotentialStatChange(statValue.Name, statValue.Value * statValue.Time);
            }
        }

        public void ClearPotentialStatChange(MyDefinitionId consumableId)
        {
            var definition = MyDefinitionManager.Static.GetDefinition(consumableId) as MyConsumableItemDefinition;
            // no longer relevant since we are using MyUsableItemDefinition for food too
            //Debug.Assert(definition != null, "Consumable definition not found!");
            if (definition == null)
                return;

            foreach (var statValue in definition.Stats)
            {
                SetPotentialStatChange(statValue.Name, 0.0f);
            }
        }
    }
}
