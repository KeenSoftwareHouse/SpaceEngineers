using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VRage.Audio;
using VRage.Collections;
using VRage.Utils;
using VRage.Data.Audio;
using VRageMath;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Audio")]
    class MyGuiScreenDebugAudio : MyGuiScreenDebugBase
    {
        private const string ALL_CATEGORIES = "_ALL_CATEGORIES_";
        MyGuiControlCombobox m_categoriesCombo;
        MyGuiControlCombobox m_cuesCombo;
        static string m_currentCategorySelectedItem;
        static int m_currentCueSelectedItem = 0;
        bool m_canUpdateValues = true;
        IMySourceVoice m_sound = null;

        MySoundData m_currentCue;
        MyGuiControlSlider m_cueVolumeSlider;
        MyGuiControlCombobox m_cueVolumeCurveCombo;
        MyGuiControlSlider m_cueMaxDistanceSlider;
        MyGuiControlSlider m_cueVolumeVariationSlider;
        MyGuiControlSlider m_cuePitchVariationSlider;
        MyGuiControlCheckbox m_soloCheckbox;
        MyGuiControlButton m_applyVolumeToCategory;
        MyGuiControlButton m_applyMaxDistanceToCategory;
        private MyGuiControlCombobox m_effects;
        private List<MyGuiControlCombobox> m_cues = new List<MyGuiControlCombobox>();

        public MyGuiScreenDebugAudio()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;

            
            m_scale = 0.7f;

            AddCaption("Audio FX", Color.Yellow.ToVector4());
            AddShareFocusHint();

            if (MyAudio.Static is MyNullAudio)
                return;

            m_categoriesCombo = AddCombo();
            List<MyStringId> categories = MyAudio.Static.GetCategories(); 
            m_categoriesCombo.AddItem(0, new StringBuilder(ALL_CATEGORIES));
            int catCount = 1;
            foreach (var category in categories)
            {
                m_categoriesCombo.AddItem(catCount++, new StringBuilder(category.ToString()));//jn:TODO get rid of ToString
            }

            m_categoriesCombo.SortItemsByValueText();
            m_categoriesCombo.ItemSelected += new MyGuiControlCombobox.ItemSelectedDelegate(categoriesCombo_OnSelect);

            m_cuesCombo = AddCombo();

            m_cuesCombo.ItemSelected += new MyGuiControlCombobox.ItemSelectedDelegate(cuesCombo_OnSelect);

            m_cueVolumeSlider = AddSlider("Volume", 1f, 0f, 1f, null);
            m_cueVolumeSlider.ValueChanged = CueVolumeChanged;

            m_applyVolumeToCategory = AddButton(new StringBuilder("Apply to category"), OnApplyVolumeToCategorySelected);
            m_applyVolumeToCategory.Enabled = false;

            m_cueVolumeCurveCombo = AddCombo();
            foreach (var curveType in Enum.GetValues(typeof(MyCurveType)))
            {
                m_cueVolumeCurveCombo.AddItem((int)curveType, new StringBuilder(curveType.ToString()));
            }

            m_effects = AddCombo();
            m_effects.AddItem(0,new StringBuilder(""));
            catCount =1;
            foreach(var effect in MyDefinitionManager.Static.GetAudioEffectDefinitions())
            {
                m_effects.AddItem(catCount++, new StringBuilder(effect.Id.SubtypeName));
            }
            m_effects.SelectItemByIndex(0);
            m_effects.ItemSelected += effects_ItemSelected;

            m_cueMaxDistanceSlider = AddSlider("Max distance", 0, 0, 2000, null);
            m_cueMaxDistanceSlider.ValueChanged = MaxDistanceChanged;

            m_applyMaxDistanceToCategory = AddButton(new StringBuilder("Apply to category"), OnApplyMaxDistanceToCategorySelected);
            m_applyMaxDistanceToCategory.Enabled = false;

            m_cueVolumeVariationSlider = AddSlider("Volume variation", 0, 0, 10, null);
            m_cueVolumeVariationSlider.ValueChanged = VolumeVariationChanged;
            m_cuePitchVariationSlider = AddSlider("Pitch variation", 0, 0, 500, null);
            m_cuePitchVariationSlider.ValueChanged = PitchVariationChanged;

            m_soloCheckbox = AddCheckBox("Solo", false, null);
            m_soloCheckbox.IsCheckedChanged = SoloChanged;

            MyGuiControlButton btn = AddButton(new StringBuilder("Play selected"), OnPlaySelected);
            btn.CueEnum = GuiSounds.None;

            AddButton(new StringBuilder("Stop selected"), OnStopSelected);
            AddButton(new StringBuilder("Save"), OnSave);
            AddButton(new StringBuilder("Reload"), OnReload);

            if (m_categoriesCombo.GetItemsCount() > 0)
                m_categoriesCombo.SelectItemByIndex(0);
        }

        void effects_ItemSelected()
        {
            foreach(var box in m_cues)
            {
                Controls.Remove(box);
            }
            m_cues.Clear();
            var eff = MyStringHash.TryGet(m_effects.GetSelectedValue().ToString());
            MyAudioEffectDefinition def;
            if(MyDefinitionManager.Static.TryGetDefinition<MyAudioEffectDefinition>(new MyDefinitionId(typeof(MyObjectBuilder_AudioEffectDefinition), eff), out def))
            {
                for(int i = 0; i < def.Effect.SoundsEffects.Count -1; i++)
                {
                    var combo = AddCombo();
                    UpdateCuesCombo(combo);
                    m_cues.Add(combo);
                }
            }
        }

        void categoriesCombo_OnSelect()
        {
            m_currentCategorySelectedItem = m_categoriesCombo.GetSelectedValue().ToString();
            m_applyVolumeToCategory.Enabled = (m_currentCategorySelectedItem != ALL_CATEGORIES);
            m_applyMaxDistanceToCategory.Enabled = (m_currentCategorySelectedItem != ALL_CATEGORIES);
            UpdateCuesCombo(m_cuesCombo);
            foreach (var box in m_cues)
                UpdateCuesCombo(box);
        }

        void UpdateCuesCombo(MyGuiControlCombobox box)
        {

            box.ClearItems();
            long key = 0;
            foreach (var cue in MyAudio.Static.CueDefinitions)
            {
                if ((m_currentCategorySelectedItem == ALL_CATEGORIES) || (m_currentCategorySelectedItem == cue.Category.ToString()))
                {
                    box.AddItem(key, new StringBuilder(cue.SubtypeId.ToString()));
                    key++;
                }
            }

            box.SortItemsByValueText();
            if (box.GetItemsCount() > 0)
                box.SelectItemByIndex(0);
        }

        void cuesCombo_OnSelect()
        {
            m_currentCueSelectedItem = (int)m_cuesCombo.GetSelectedKey();
            var cue = new MyCueId(MyStringHash.TryGet(m_cuesCombo.GetSelectedValue().ToString()));
            m_currentCue = MyAudio.Static.GetCue(cue);
           
            UpdateCueValues();
            //RecreateControls(false);
        }

        void UpdateCueValues()
        {
            m_canUpdateValues = false;

            m_cueVolumeSlider.Value = m_currentCue.Volume;
            m_cueVolumeCurveCombo.SelectItemByKey((int)m_currentCue.VolumeCurve);
            m_cueMaxDistanceSlider.Value = m_currentCue.MaxDistance;
            m_cueVolumeVariationSlider.Value = m_currentCue.VolumeVariation;
            m_cuePitchVariationSlider.Value = m_currentCue.PitchVariation;
            m_soloCheckbox.IsChecked = m_currentCue == MyAudio.Static.SoloCue;

            m_canUpdateValues = true;
        }

        void CueVolumeChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                m_currentCue.Volume = slider.Value;
            }
        }

        void CueVolumeCurveChanged(MyGuiControlCombobox combobox)
        {
            if (m_canUpdateValues)
            {
                m_currentCue.VolumeCurve = (MyCurveType)combobox.GetSelectedKey();
            }
        }

        void MaxDistanceChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                m_currentCue.MaxDistance = slider.Value;
            }
        }

        void VolumeVariationChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                m_currentCue.VolumeVariation = slider.Value;
            }
        }

        void PitchVariationChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                m_currentCue.PitchVariation = slider.Value;
            }
        }

        void SoloChanged(MyGuiControlCheckbox checkbox)
        {
            if (m_canUpdateValues)
            {
                if (checkbox.IsChecked)
                    MyAudio.Static.SoloCue = m_currentCue;
                else
                    MyAudio.Static.SoloCue = null;
            }
        }

        void OnApplyVolumeToCategorySelected(MyGuiControlButton button)
        {
            m_canUpdateValues = false;
            foreach (var soundCue in MyAudio.Static.CueDefinitions)
                if (m_currentCategorySelectedItem == soundCue.Category.ToString())
                    soundCue.Volume = m_cueVolumeSlider.Value;
            m_canUpdateValues = true;
        }

        void OnApplyMaxDistanceToCategorySelected(MyGuiControlButton button)
        {
            m_canUpdateValues = false;
            foreach (var soundCue in MyAudio.Static.CueDefinitions)
                if (m_currentCategorySelectedItem == soundCue.Category.ToString())
                        soundCue.MaxDistance = m_cueMaxDistanceSlider.Value;
            m_canUpdateValues = true;
        }

        List<MyCueId> m_cueCache = new List<MyCueId>();
        void OnPlaySelected(MyGuiControlButton button)
        {
            if ((m_sound != null) && (m_sound.IsPlaying))
                m_sound.Stop(true);
            var cue = new MyCueId(MyStringHash.TryGet(m_cuesCombo.GetSelectedValue().ToString()));
            m_sound = MyAudio.Static.PlaySound(cue);
            var effect = MyStringHash.TryGet(m_effects.GetSelectedValue().ToString());
            if(effect != MyStringHash.NullOrEmpty)
            {
                foreach(var box in m_cues)
                {
                    var effCue = new MyCueId(MyStringHash.TryGet(box.GetSelectedValue().ToString()));
                    m_cueCache.Add(effCue);
                }
                var eff = MyAudio.Static.ApplyEffect(m_sound, effect, m_cueCache.ToArray());
                m_sound = eff.OutputSound;
                m_cueCache.Clear();
            }
        }

        void OnStopSelected(MyGuiControlButton button)
        {
            if ((m_sound != null) && (m_sound.IsPlaying))
                m_sound.Stop(true);
        }

        void OnSave(MyGuiControlButton button)
        {
            var ob = new MyObjectBuilder_Definitions();

            var sounds = MyDefinitionManager.Static.GetSoundDefinitions();
            ob.Sounds = new MyObjectBuilder_AudioDefinition[sounds.Count];
            int i = 0;
            foreach (var sound in sounds)
            {
                ob.Sounds[i++] = (MyObjectBuilder_AudioDefinition)sound.GetObjectBuilder();
            }

            var path = Path.Combine(MyFileSystem.ContentPath, @"Data\Audio.sbc");
            MyObjectBuilderSerializer.SerializeXML(path, false, ob);
        }

        void OnReload(MyGuiControlButton button)
        {
            MyAudio.Static.UnloadData();
            MyDefinitionManager.Static.PreloadDefinitions();
            MyAudio.Static.ReloadData(MyAudioExtensions.GetSoundDataFromDefinitions(), MyAudioExtensions.GetEffectData());
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugAudio";
        }
    }
}
