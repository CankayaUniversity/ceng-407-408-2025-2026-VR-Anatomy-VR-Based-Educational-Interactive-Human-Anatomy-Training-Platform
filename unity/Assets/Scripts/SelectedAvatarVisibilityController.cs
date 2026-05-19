using UnityEngine;

/// <summary>
/// PlayerPrefs / SettingsManager'dan okunan avatar tipine göre
/// sahnede hazır bulunan 4 avatar objesinin görünürlüğünü kontrol eder.
///
/// Tipler (SettingsManager.AvatarType):
///   Female      = 0  →  femaleAvatarObject
///   Male        = 1  →  maleAvatarObject
///   YoungFemale = 2  →  youngFemaleAvatarObject  (model3.glb)
///   YoungMale   = 3  →  youngMaleAvatarObject    (model4.glb)
///
/// Kullanım:
///   - Sahnede boş bir GameObject oluştur.
///   - Bu scripti o objeye ekle.
///   - Inspector'dan 4 avatar objesini ilgili alanlara ata.
///   - Atanmayan alanlar için script sahnede ada göre otomatik arama yapar.
/// </summary>
public class SelectedAvatarVisibilityController : MonoBehaviour
{
    private const string AvatarTypeKey = "AvatarType"; // SettingsManager ile aynı key

    // Otomatik arama için varsayılan obje adları
    private const string DefaultFemaleAvatarName      = "FemaleStudent";
    private const string DefaultMaleAvatarName        = "MaleStudent";
    private const string DefaultYoungFemaleAvatarName = "model3";
    private const string DefaultYoungMaleAvatarName   = "model4";

    [Header("Avatar Objeleri (Inspector'dan Ata — boş bırakılırsa otomatik aranır)")]
    [Tooltip("Kız avatar objesi (AvatarType = Female = 0)")]
    public GameObject femaleAvatarObject;

    [Tooltip("Erkek avatar objesi (AvatarType = Male = 1)")]
    public GameObject maleAvatarObject;

    [Tooltip("Genç Kız avatar objesi (AvatarType = YoungFemale = 2) → model3.glb")]
    public GameObject youngFemaleAvatarObject;

    [Tooltip("Genç Erkek avatar objesi (AvatarType = YoungMale = 3) → model4.glb")]
    public GameObject youngMaleAvatarObject;

    private void Awake()
    {
        // Inspector'da atanmamış referansları sahnede ada göre otomatik bul
        ResolveReference(ref femaleAvatarObject,      DefaultFemaleAvatarName);
        ResolveReference(ref maleAvatarObject,        DefaultMaleAvatarName);
        ResolveReference(ref youngFemaleAvatarObject, DefaultYoungFemaleAvatarName);
        ResolveReference(ref youngMaleAvatarObject,   DefaultYoungMaleAvatarName);
    }

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged += OnAvatarTypeChanged;

        ApplyAvatarVisibility();
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged -= OnAvatarTypeChanged;
    }

    private void OnAvatarTypeChanged(SettingsManager.AvatarType _)
    {
        ApplyAvatarVisibility();
    }

    private void ApplyAvatarVisibility()
    {
        SettingsManager.AvatarType selectedType = GetSelectedAvatarType();

        Debug.Log($"[SelectedAvatarVisibilityController] Seçili avatar: {selectedType} ({(int)selectedType})");

        SetActive(femaleAvatarObject,      selectedType == SettingsManager.AvatarType.Female);
        SetActive(maleAvatarObject,        selectedType == SettingsManager.AvatarType.Male);
        SetActive(youngFemaleAvatarObject, selectedType == SettingsManager.AvatarType.YoungFemale);
        SetActive(youngMaleAvatarObject,   selectedType == SettingsManager.AvatarType.YoungMale);
    }

    private static SettingsManager.AvatarType GetSelectedAvatarType()
    {
        if (SettingsManager.Instance != null)
            return SettingsManager.Instance.SelectedAvatarType;

        int savedValue = PlayerPrefs.GetInt(AvatarTypeKey, (int)SettingsManager.AvatarType.Female);
        savedValue = Mathf.Clamp(savedValue, 0, 3);
        return (SettingsManager.AvatarType)savedValue;
    }

    private static void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
        // Null ise sessizce geç — atanmamış avatar için uyarı Awake'de verildi
    }

    /// <summary>
    /// Referans null ise sahnede (pasif objeler dahil) ada göre arar.
    /// </summary>
    private void ResolveReference(ref GameObject target, string searchName)
    {
        if (target != null) return;

        // Aktif objede dene
        target = GameObject.Find(searchName);
        if (target != null)
        {
            Debug.Log($"[SelectedAvatarVisibilityController] '{searchName}' otomatik bulundu (aktif).", this);
            return;
        }

        // Pasif objeler dahil ara
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.hideFlags != HideFlags.None) continue;
            if (!go.scene.isLoaded) continue;
            if (go.name != searchName) continue;
            target = go;
            Debug.Log($"[SelectedAvatarVisibilityController] '{searchName}' otomatik bulundu (pasif).", this);
            return;
        }

        Debug.LogWarning(
            $"[SelectedAvatarVisibilityController] '{searchName}' sahnede bulunamadı. " +
            $"Inspector'dan manuel atayın veya obje adını kontrol edin.", this);
    }
}
