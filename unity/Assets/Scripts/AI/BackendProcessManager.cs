using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

/// <summary>
/// Unity başladığında Python backend sunucusunu otomatik başlatır,
/// kapandığında otomatik kapatır. Kullanıcı hiçbir teknik işlem yapmaz.
///
/// Kullanım: Boş bir GameObject'e ekle, sahnenin en başında yüklenen
/// sahneye koy. Inspector'dan ayar değiştirmeye gerek yok.
/// </summary>
public class BackendProcessManager : MonoBehaviour
{
    [Header("Sunucu Ayarları")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 8001;

    [Header("Yol Ayarları (boş bırakırsan otomatik bulur)")]
    [Tooltip("Boş bırak = otomatik algıla")]
    [SerializeField] private string customBackendPath = "";

    [Header("Başlatma Ayarları")]
    [SerializeField] private float healthCheckInterval = 1.5f;
    [SerializeField] private int maxRetries = 20;
    [SerializeField] private bool autoRestart = true;
    [SerializeField] private float backgroundCheckInterval = 10f;

    private Process _backendProcess;
    private bool _isReady;
    private bool _weStartedIt; // Biz mi başlattık, yoksa zaten çalışıyor muydu?
    private static BackendProcessManager _instance;
    private Coroutine _backgroundHealthCheck;

    public bool IsBackendReady => _isReady;

    /// <summary>Herhangi bir yerden erişim için singleton.</summary>
    public static BackendProcessManager Instance => _instance;

    // ───────────────────────── Otomatik Oluşturma ─────────────────────────

    /// <summary>
    /// Sahneye elle eklemeye gerek yok! Oyun başlarken otomatik oluşur.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;

        var go = new GameObject("BackendManager");
        go.AddComponent<BackendProcessManager>();
        // Awake() içinde DontDestroyOnLoad çağrılacak
        Debug.Log("[BackendManager] Otomatik oluşturuldu (RuntimeInitializeOnLoadMethod).");
    }

