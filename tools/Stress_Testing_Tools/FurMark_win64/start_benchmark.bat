
rem benchmark with preset (-pxxxx)
rem --no-score-box: the score box is not displayed at the end of the benchmark 
rem but score information is available in the log file and in the _scores.csv file.
rem
rem furmark --demo furmark-gl --p1080 --no-score-box
rem furmark --demo furmark-vk --p1080 --no-score-box

rem benchmark with custom settings and duration (10 sec).
rem furmark --demo furmark-vk --benchmark --width 1920 --height 1080 --max-time 10
furmark --demo furmark-gl --benchmark --width 1920 --height 1080 --max-time 10
rem furmark --demo furmark-vk --benchmark --width 1920 --height 1080 --no-osi

rem benchmark with custom settings (duration 5 sec). 
rem furmark --demo furmark-vk --benchmark --width 1920 --height 1080 --no-osi --max-time 5

