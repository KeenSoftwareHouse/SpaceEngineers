using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Entity;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1_TMP

    [MyDebugScreen("Render", "Environment Atmosphere")]
    public class MyGuiScreenDebugRenderEnvironmentAtmosphere : MyGuiScreenDebugBase
    {
        static long m_selectedPlanetEntityID;
        static MyAtmosphereSettings m_originalAtmosphereSettings;
        static MyAtmosphereSettings m_atmosphereSettings;
        static bool m_atmosphereEnabled = true;

        static MyPlanet SelectedPlanet
        {
            get
            {
                MyEntity planet;
                if (MyEntities.TryGetEntityById(m_selectedPlanetEntityID, out planet))
                {
                    return planet as MyPlanet;
                }
                return null;
            }
        }

        public MyGuiScreenDebugRenderEnvironmentAtmosphere()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Atmosphere", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;

            if (MySession.Static.GetComponent<MySectorWeatherComponent>() != null)
            {
                AddCheckBox("Enable Sun Rotation",
                    () => MySession.Static.GetComponent<MySectorWeatherComponent>().Enabled,
                    x => MySession.Static.GetComponent<MySectorWeatherComponent>().Enabled = x
                );

                AddSlider("Time of day", 0.0f, MySession.Static == null ? 1.0f : MySession.Static.Settings.SunRotationIntervalMinutes,
                    () => MyTimeOfDayHelper.TimeOfDay,
                    MyTimeOfDayHelper.UpdateTimeOfDay
                );

                AddSlider("Sun Speed", 0.5f, 60,
                    () => MySession.Static.GetComponent<MySectorWeatherComponent>().RotationInterval,
                    f => MySession.Static.GetComponent<MySectorWeatherComponent>().RotationInterval = f
                );
            }

            AddCheckBox("Enable atmosphere",
                () => m_atmosphereEnabled,
                (bool b) =>
                {
                    EnableAtmosphere(b);
                }
            );

            AddButton(new StringBuilder("Pick planet"), OnPickPlanet);

            if (SelectedPlanet != null)
            {
                if (m_atmosphereSettings.MieColorScattering.X == 0.0f)
                {
                    m_atmosphereSettings.MieColorScattering = new Vector3(m_atmosphereSettings.MieScattering);
                }
                if (m_atmosphereSettings.Intensity == 0.0f)
                {
                    m_atmosphereSettings.Intensity = 1.0f;
                }

                AddLabel("Atmosphere Settings", Color.White, 1f);
                AddSlider("Rayleigh Scattering R", 1f, 100f,
                    () => m_atmosphereSettings.RayleighScattering.X,
                    (float f) =>
                    {
                        m_atmosphereSettings.RayleighScattering.X = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Rayleigh Scattering G", 1f, 100f,
                    () => m_atmosphereSettings.RayleighScattering.Y,
                    (float f) =>
                    {
                        m_atmosphereSettings.RayleighScattering.Y = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Rayleigh Scattering B", 1f, 100f,
                    () => m_atmosphereSettings.RayleighScattering.Z,
                    (float f) =>
                    {
                        m_atmosphereSettings.RayleighScattering.Z = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Mie Scattering R", 5f, 150f,
                    () => m_atmosphereSettings.MieColorScattering.X,
                    (float f) =>
                    {
                        m_atmosphereSettings.MieColorScattering.X = f;
                        UpdateAtmosphere();
                    }
                );

                AddSlider("Mie Scattering G", 5f, 150f,
                    () => m_atmosphereSettings.MieColorScattering.Y,
                    (float f) =>
                    {
                        m_atmosphereSettings.MieColorScattering.Y = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Mie Scattering B", 5f, 150f,
                    () => m_atmosphereSettings.MieColorScattering.Z,
                    (float f) =>
                    {
                        m_atmosphereSettings.MieColorScattering.Z = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Rayleigh Height Surfrace", 1f, 50f,
                    () => m_atmosphereSettings.RayleighHeight,
                    (float f) =>
                    {
                        m_atmosphereSettings.RayleighHeight = f;
                        UpdateAtmosphere();
                    }
                );

                AddSlider("Rayleigh Height Space", 1f, 25f,
                    () => m_atmosphereSettings.RayleighHeightSpace,
                    (float f) =>
                    {
                        m_atmosphereSettings.RayleighHeightSpace = f;
                        UpdateAtmosphere();
                    }
                );

                AddSlider("Rayleigh Transition", 0.1f, 1.5f,
                    () => m_atmosphereSettings.RayleighTransitionModifier,
                    (float f) =>
                    {
                        m_atmosphereSettings.RayleighTransitionModifier = f;
                        UpdateAtmosphere();
                    }
                );

                AddSlider("Mie Height", 5f, 200f,
                    () => m_atmosphereSettings.MieHeight,
                    (float f) =>
                    {
                        m_atmosphereSettings.MieHeight = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Sun size", 0.99f, 1.0f,
                    () => m_atmosphereSettings.MieG,
                    (float f) =>
                    {
                        m_atmosphereSettings.MieG = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Sea floor modifier", 0.9f, 1.1f,
                    () => m_atmosphereSettings.SeaLevelModifier,
                    (float f) =>
                    {
                        m_atmosphereSettings.SeaLevelModifier = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Atmosphere top modifier", 0.9f, 1.1f,
                    () => m_atmosphereSettings.AtmosphereTopModifier,
                    (float f) =>
                    {
                        m_atmosphereSettings.AtmosphereTopModifier = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Intensity", 0.1f, 50f,
                    () => m_atmosphereSettings.Intensity,
                    (float f) =>
                    {
                        m_atmosphereSettings.Intensity = f;
                        UpdateAtmosphere();
                    }
                );
                AddSlider("Fog Intensity", 0.0f, 1.0f,
                    () => m_atmosphereSettings.FogIntensity,
                    (float f) =>
                    {
                        m_atmosphereSettings.FogIntensity = f;
                        UpdateAtmosphere();
                    }
                );

                AddButton(new StringBuilder("Restore"), OnRestoreButtonClicked);
                AddButton(new StringBuilder("Earth settings"), OnResetButtonClicked);
            }
        }

        void OnRestoreButtonClicked(MyGuiControlButton button)
        {
            m_atmosphereSettings = m_originalAtmosphereSettings;
            RecreateControls(false);
            UpdateAtmosphere();
        }

        void OnResetButtonClicked(MyGuiControlButton button)
        {
            m_atmosphereSettings = MyAtmosphereSettings.Defaults();
            RecreateControls(false);
            UpdateAtmosphere();
        }

        void OnPickPlanet(MyGuiControlButton button)
        {
            var results = new List<MyLineSegmentOverlapResult<MyEntity>>();
            var ray = new LineD(MySector.MainCamera.Position, MySector.MainCamera.ForwardVector);
            MyGamePruningStructure.GetAllEntitiesInRay(ref ray, results);

            float closestPlanetDistance = float.MaxValue;
            MyPlanet closestPlanet = null;

            foreach (var result in results)
            {
                var planet = result.Element as MyPlanet;
                if (planet != null && planet.EntityId != m_selectedPlanetEntityID)
                {
                    if (result.Distance < closestPlanetDistance)
                    {
                        closestPlanet = planet;
                    }
                }
            }

            if (closestPlanet != null)
            {
                m_selectedPlanetEntityID = closestPlanet.EntityId;
                m_atmosphereSettings = closestPlanet.AtmosphereSettings;
                m_originalAtmosphereSettings = m_atmosphereSettings;
                RecreateControls(false);
            }
        }

        private void UpdateAtmosphere()
        {
            if (SelectedPlanet != null)
            {
                SelectedPlanet.AtmosphereSettings = m_atmosphereSettings;
            }
        }

        private void EnableAtmosphere(bool enabled)
        {
            m_atmosphereEnabled = enabled;
            MyRenderProxy.EnableAtmosphere(m_atmosphereEnabled);
        }
    }

#endif
}
