using System;
using UnityEngine;

/// <summary>
/// 한 국가의 주요 지표를 담는 값 타입(스냅샷).
/// 상단 HUD 에 표시되는 GDP, 식자율, 평균 교육수준, 생활수준, 인구 수, 악명을 포함한다.
/// 값이 바뀔 때는 새 스냅샷을 만들어 통째로 교체한다(불변 객체처럼 사용).
/// </summary>
[Serializable]
public struct NationStats
{
    [Tooltip("국내총생산($)")]
    [SerializeField] double gdp;

    [Tooltip("식자율 (0 = 0%, 1 = 100%)")]
    [SerializeField, Range(0f, 1f)] float literacy;

    [Tooltip("평균 교육수준 (0~10 기준)")]
    [SerializeField] float averageEducation;

    [Tooltip("평균 생활수준 (0~30 기준)")]
    [SerializeField] float standardOfLiving;

    [Tooltip("총 인구 수")]
    [SerializeField] long population;

    [Tooltip("악명. 높을수록 다른 국가의 경계를 받는다.")]
    [SerializeField] float infamy;

    /// <summary>
    /// 모든 지표를 지정해 스냅샷을 만든다.
    /// </summary>
    public NationStats(double gdp, float literacy, float averageEducation, float standardOfLiving,
        long population, float infamy)
    {
        this.gdp = gdp;
        this.literacy = Mathf.Clamp01(literacy);
        this.averageEducation = averageEducation;
        this.standardOfLiving = standardOfLiving;
        this.population = population;
        this.infamy = infamy;
    }

    /// <summary>국내총생산($)</summary>
    public double Gdp => gdp;

    /// <summary>식자율 (0~1)</summary>
    public float Literacy => literacy;

    /// <summary>평균 교육수준</summary>
    public float AverageEducation => averageEducation;

    /// <summary>평균 생활수준</summary>
    public float StandardOfLiving => standardOfLiving;

    /// <summary>총 인구 수</summary>
    public long Population => population;

    /// <summary>악명</summary>
    public float Infamy => infamy;
}