    // ───────────────────────── Lifecycle ─────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(StartBackendRoutine());
    }

    private void OnApplicationQuit()
    {
        StopBackend();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            StopBackgroundHealthCheck();
            StopBackend();
        }
    }

    // ───────────────────────── Backend Başlatma ─────────────────────────

    private IEnumerator StartBackendRoutine()
    {
        // Zaten çalışıyorsa tekrar başlatma
        yield return StartCoroutine(CheckHealth(isInitial: true));
        if (_isReady)
        {
            Debug.Log("[BackendManager] ✅ Backend zaten çalışıyor, tekrar başlatmaya gerek yok.");
            _weStartedIt = false;
            StartBackgroundHealthCheck();
            yield break;
        }

        // Portu meşgul eden eski bir process varsa öldür
        KillProcessOnPort(port);
        yield return new WaitForSeconds(0.5f);

        string backendDir = ResolveBackendPath();
        if (string.IsNullOrEmpty(backendDir))
        {
            Debug.LogError("[BackendManager] ❌ Backend klasörü bulunamadı! Aranan yollar:\n" +
                           "  - Build: <exe_dir>/backend\n" +
                           "  - Editor: <project>/../ai/backend");
            yield break;
        }

        string pythonExe = FindPythonExecutable(backendDir);
        if (string.IsNullOrEmpty(pythonExe))
        {
            Debug.LogError("[BackendManager] ❌ Python bulunamadı! Aranan yollar:\n" +
                           $"  - {Path.Combine(backendDir, ".venv", "Scripts", "python.exe")}\n" +
                           $"  - {Path.Combine(backendDir, "venv", "Scripts", "python.exe")}\n" +
                           "  - Sistem PATH'indeki python.exe");
            yield break;
        }

        string appDir = Path.Combine(backendDir, "app");
        if (!Directory.Exists(appDir))
        {
            Debug.LogError($"[BackendManager] ❌ app klasörü bulunamadı: {appDir}");
            yield break;
        }

        Debug.Log($"[BackendManager] Backend başlatılıyor...\n" +
                  $"  Python : {pythonExe}\n" +
                  $"  Dizin  : {backendDir}\n" +
                  $"  Adres  : http://{host}:{port}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"-m uvicorn app.main:app --host {host} --port {port}",
                WorkingDirectory = backendDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            // ─── Environment Variables ───
            // PYTHONPATH'e backend kök dizinini ekle (rag_core import için)
            string existingPythonPath = Environment.GetEnvironmentVariable("PYTHONPATH") ?? "";
            psi.EnvironmentVariables["PYTHONPATH"] = backendDir +
                (string.IsNullOrEmpty(existingPythonPath) ? "" : ";" + existingPythonPath);

            // .env dosyasını oku ve environment'a ekle
            LoadDotEnv(backendDir, psi);

            _backendProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            // ─── Async stdout/stderr okuma (DEADLOCK ÖNLEMİ) ───
            // Bunları OKUMADAN redirect ederseniz, buffer dolar ve process kilitlenir.
            _backendProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    Debug.Log($"[Backend] {args.Data}");
            };
            _backendProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // Uvicorn normal loglarını stderr'e yazar, bu yüzden hepsini error yapmıyoruz
                    if (args.Data.Contains("ERROR") || args.Data.Contains("Traceback"))
                        Debug.LogError($"[Backend] {args.Data}");
                    else
                        Debug.Log($"[Backend] {args.Data}");
                }
            };

            if (!_backendProcess.Start())
            {
                Debug.LogError("[BackendManager] ❌ Backend process başlatılamadı!");
                yield break;
            }

            // Async okumayı BAŞLAT (Start()'tan sonra çağrılmalı)
            _backendProcess.BeginOutputReadLine();
            _backendProcess.BeginErrorReadLine();

            _weStartedIt = true;

            Debug.Log($"[BackendManager] Process başlatıldı (PID: {_backendProcess.Id}). " +
                       "Sunucunun hazır olması bekleniyor...");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BackendManager] ❌ Process başlatma hatası: {e.Message}\n{e.StackTrace}");
            yield break;
        }

        // Health check ile hazır olmasını bekle
        for (int i = 0; i < maxRetries; i++)
        {
            yield return new WaitForSeconds(healthCheckInterval);
            yield return StartCoroutine(CheckHealth(isInitial: false));

            if (_isReady)
            {
                Debug.Log($"[BackendManager] ✅ Backend hazır! (http://{host}:{port}) — " +
                          $"{i + 1}. denemede başarılı");
                StartBackgroundHealthCheck();
                yield break;
            }

            // Process çöktüyse vazgeç
            if (_backendProcess != null && _backendProcess.HasExited)
            {
                Debug.LogError($"[BackendManager] ❌ Backend process beklenmedik şekilde kapandı! " +
                               $"Exit code: {_backendProcess.ExitCode}");
                yield break;
            }
        }

        Debug.LogWarning($"[BackendManager] ⚠️ Backend {maxRetries} denemede hazır olmadı. " +
                          "Process çalışıyor ama health check yanıt vermiyor.");
    }

    // ───────────────────────── Backend Durdurma ─────────────────────────

    private void StopBackend()
    {
        // Biz başlatmadıysak dokunma (dışarıdan manuel başlatılmış olabilir)
        if (!_weStartedIt) return;
        if (_backendProcess == null) return;

        try
        {
            if (!_backendProcess.HasExited)
            {
                // Process tree kill — alt process'leri de öldürür
                // (uvicorn worker'ları portu meşgul bırakmasın)
                KillProcessTree(_backendProcess.Id);
                Debug.Log("[BackendManager] Backend process ağacı sonlandırıldı.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BackendManager] Process kapatma hatası: {e.Message}");
        }
        finally
        {
            try { _backendProcess.Dispose(); } catch { }
            _backendProcess = null;
            _isReady = false;
            _weStartedIt = false;
        }
    }

    /// <summary>Bir process ve tüm alt process'lerini öldürür.</summary>
    private static void KillProcessTree(int pid)
    {
        try
        {
            var killPsi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/T /F /PID {pid}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var killProc = Process.Start(killPsi))
            {
                killProc?.WaitForExit(5000);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BackendManager] taskkill hatası: {e.Message}");
        }
    }

    /// <summary>Belirli bir portu dinleyen process'i öldürür.</summary>
    private static void KillProcessOnPort(int targetPort)
    {
        try
        {
            // netstat ile portu dinleyen PID'i bul
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C netstat -ano | findstr \":{targetPort}\" | findstr \"LISTENING\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            string output;
            using (var proc = Process.Start(psi))
            {
                output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit(3000);
            }

            if (string.IsNullOrEmpty(output)) return;

            // Son sütun PID
            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 && int.TryParse(parts[parts.Length - 1], out int pid) && pid > 0)
                {
                    Debug.Log($"[BackendManager] Port {targetPort} üzerinde eski process bulundu (PID: {pid}), kapatılıyor...");
                    KillProcessTree(pid);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BackendManager] Port kontrol hatası: {e.Message}");
        }
    }

    // ───────────────────────── .env Dosyası ─────────────────────────

    /// <summary>.env dosyasını okur ve environment variable'ları ProcessStartInfo'ya ekler.</summary>
    private static void LoadDotEnv(string backendDir, ProcessStartInfo psi)
    {
        string envPath = Path.Combine(backendDir, ".env");
        if (!File.Exists(envPath))
        {
            Debug.Log("[BackendManager] .env dosyası bulunamadı, atlanıyor.");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(envPath);
            int count = 0;
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                int eqIndex = trimmed.IndexOf('=');
                if (eqIndex <= 0) continue;

                string key = trimmed.Substring(0, eqIndex).Trim();
                string value = trimmed.Substring(eqIndex + 1).Trim();

                // Tırnak işaretlerini kaldır
                if (value.Length >= 2 &&
                    ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                     (value.StartsWith("'") && value.EndsWith("'"))))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                psi.EnvironmentVariables[key] = value;
                count++;
            }
            Debug.Log($"[BackendManager] .env dosyasından {count} değişken yüklendi.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BackendManager] .env okuma hatası: {e.Message}");
        }
    }

    // ───────────────────────── Yol Algılama ─────────────────────────

    private string ResolveBackendPath()
    {
        // 1) Inspector'dan özel yol verilmişse onu kullan
        if (!string.IsNullOrEmpty(customBackendPath))
        {
            string custom = Path.GetFullPath(customBackendPath);
            if (Directory.Exists(custom))
            {
                Debug.Log($"[BackendManager] Özel yol kullanılıyor: {custom}");
                return custom;
            }
            Debug.LogWarning($"[BackendManager] Özel yol bulunamadı: {custom}");
        }

        // 2) Build modunda: exe'nin yanındaki "backend" klasörü
        string buildPath = Path.Combine(Application.dataPath, "..", "backend");
        buildPath = Path.GetFullPath(buildPath);
        if (Directory.Exists(Path.Combine(buildPath, "app")))
        {
            Debug.Log($"[BackendManager] Build path kullanılıyor: {buildPath}");
            return buildPath;
        }

        // 3) Editor modunda: proje kökünden ../../ai/backend
        string editorPath = Path.Combine(Application.dataPath, "..", "..", "ai", "backend");
        editorPath = Path.GetFullPath(editorPath);
        if (Directory.Exists(Path.Combine(editorPath, "app")))
        {
            Debug.Log($"[BackendManager] Editor path kullanılıyor: {editorPath}");
            return editorPath;
        }

        // 4) Alternatif: proje kökünden ../ai/backend
        string altPath = Path.Combine(Application.dataPath, "..", "ai", "backend");
        altPath = Path.GetFullPath(altPath);
        if (Directory.Exists(Path.Combine(altPath, "app")))
        {
            Debug.Log($"[BackendManager] Alternatif path kullanılıyor: {altPath}");
            return altPath;
        }

        return null;
    }

    private string FindPythonExecutable(string backendDir)
    {
        // 1) .venv içindeki Python
        string venvPython = Path.Combine(backendDir, ".venv", "Scripts", "python.exe");
        if (File.Exists(venvPython)) return venvPython;

        // 2) venv klasörü (alternatif isim)
        string venvAlt = Path.Combine(backendDir, "venv", "Scripts", "python.exe");
        if (File.Exists(venvAlt)) return venvAlt;

        // 3) Sistem Python'u (PATH'ten)
        string systemPython = FindInPath("python.exe");
        if (!string.IsNullOrEmpty(systemPython)) return systemPython;

        return null;
    }

    private static string FindInPath(string exe)
    {
        string path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (string dir in path.Split(';'))
        {
            string full = Path.Combine(dir.Trim(), exe);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    // ───────────────────────── Health Check ─────────────────────────

    private IEnumerator CheckHealth(bool isInitial)
    {
        string url = $"http://{host}:{port}/health";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 3;
            yield return req.SendWebRequest();

            _isReady = (req.result == UnityWebRequest.Result.Success);

            if (isInitial && _isReady)
                Debug.Log("[BackendManager] Health check başarılı — sunucu zaten çalışıyor.");
        }
    }

    // ───────────────────────── Arka Plan Health Check ─────────────────────────

    private void StartBackgroundHealthCheck()
    {
        StopBackgroundHealthCheck();
        _backgroundHealthCheck = StartCoroutine(BackgroundHealthCheckRoutine());
    }

    private void StopBackgroundHealthCheck()
    {
        if (_backgroundHealthCheck != null)
        {
            StopCoroutine(_backgroundHealthCheck);
            _backgroundHealthCheck = null;
        }
    }

    private IEnumerator BackgroundHealthCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(backgroundCheckInterval);
            yield return StartCoroutine(CheckHealth(isInitial: false));

            if (!_isReady)
            {
                Debug.LogWarning("[BackendManager] ⚠️ Backend yanıt vermiyor!");

                if (autoRestart && _weStartedIt)
                {
                    Debug.Log("[BackendManager] Otomatik yeniden başlatma deneniyor...");
                    StopBackend();
                    yield return new WaitForSeconds(1f);
                    yield return StartCoroutine(StartBackendRoutine());
                    yield break; // StartBackendRoutine kendi arka plan kontrolünü başlatır
                }
            }
        }
    }
}
