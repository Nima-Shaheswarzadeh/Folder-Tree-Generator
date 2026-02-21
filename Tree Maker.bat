@echo off
title Folder Structure Generator
color 0B
mode 80,25

:menu
cls
echo ==========================================================
echo                 FOLDER TREE STRUCTURE TOOL
echo ==========================================================
echo.
echo This tool will:
echo.
echo  - Scan current folder and all subfolders
echo  - Generate a clean tree structure file
echo  - Save result as: Folder_Structure.txt
echo.
echo ==========================================================
echo.
set /p choice=Do you want to continue? (Y/N): 

if /I "%choice%"=="Y" goto start
if /I "%choice%"=="N" goto exit
goto menu

:start
cls
echo.
echo Generating tree structure...
echo Please wait...
echo.

:: Generate tree with files included
tree /F /A > Folder_Structure.txt

echo.
echo ==========================================================
echo                  PROCESS COMPLETED SUCCESSFULLY
echo ==========================================================
echo.
echo Output file created:
echo %cd%\Folder_Structure.txt
echo.
pause
goto end

:exit
cls
echo Operation cancelled by user.
pause
goto end

:end
exit
