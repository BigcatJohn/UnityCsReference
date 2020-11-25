// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class UIDetailsProfilerModule : UIProfilerModule
    {
        const string k_IconName = "Profiler.UIDetails";
        const int k_DefaultOrderIndex = 11;
        static readonly string k_UnLocalizedName = "UI Details";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);

        public UIDetailsProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_UnLocalizedName, k_Name, k_IconName, Chart.ChartType.Line) {}

        public override ProfilerArea area => ProfilerArea.UIDetails;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartUIDetails";

        UISystemProfilerChart UISystemProfilerChart => m_Chart as UISystemProfilerChart;

        protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            // [Coverity Defect 53724] Intentionally not calling the base class here to instantiate a custom chart type.
            m_Chart = new UISystemProfilerChart(m_ChartType, defaultChartScale, chartMaximumScaleInterpolationValue, m_ChartCounters.Count, k_Name, name, m_IconName);
            return m_Chart;
        }
    }
}
