@echo off
rem VRAM test is only available for FurMark 2 64-bit and currently only for OpenGL.

:menu
cls
echo 1. start a 2GB VRAM test
echo 2. start a 4GB VRAM test
echo 3. start a 8GB VRAM test
echo 4. start a 12GB VRAM test
echo 5. start a 16GB VRAM test
echo 6. start a 20GB VRAM test
echo 7. start a 24GB VRAM test
echo 8. Exit

set /p choice=Select the menu item: 

if "%choice%"=="1" goto task1
if "%choice%"=="2" goto task2
if "%choice%"=="3" goto task3
if "%choice%"=="4" goto task4
if "%choice%"=="5" goto task5
if "%choice%"=="6" goto task6
if "%choice%"=="7" goto task7
if "%choice%"=="8" goto exit

:task1
echo start a 2GB VRAM test
furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 2
pause
goto menu

:task2
echo start a 4GB VRAM test
furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 4
pause
goto menu

:task3
echo start a 8GB VRAM test
furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 8
pause
goto menu

:task4
echo start a 12GB VRAM test
furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 12
pause
goto menu

:task5
echo start a 16GB VRAM test
furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 16
pause
goto menu

:task6
echo start a 20GB VRAM test
furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 20
pause
goto menu

:task7
echo start a 24GB VRAM test
furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 24
pause
goto menu


:exit
echo Good bye!
pause