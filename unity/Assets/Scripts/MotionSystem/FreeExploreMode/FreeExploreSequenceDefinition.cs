using System;
using UnityEngine;

[Serializable]
public class FreeExploreSequenceDefinition
{
    [Header("Identity")]
    [Tooltip("NavigationState.SelectedMotionSubUnit değerinin int karşılığı")]
    public int subUnitValue;

    [Tooltip("Inspector'da kolay ayırt etmek için isim")]
    public string displayName;

    [Header("Overview")]
    [Tooltip("Overview aşamasında açılacak bağlam objeleri")]
    public GameObject[] contextObjects;

    [Header("Focus")]
    [Tooltip("Focus aşamasında öne çıkarılacak objeler")]
    public GameObject[] focusTargets;

    [Tooltip("Focus aşamasında soluklaştırılacak objeler")]
    public GameObject[] dimTargets;

    [Header("Timing")]
    [Tooltip("0 veya daha küçükse controller üzerindeki default değer kullanılır")]
    public float overviewDurationOverride = 0f;
}