@echo off
chcp 866 >nul
cd /d "%~dp0"

set "PATH=C:\Program Files\Git\cmd;%PATH%"

where git >nul 2>&1
if errorlevel 1 (
    echo Git не найден. Установите его: winget install Git.Git
    echo.
    pause
    exit /b 1
)

echo ============================================
echo   Обновление репозитория на GitHub
echo ============================================
echo.

git add -A

git diff --cached --quiet
if %errorlevel%==0 (
    echo Изменений нет - все уже на GitHub.
    echo.
    pause
    exit /b 0
)

set "MSG=%~1"
if "%MSG%"=="" set "MSG=update: %date% %time%"

echo Коммит: %MSG%
git commit -m "%MSG%"
git push

echo.
echo ============================================
echo   Готово! Изменения загружены на GitHub.
echo   https://github.com/oneshkof/ecp-certificates
echo ============================================
echo.
pause