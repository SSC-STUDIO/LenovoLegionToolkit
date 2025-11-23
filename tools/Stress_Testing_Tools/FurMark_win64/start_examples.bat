
rem benchmark with preset (-pxxxx)
rem --no-score-box: the score box is not displayed at the end of the benchmark 
rem but score information is available in the log file and in the _scores.csv file.
rem
rem furmark --demo furmark-gl --p1080 --no-score-box
rem furmark --demo furmark-vk --p1080 --no-score-box

rem start 1280x720 / OpenGL
furmark --demo furmark-gl --width 1280 --height 720

rem start 1280x720 / Vulkan, GPU:0
rem furmark --demo furmark-vk --width 1280 --height 720 --gpu-index 0

rem start in fullscreen mode
rem furmark --demo furmark-gl --width 1920 --height 1080 --fullscreen

rem benchmark with custom settings and default duration (60 sec).
rem furmark --demo furmark-vk --benchmark --width 1920 --height 1080 --duration-ms 60000

rem benchmark with custom settings and default duration (5 sec).
rem furmark --demo furmark-gl --benchmark --width 1920 --height 1080 --duration-ms 5000

rem start without OSI (On Screen Information).
rem furmark --demo furmark-vk --benchmark --width 1920 --height 1080 --no-osi

rem benchmark with custom settings (duration 5 sec). 
rem furmark --demo furmark-vk --benchmark --width 1920 --height 1080 --no-osi --duration-ms 5000

rem start with DXT5 compressed textures
rem furmark --demo furmark-gl --width 1920 --height 1080 --furmark-dxt5

rem start a 4GB VRAM test (FurMark 64-bit only)
rem furmark --demo furmark-gl --width 1920 --height 1080 --furmark-vram-test-gb 4

rem change background image and fur color
rem furmark --demo furmark-gl --width 1920 --height 1080 --furmark-bkg-img-id 4 --furmark-fur-img-id 1

rem start artifact scanner
rem furmark --demo furmark-gl --width 1920 --height 1080 --artifact-scanner

rem run the demo for 20 sec
rem furmark --demo furmark-gl --width 1920 --height 1080 --max-time 20

rem run the demo for 1000 frames
rem furmark --demo furmark-gl --width 1920 --height 1080 --max-frames 1000

rem run the demo and log gpu data to file (in exports/ folder)
rem furmark --demo furmark-gl --width 1920 --height 1080 --log-gpu-data

rem run the demo and log gpu 0 data to file (in exports/ folder)
rem furmark --demo furmark-gl --width 1920 --height 1080 --log-gpu-data --log-gpu-index 0

rem run the demo and log gpu 0 data to custom file
rem furmark --demo furmark-gl --width 1920 --height 1080 --log-gpu-data --log-gpu-index 0 --log-gpu-data-filename "exports/gpudata.csv"
