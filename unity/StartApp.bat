@echo off
chcp 65001 >nul
title VR Anatomy

:: ─── Yolları belirle ───
set "ROOT=%~dp0"
set "BACKEND=%ROOT%backend"
set "PYTHON=%BACKEND%\.venv\Scripts\python.exe"
set "APP_DIR=%BACKEND%\app"
set "UNITY_EXE=%ROOT%VRAnatomy.exe"

:: ─── Python kontrolü ───
if not exist "%PYTHON%" (
    echo [HATA] Python bulunamadi: %PYTHON%
    echo Lutfen .venv klasorunun "backend" icinde oldugunden emin olun.
    pause
    exit /b 1
)

:: ─── Unity exe kontrolü ───
if not exist "%UNITY_EXE%" (
    echo [HATA] Unity uygulamasi bulunamadi: %UNITY_EXE%
    echo Bu dosyayi Unity build klasorune koyun.
    pause
    exit /b 1
)

:: ─── PYTHONPATH ayarla (rag_core import için) ───
set "PYTHONPATH=%BACKEND%;%PYTHONPATH%"

:: ─── Backend'i gizli pencerede başlat ───
echo Backend baslatiliyor...
start /B "" "%PYTHON%" -m uvicorn main:app --host 127.0.0.1 --port 8001 --app-dir "%APP_DIR%"

:: ─── Sunucunun hazır olmasını bekle ───
set RETRIES=0
:wait_loop
if %RETRIES% GEQ 10 (
    echo [UYARI] Backend %RETRIES% denemede hazir olmadi, yine de Unity baslatiliyor...
    goto launch_unity
)
timeout /t 1 /nobreak >nul
set /a RETRIES+=1

:: Health check
powershell -Command "try { $r = Invoke-WebRequest -Uri 'http://127.0.0.1:8001/health' -UseBasicParsing -TimeoutSec 2; if ($r.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }" >nul 2>&1
if %errorlevel%==0 (
    echo Backend hazir!
    goto launch_unity
)
echo   Bekleniyor... (%RETRIES%/10)
goto wait_loop

:launch_unity
echo VR Anatomy baslatiliyor...

:: ─── Unity'yi başlat ve kapanmasını BEKLE ───
start /wait "" "%UNITY_EXE%"

:: ─── Unity kapandı, backend'i kapat ───
echo Kapatiliyor...
taskkill /f /im python.exe /fi "WINDOWTITLE eq *" >nul 2>&1

echo Tamam, iyi gunler!
timeout /t 2 /nobreak >nul
