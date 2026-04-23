using UnityEngine;
using UnityEngine.Serialization;

public class VeinIdentity : MonoBehaviour
{
    [FormerlySerializedAs("title")]
    [Tooltip("CSV'deki id ile birebir aynı olmalı. Örn: os_frontale")]
    public string id;

    [Tooltip("DB'de bulunamazsa gösterilecek isim (opsiyonel)")]
    public string fallbackDisplayName;
}