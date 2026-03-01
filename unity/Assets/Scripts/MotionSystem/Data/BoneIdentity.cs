using UnityEngine;

public class BoneIdentity : MonoBehaviour
{
    [Tooltip("CSV'deki id ile birebir aynı olmalı. Örn: os_frontale")]
    public string id;

    [Tooltip("DB'de bulunamazsa gösterilecek isim (opsiyonel)")]
    public string fallbackDisplayName;
}